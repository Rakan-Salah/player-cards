using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace PlayerCards.Services
{
    public class RecaptchaValidator : IRecaptchaValidator
    {
        private readonly HttpClient _httpClient;
        private readonly RecaptchaSettings _settings;

        public RecaptchaValidator(HttpClient httpClient, IOptions<RecaptchaSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<bool> IsCaptchaPassedAsync(string? token, string? remoteIp = null)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(_settings.SecretKey))
                return false;

            var url = $"https://www.google.com/recaptcha/api/siteverify" +
                      $"?secret={_settings.SecretKey}&response={token}" +
                      (string.IsNullOrWhiteSpace(remoteIp) ? "" : $"&remoteip={remoteIp}");

            using var resp = await _httpClient.PostAsync(url, content: null);
            if (!resp.IsSuccessStatusCode) return false;

            var json = await resp.Content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<RecaptchaResponse>(json);

            return parsed?.Success == true;
        }
    }
}
