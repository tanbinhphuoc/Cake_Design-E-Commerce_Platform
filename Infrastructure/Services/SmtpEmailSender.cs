using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Application.Services;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        public SmtpEmailSender(IConfiguration config) => _config = config;

        public async Task SendAsync(string to, string subject, string body)
        {
            var host = _config["Smtp:Host"];
            var port = int.Parse(_config["Smtp:Port"] ?? "587");
            var user = _config["Smtp:User"];
            var pass = _config["Smtp:Pass"];
            var from = _config["Smtp:From"] ?? user;

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(user, pass)
            };
            var msg = new MailMessage(from, to, subject, body);
            await client.SendMailAsync(msg);
        }
    }
}
