using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WorkshopManager.Models;
using System.IO;

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
}
