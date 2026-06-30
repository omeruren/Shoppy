using Microsoft.Extensions.Options;
using Shoppy.Business.Options;
using System.Net;
using System.Net.Mail;

namespace Shoppy.Business.Services;

public sealed class SmtpEmailService(IOptions<EmailSettings> _emailSettings) : IEmailService
{
    public async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var settings = _emailSettings.Value;

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            Credentials = new NetworkCredential(settings.Email, settings.Password),
            EnableSsl = true
        };

        var mailMessage = new MailMessage(
            from: settings.SenderEmail,
            to: toEmail,
            subject: subject,
            body: body)
        {
            IsBodyHtml = true
        };

        // Note: SmtpClient doesn't have a cancellationToken overload for SendMailAsync, 
        // but we'll use SendMailAsync().WaitAsync(cancellationToken) or simply await it.
        await client.SendMailAsync(mailMessage, cancellationToken);
    }
}
