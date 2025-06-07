using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WorkshopManager.Models;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

namespace WorkshopManager.Services
{
    public class PdfService
    {
        public byte[] GenerateServiceOrderPdf(ServiceOrder order)
        {
            // Aktywacja licencji community QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;

            // Tworzenie dokumentu
            var document = new ServiceOrderDocument(order);
            
            // Generowanie dokumentu PDF jako tablica bajtów
            return document.GeneratePdf();
        }
        
        public byte[] GenerateOpenOrdersReport(List<ServiceOrder> orders)
        {
            // Aktywacja licencji community QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;

            // Tworzenie dokumentu
            var document = new OpenOrdersReportDocument(orders);
            
            // Generowanie dokumentu PDF jako tablica bajtów
            return document.GeneratePdf();
        }
    }

    // Klasa dokumentu do definiowania struktury i wyglądu dokumentu PDF
    public class ServiceOrderDocument : QuestPDF.Infrastructure.IDocument
    {
        private readonly ServiceOrder _order;
        
        public ServiceOrderDocument(ServiceOrder order)
        {
            _order = order;
        }

        public void Compose(QuestPDF.Infrastructure.IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(50);

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        void ComposeHeader(QuestPDF.Infrastructure.IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Warsztat samochodowy").Bold().FontSize(20);
                    column.Item().Text("Raport zamówienia serwisowego").FontSize(12);
                    column.Item().Text($"Nr zamówienia: {_order.Id}").FontSize(12);
                });

                row.ConstantItem(100).Image("wwwroot/favicon.ico");
            });
        }

        void ComposeContent(QuestPDF.Infrastructure.IContainer container)
        {
            container.PaddingVertical(20).Column(column =>
            {
                // Dane klienta
                column.Item().Text("Dane klienta").Bold().FontSize(14);
                column.Item().PaddingBottom(5).LineHorizontal(1);
                
                if (_order.Vehicle?.Customer != null)
                {
                    column.Item().Text($"Imię i nazwisko: {_order.Vehicle.Customer.FirstName} {_order.Vehicle.Customer.LastName}");
                    column.Item().Text($"Telefon: {_order.Vehicle.Customer.Phone}");
                }
                
                column.Item().PaddingVertical(10);

                // Dane pojazdu
                column.Item().Text("Dane pojazdu").Bold().FontSize(14);
                column.Item().PaddingBottom(5).LineHorizontal(1);

                if (_order.Vehicle != null)
                {
                    column.Item().Text($"Marka: {_order.Vehicle.Brand}");
                    column.Item().Text($"Model: {_order.Vehicle.Model}");
                    column.Item().Text($"VIN: {_order.Vehicle.Vin}");
                    column.Item().Text($"Nr rejestracyjny: {_order.Vehicle.RegistrationNumber}");
                    column.Item().Text($"Rok produkcji: {_order.Vehicle.Year}");
                }
                
                column.Item().PaddingVertical(10);

                // Dane zamówienia
                column.Item().Text("Szczegóły zamówienia").Bold().FontSize(14);
                column.Item().PaddingBottom(5).LineHorizontal(1);
                
                column.Item().Text($"Status: {_order.Status}");
                column.Item().Text($"Data utworzenia: {_order.CreatedAt:dd.MM.yyyy HH:mm}");
                
                if (_order.ClosedAt.HasValue)
                    column.Item().Text($"Data zamknięcia: {_order.ClosedAt:dd.MM.yyyy HH:mm}");
                
                column.Item().Text($"Opis problemu: {_order.ProblemDescription}");
                
                column.Item().PaddingVertical(10);

                // Lista zadań serwisowych
                if (_order.ServiceTasks != null && _order.ServiceTasks.Any())
                {
                    column.Item().Text("Zadania serwisowe").Bold().FontSize(14);
                    column.Item().PaddingBottom(5).LineHorizontal(1);
                    
                    foreach (var task in _order.ServiceTasks)
                    {
                        column.Item().Text($"• {task.Description} - Koszt robocizny: {task.LaborCost:C}");
                        
                        // Użyte części
                        if (task.UsedParts != null && task.UsedParts.Any())
                        {
                            foreach (var part in task.UsedParts)
                            {
                                // Wyświetlenie informacji o części jeśli dostępna
                                if (part.Part != null)
                                {
                                    var serviceCost = part.ServiceCost.HasValue ? $", Koszt usługi: {part.ServiceCost:C}" : "";
                                    column.Item().Text($"  - {part.Part.Name} ({part.Part.Manufacturer}) - {part.Quantity} szt. x {part.Part.UnitPrice:C}{serviceCost}");
                                }
                                else
                                {
                                    column.Item().Text($"  - Część #{part.PartId} - {part.Quantity} szt.");
                                }
                            }
                        }
                    }
                }
                
                column.Item().PaddingVertical(10);

                // Suma kosztów
                decimal laborCosts = _order.ServiceTasks?.Sum(t => t.LaborCost) ?? 0;
                decimal partsCosts = 0;
                decimal serviceCosts = 0;

                if (_order.ServiceTasks != null)
                {
                    foreach (var task in _order.ServiceTasks)
                    {
                        if (task.UsedParts != null)
                        {
                            foreach (var part in task.UsedParts)
                            {
                                if (part.Part != null)
                                {
                                    partsCosts += part.Quantity * part.Part.UnitPrice;
                                    serviceCosts += part.ServiceCost ?? 0;
                                }
                            }
                        }
                    }
                }

                column.Item().LineHorizontal(1);
                column.Item().Text("Podsumowanie kosztów").Bold().FontSize(14);
                column.Item().Text($"Koszt robocizny: {laborCosts:C}");
                column.Item().Text($"Koszt części: {partsCosts:C}");
                column.Item().Text($"Koszt usług: {serviceCosts:C}");
                column.Item().Text($"Suma: {(laborCosts + partsCosts + serviceCosts):C}").Bold();
            });
        }

        void ComposeFooter(QuestPDF.Infrastructure.IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(text =>
                    {
                        text.Span("Wygenerowano: ").FontSize(10);
                        text.Span($"{DateTime.Now:dd.MM.yyyy HH:mm:ss}").FontSize(10);
                    });
                    column.Item().Text(text =>
                    {
                        text.Span("© 2025 Warsztat samochodowy - ").FontSize(10);
                        text.Span("Wszelkie prawa zastrzeżone").FontSize(10);
                    });
                });

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Strona ").FontSize(10);
                    text.CurrentPageNumber().FontSize(10);
                    text.Span(" z ").FontSize(10);
                    text.TotalPages().FontSize(10);
                });
            });
        }

        public QuestPDF.Infrastructure.DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = $"Raport zamówienia #{_order.Id}",
            Author = "System Warsztat Samochodowy",
            Subject = $"Raport zamówienia serwisowego #{_order.Id}",
            Keywords = "zamówienie, serwis, warsztat, naprawa"
        };
    }
    
    // Klasa dokumentu dla raportu o otwartych zamówieniach
    public class OpenOrdersReportDocument : QuestPDF.Infrastructure.IDocument
    {
        private readonly List<ServiceOrder> _orders;
        
        public OpenOrdersReportDocument(List<ServiceOrder> orders)
        {
            _orders = orders;
        }

        public void Compose(QuestPDF.Infrastructure.IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(50);

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        void ComposeHeader(QuestPDF.Infrastructure.IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Warsztat samochodowy").Bold().FontSize(20);
                    column.Item().Text("Raport otwartych zamówień serwisowych").FontSize(14);
                    column.Item().Text($"Data wygenerowania: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(12);
                });

                row.ConstantItem(100).Image("wwwroot/favicon.ico");
            });
        }

        void ComposeContent(QuestPDF.Infrastructure.IContainer container)
        {
            container.PaddingVertical(20).Column(column =>
            {
                column.Item().Text($"Liczba otwartych zleceń: {_orders.Count}").Bold().FontSize(14);
                column.Item().PaddingVertical(10);
                
                // Tabela z podsumowaniem
                column.Item().Table(table =>
                {
                    // Definicje kolumn
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);       // Nr
                        columns.RelativeColumn(2);        // Klient
                        columns.RelativeColumn(2);        // Pojazd
                        columns.RelativeColumn(1);        // Status
                        columns.RelativeColumn(2);      // Data utworzenia
                        columns.RelativeColumn(2);      // Mechanik
                    });

                    // Nagłówek tabeli
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Nr");
                        header.Cell().Element(HeaderCell).Text("Klient");
                        header.Cell().Element(HeaderCell).Text("Pojazd");
                        header.Cell().Element(HeaderCell).Text("Status");
                        header.Cell().Element(HeaderCell).Text("Data utworzenia");
                        header.Cell().Element(HeaderCell).Text("Mechanik");
                        
                        static IContainer HeaderCell(IContainer container) => 
                            container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                    });

                    // Zawartość tabeli
                    foreach (var order in _orders)
                    {
                        string clientName = order.Vehicle?.Customer != null ? $"{order.Vehicle.Customer.FirstName} {order.Vehicle.Customer.LastName}" : "Brak danych";
                        string vehicleInfo = order.Vehicle != null ? $"{order.Vehicle.Brand} {order.Vehicle.Model} ({order.Vehicle.RegistrationNumber})" : "Brak danych";
                        string mechanicName = order.AssignedMechanic?.UserName ?? "Nieprzypisany";

                        table.Cell().Element(NormalCell).Text(order.Id.ToString());
                        table.Cell().Element(NormalCell).Text(clientName);
                        table.Cell().Element(NormalCell).Text(vehicleInfo);
                        table.Cell().Element(NormalCell).Text(order.Status.ToString());
                        table.Cell().Element(NormalCell).Text(order.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
                        table.Cell().Element(NormalCell).Text(mechanicName);

                        static IContainer NormalCell(IContainer container) =>
                            container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                    }
                });
                
                column.Item().PaddingVertical(10);
                
                // Szczegóły każdego zamówienia
                column.Item().Text("Szczegóły zamówień:").Bold().FontSize(14);
                column.Item().PaddingVertical(5);
                
                foreach (var order in _orders)
                {
                    column.Item().Element(container => ComposeSingleOrder(container, order));
                    column.Item().PaddingVertical(5);
                }
            });
        }
        
        void ComposeSingleOrder(QuestPDF.Infrastructure.IContainer container, ServiceOrder order)
        {
            container.Border(1).BorderColor(Colors.Grey.Medium).Padding(10).Column(column =>
            {
                // Nagłówek zamówienia
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Zamówienie #{order.Id}").Bold().FontSize(12);
                    row.RelativeItem().AlignRight().Text($"Status: {order.Status}").FontSize(12);
                });
                
                column.Item().PaddingVertical(3);

                // Dane klienta i pojazdu
                if (order.Vehicle?.Customer != null)
                {
                    column.Item().Text($"Klient: {order.Vehicle.Customer.FirstName} {order.Vehicle.Customer.LastName}");
                }
                
                if (order.Vehicle != null)
                {
                    column.Item().Text($"Pojazd: {order.Vehicle.Brand} {order.Vehicle.Model} ({order.Vehicle.RegistrationNumber})");
                }
                
                column.Item().Text($"Data przyjęcia: {order.CreatedAt:dd.MM.yyyy HH:mm}");
                column.Item().Text($"Mechanik: {order.AssignedMechanic?.UserName ?? "Nieprzypisany"}");
                column.Item().Text($"Opis problemu: {order.ProblemDescription}");
                
                // Zadania serwisowe i części
                if (order.ServiceTasks != null && order.ServiceTasks.Any())
                {
                    column.Item().PaddingVertical(5);
                    column.Item().Text("Zadania serwisowe:").SemiBold();
                    
                    foreach (var task in order.ServiceTasks)
                    {
                        column.Item().Text($"• {task.Description} - Koszt robocizny: {task.LaborCost:C}");
                        
                        if (task.UsedParts != null && task.UsedParts.Any())
                        {
                            foreach (var usedPart in task.UsedParts)
                            {
                                if (usedPart.Part != null)
                                {
                                    decimal partTotal = usedPart.Quantity * usedPart.Part.UnitPrice;
                                    decimal serviceTotal = usedPart.ServiceCost ?? 0;
                                    
                                    column.Item().PaddingLeft(5).Text(text =>
                                    {
                                        text.Span("- ");
                                        text.Span($"{usedPart.Part.Name} ({usedPart.Part.Manufacturer}): ");
                                        text.Span($"{usedPart.Quantity} szt. x {usedPart.Part.UnitPrice:C} = {partTotal:C}");
                                        
                                        if (serviceTotal > 0)
                                        {
                                            text.Span($" + usługa {serviceTotal:C}");
                                        }
                                    });
                                }
                            }
                        }
                    }
                }
                
                // Podsumowanie kosztów
                decimal laborCosts = order.ServiceTasks?.Sum(t => t.LaborCost) ?? 0;
                decimal partsCosts = 0;
                decimal serviceCosts = 0;
                
                if (order.ServiceTasks != null)
                {
                    foreach (var task in order.ServiceTasks)
                    {
                        if (task.UsedParts != null)
                        {
                            foreach (var part in task.UsedParts)
                            {
                                if (part.Part != null)
                                {
                                    partsCosts += part.Quantity * part.Part.UnitPrice;
                                    serviceCosts += part.ServiceCost ?? 0;
                                }
                            }
                        }
                    }
                }
                
                column.Item().PaddingVertical(5);
                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
                column.Item().PaddingVertical(3);
                column.Item().Text("Podsumowanie kosztów:").SemiBold();
                column.Item().Text($"Koszt robocizny: {laborCosts:C}");
                column.Item().Text($"Koszt części: {partsCosts:C}");
                column.Item().Text($"Koszt usług: {serviceCosts:C}");
                column.Item().Text($"Łącznie: {(laborCosts + partsCosts + serviceCosts):C}").SemiBold();
            });
        }

        void ComposeFooter(QuestPDF.Infrastructure.IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(text =>
                    {
                        text.Span("Wygenerowano: ").FontSize(10);
                        text.Span($"{DateTime.Now:dd.MM.yyyy HH:mm:ss}").FontSize(10);
                    });
                    column.Item().Text(text =>
                    {
                        text.Span("© 2025 Warsztat samochodowy - ").FontSize(10);
                        text.Span("Wszelkie prawa zastrzeżone").FontSize(10);
                    });
                });

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Strona ").FontSize(10);
                    text.CurrentPageNumber().FontSize(10);
                    text.Span(" z ").FontSize(10);
                    text.TotalPages().FontSize(10);
                });
            });
        }

        public QuestPDF.Infrastructure.DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = "Raport otwartych zamówień",
            Author = "System Warsztat Samochodowy",
            Subject = $"Raport otwartych zamówień z dnia {DateTime.Now:yyyy-MM-dd}",
            Keywords = "zamówienie, serwis, warsztat, naprawa, raport"
        };
    }
}
