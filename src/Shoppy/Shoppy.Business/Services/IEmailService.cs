namespace Shoppy.Business.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
