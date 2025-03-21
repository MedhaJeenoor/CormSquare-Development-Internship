using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http.HttpResults;
using MimeKit;
using System.Threading.Tasks;

namespace SupportHub.Utility.EmailServices
{
    public class EmailService
    {
        private readonly string _smtpServer = "smtp.gmail.com"; // Change as per your email provider
        private readonly int _smtpPort = 587;
        private readonly string _emailFrom = "hiremathniranjan822@gmail.com";
        private readonly string _emailPassword = "tdal iumj wjqu qdtl"; // Use secrets instead of hardcoding

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Support", _emailFrom));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            message.Body = new TextPart("html") { Text = body };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailFrom, _emailPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}