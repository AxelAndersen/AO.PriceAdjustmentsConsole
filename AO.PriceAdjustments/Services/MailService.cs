using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace AO.PriceAdjustments.Services
{
    public class MailService : IMailService
    {
        private SmtpClient _smtpClient;
        private IConfiguration _config;

        public MailService(SmtpClient smtpClient, IConfiguration config)
        {
            _smtpClient = smtpClient;
            _config = config;
        }

        public void SendMail(string subject, string body, string to)
        {
            // async applied here, you can safely remove from the function signature
            Task t = Task.Run(async () =>
            {
                MailMessage msg = new MailMessage(
                 from: _config["Email:Smtp:From"],
                 to: to,
                 subject: subject,
                 body: body);

                await _smtpClient.SendMailAsync(msg);

            });

            t.Wait(); // Wait until the above task is complete, email is sent
        }
    }
}
