using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkshopManager.Data;
using WorkshopManager.Models;
using WorkshopManager.Services;

namespace WorkshopManager.Services
{
    public class OpenOrderReportBackgroundService : BackgroundService
    {
        private readonly ILogger<OpenOrderReportBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly string _reportsFolder;

        public OpenOrderReportBackgroundService(
            ILogger<OpenOrderReportBackgroundService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _reportsFolder = Path.Combine("wwwroot", "reports");

            // Utworzenie folderu na raporty, jeśli nie istnieje
            if (!Directory.Exists(_reportsFolder))
            {
                Directory.CreateDirectory(_reportsFolder);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Usługa generowania raportów została uruchomiona");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Rozpoczynam generowanie raportu z otwartych zamówień - {time}", DateTimeOffset.Now);

                try
                {
                    // Generowanie i wysłanie raportu
                    await GenerateAndSendReportAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Wystąpił błąd podczas generowania lub wysyłania raportu");
                }

                // W trybie testowym generowanie co 2 minuty
                // W wersji produkcyjnej można zmienić na np. 24 godziny
                _logger.LogInformation("Raport wygenerowany. Następny raport za 2 minuty.");
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                
                // W wersji produkcyjnej użyj:
                // await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task GenerateAndSendReportAsync()
        {
            // Tworzenie nowego zakresu dla usług aby zapewnić prawidłowe zarządzanie zasobami
            using var scope = _serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            
            try
            {
                // Pobranie niezbędnych serwisów
                var dbContext = services.GetRequiredService<ApplicationDbContext>();
                var pdfService = services.GetRequiredService<PdfService>();
                var emailService = services.GetRequiredService<EmailService>();

                // Pobranie otwartych zamówień serwisowych
                var openOrders = await dbContext.ServiceOrders
                    .Where(so => so.Status != ServiceOrderStatus.Zakonczone && 
                                so.Status != ServiceOrderStatus.Anulowane)
                    .Include(so => so.Vehicle)
                        .ThenInclude(v => v.Customer)
                    .Include(so => so.ServiceTasks)
                        .ThenInclude(st => st.UsedParts)
                            .ThenInclude(up => up.Part)
                    .Include(so => so.AssignedMechanic)
                    .ToListAsync();

                if (!openOrders.Any())
                {
                    _logger.LogInformation("Brak otwartych zamówień do uwzględnienia w raporcie");
                    return;
                }

                _logger.LogInformation("Znaleziono {count} otwartych zamówień", openOrders.Count);

                // Generowanie raportu PDF
                var reportDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
                var reportFileName = $"raport_otwartych_zamowien_{reportDate}.pdf";
                var reportFilePath = Path.Combine(_reportsFolder, reportFileName);

                // Generowanie danych raportu
                var reportPdfBytes = pdfService.GenerateOpenOrdersReport(openOrders);

                // Zapisz raport jako plik
                await File.WriteAllBytesAsync(reportFilePath, reportPdfBytes);
                
                _logger.LogInformation("Raport został wygenerowany i zapisany w lokalizacji: {path}", reportFilePath);

                // Przygotowanie treści emaila
                var emailBody = $@"
Dzień dobry,

W załączeniu przesyłamy raport otwartych zamówień wygenerowany dnia {DateTime.Now.ToString("dd.MM.yyyy")} o godzinie {DateTime.Now.ToString("HH:mm")}.

Statystyki:
- Liczba otwartych zamówień: {openOrders.Count}
- Zamówienia w trakcie realizacji: {openOrders.Count(o => o.Status == ServiceOrderStatus.WTrakcie)}
- Nowe zamówienia: {openOrders.Count(o => o.Status == ServiceOrderStatus.Nowe)}

Pozdrawiamy,
Zespół Warsztatu Samochodowego
";

                // Pobierz adres email administratora z konfiguracji (lub ustaw domyślny)
                var adminEmail = _configuration["AdminEmail"] ?? "admin@example.com";
                
                // Wyślij email z raportem
                var emailSent = await emailService.SendEmailWithAttachmentAsync(
                    adminEmail,
                    $"Raport otwartych zamówień - {DateTime.Now.ToString("dd.MM.yyyy")}",
                    emailBody,
                    reportFilePath,
                    reportFileName
                );

                if (emailSent)
                {
                    _logger.LogInformation("Raport został wysłany na adres: {email}", adminEmail);
                }
                else
                {
                    _logger.LogError("Nie udało się wysłać raportu na adres: {email}", adminEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wystąpił błąd podczas generowania lub wysyłania raportu");
            }
        }

        private byte[] GenerateOpenOrdersReport(PdfService pdfService, List<ServiceOrder> orders)
        {
            return pdfService.GenerateOpenOrdersReport(orders);
        }
    }
}
