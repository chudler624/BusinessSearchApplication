using System.Threading.Tasks;

namespace BusinessSearch.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}