using System.Net;
using System.Net.Mail;

namespace PlayerCards.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
}
