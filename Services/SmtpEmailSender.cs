using Microsoft.Extensions.Options;
using PlayerCards.Services;
using System.Net;
using System.Net.Mail;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;

    public SmtpEmailSender(IOptions<SmtpSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(to)) throw new ArgumentException("to");
        if (string.IsNullOrWhiteSpace(_settings.SenderEmail)) throw new InvalidOperationException("Sender email not configured.");

        using var message = new MailMessage();
        message.From = new MailAddress(_settings.SenderEmail, _settings.SenderName);
        message.To.Add(new MailAddress(to));
        message.Subject = subject;
        message.Body = htmlBody;
        message.IsBodyHtml = true;

        using var client = new SmtpClient(_settings.Server, _settings.Port)
        {
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
            EnableSsl = true // for port 587 use STARTTLS; EnableSsl = true is fine
        };

        // Important: exceptions will bubble up if send fails; caller can catch
        await client.SendMailAsync(message);
    }
}
