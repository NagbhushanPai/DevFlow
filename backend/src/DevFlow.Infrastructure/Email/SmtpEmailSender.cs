using DevFlow.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DevFlow.Infrastructure.Email;

public sealed class SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger) : IEmailNotificationSender
{
    private const int MaxAttempts = 3;
    private readonly EmailSettings _settings = options.Value;

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            logger.LogInformation("Email sending is not configured; skipping email to {ToEmail}.", toEmail);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);
                if (!string.IsNullOrWhiteSpace(_settings.Username)) await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
                await client.SendAsync(message, ct);
                await client.DisconnectAsync(true, ct);
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                logger.LogWarning(ex, "Attempt {Attempt} to send email to {ToEmail} failed; retrying.", attempt, toEmail);
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send email to {ToEmail} after {MaxAttempts} attempts.", toEmail, MaxAttempts);
                throw;
            }
        }
    }
}
