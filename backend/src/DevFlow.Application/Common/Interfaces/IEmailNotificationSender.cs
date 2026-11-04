namespace DevFlow.Application.Common.Interfaces;

public interface IEmailNotificationSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct);
}
