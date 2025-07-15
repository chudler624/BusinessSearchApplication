using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace BusinessSearch.Services.Email
{
    public class SendGridEmailService : IEmailSender, IEmailService
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly ILogger<SendGridEmailService> _logger;

        public SendGridEmailService(IOptions<SendGridSettings> options, ILogger<SendGridEmailService> logger)
        {
            _apiKey = options.Value.ApiKey;
            _fromEmail = options.Value.FromEmail;
            _fromName = options.Value.FromName;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(email);
            var msg = MailHelper.CreateSingleEmail(from, to, subject,
                                                  htmlMessage.Replace("<strong>", "").Replace("</strong>", ""),
                                                  htmlMessage);

            var response = await client.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to send email to {email}. Status code: {response.StatusCode}");
                // Log more detailed error information
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError($"SendGrid error details: {responseBody}");
            }
        }
    }

    public class SendGridSettings
    {
        public string ApiKey { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
    }
}