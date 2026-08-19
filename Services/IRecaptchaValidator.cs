using System.Threading.Tasks;

namespace PlayerCards.Services
{
    public interface IRecaptchaValidator
    {
        Task<bool> IsCaptchaPassedAsync(string? token, string? remoteIp = null);
    }
}
