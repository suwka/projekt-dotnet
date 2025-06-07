using System;
using System.IO;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace WorkshopManager.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailWithAttachmentAsync(string to, string subject, string body, string attachmentPath, string attachmentName)
        {
            try
            {
                // Pobranie ustawień SMTP z konfiguracji
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                string host = smtpSettings["Host"];
                int port = int.Parse(smtpSettings["Port"]);
                string username = smtpSettings["Username"];
                string password = smtpSettings["Password"];
                bool enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");
                string fromEmail = smtpSettings["FromEmail"];
                string fromName = smtpSettings["FromName"];

                _logger.LogInformation($"Próba wysłania e-maila do {to} z serwera {host}:{port}");

                // Tworzenie wiadomości
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress("Administrator", to));
                message.Subject = subject;
                
                // Dodanie nagłówków poprawiających dostarczalność
                message.Headers.Add("X-Mailer", "WorkshopManager Application");
                message.Headers.Add("X-Priority", "1"); // Wysoki priorytet
                
                // Ustawienie adresu Reply-To (odpowiedź na)
                message.ReplyTo.Add(new MailboxAddress(fromName, fromEmail));
                
                // Ustawienie priorytetu i flagowanie jako ważne
                message.Importance = MessageImportance.High;

                // Tworzenie części wiadomości
                var builder = new BodyBuilder
                {
                    TextBody = body
                };

                // Dodawanie załącznika (jeśli istnieje)
                if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                {
                    _logger.LogInformation($"Dodawanie załącznika: {attachmentPath}");
                    using var stream = File.OpenRead(attachmentPath);
                    builder.Attachments.Add(attachmentName, stream);
                }
                else if (!string.IsNullOrEmpty(attachmentPath))
                {
                    _logger.LogWarning($"Załącznik nie istnieje: {attachmentPath}");
                }

                // Dodanie wiadomości
                message.Body = builder.ToMessageBody();

                // Wysyłanie e-maila
                using var client = new SmtpClient();
                
                // Rejestracja zdarzeń do lepszego logowania
                client.MessageSent += (sender, args) => {
                    _logger.LogInformation($"Wiadomość wysłana. Response: {args.Response}");
                };
                
                _logger.LogInformation("Łączenie z serwerem SMTP...");
                
                SecureSocketOptions secureOption;
                if (port == 465)
                {
                    secureOption = enableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None;
                }
                else if (port == 587)
                {
                    secureOption = SecureSocketOptions.StartTls; 
                }
                else
                {
                    // Domyślne zachowanie dla innych portów
                    secureOption = enableSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None;
                }
                
                _logger.LogInformation($"Używane zabezpieczenie połączenia: {secureOption}");
                
                try 
                {
                    await client.ConnectAsync(host, port, secureOption);
                    
                    _logger.LogInformation($"Połączenie nawiązane z {host}:{port}. Protokół: {client.Capabilities}");
                    
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    {
                        _logger.LogInformation($"Uwierzytelnianie na serwerze SMTP z użytkownikiem: {username}");
                        await client.AuthenticateAsync(username, password);
                        _logger.LogInformation("Uwierzytelnianie zakończone sukcesem.");
                    }
                    
                    _logger.LogInformation("Wysyłanie wiadomości...");
                    var response = await client.SendAsync(message);
                    _logger.LogInformation($"Odpowiedź serwera po wysłaniu: {response}");
                    
                    _logger.LogInformation("Rozłączanie z serwerem SMTP...");
                    await client.DisconnectAsync(true);
                    
                    _logger.LogInformation("E-mail wysłany pomyślnie do {0}", to);
                    return true;
                }
                catch (AuthenticationException authEx)
                {
                    _logger.LogError($"Błąd uwierzytelniania na serwerze SMTP: {authEx.Message}");
                    return false;
                }
                catch (SmtpCommandException cmdEx)
                {
                    _logger.LogError($"Błąd polecenia SMTP: {cmdEx.Message} (Status: {cmdEx.StatusCode}, ErrorCode: {cmdEx.ErrorCode})");
                    return false;
                }
                catch (SmtpProtocolException protocolEx)
                {
                    _logger.LogError($"Błąd protokołu SMTP: {protocolEx.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Błąd podczas komunikacji z serwerem SMTP: {ex.Message}");
                    return false;
                }
                finally
                {
                    if (client.IsConnected)
                        await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wystąpił błąd podczas przygotowywania i wysyłania e-maila");
                return false;
            }
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // Ta sama metoda co wyżej, ale bez załącznika
            await SendEmailWithAttachmentAsync(to, subject, body, null, null);
        }
    }
}
