using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace PlayerCards.Middleware
{
    public class FileValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string[] _blockedExtensions =
            { ".exe", ".bat", ".cmd", ".sh", ".js", ".msi", ".vbs" };

        public FileValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.HasFormContentType)
            {
                var form = await context.Request.ReadFormAsync();

                foreach (var file in form.Files)
                {
                    var fileName = Path.GetFileName(file.FileName).ToLowerInvariant();

                    // Block dangerous extensions
                    if (_blockedExtensions.Any(ext => fileName.EndsWith(ext)))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync($"File type not allowed: {fileName}");
                        return; // stop pipeline
                    }

                    // Sanitize weird double extensions (image.png.exe → image.png)
                    var safeName = Regex.Replace(
                        fileName,
                        @"(\.exe|\.bat|\.cmd|\.sh|\.js|\.msi|\.vbs)$",
                        "",
                        RegexOptions.IgnoreCase);

                    if (safeName != fileName)
                    {
                        // Store sanitized filename so controllers can use it
                        context.Items["SanitizedFileName_" + file.Name] = safeName;
                    }
                }
            }

            await _next(context);
        }
    }

    // Extension method for cleaner usage
    public static class FileValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseFileValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<FileValidationMiddleware>();
        }
    }
}
