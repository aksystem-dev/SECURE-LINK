using Serilog;
using System.Text;

namespace SecureLink.Api
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        // Maximální délka zaznamenaného těla (např. 1024 znaků)
        private const int MaxBodyLengthToLog = 1024;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string logPrefix = "[RequestLoggingMiddleware - InvokeAsync] - ";

            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Neznámá IP";
            var method = context.Request.Method;
            var path = context.Request.Path;
            var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;

            Log.Information($"{logPrefix}Request {method} {path}{queryString} from {ipAddress}");

            foreach (var header in context.Request.Headers)
            {
                if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    var headerValue = header.Value.ToString();
                    string maskedValue;
                    if (headerValue.Length > 5)
                    {
                        maskedValue = new string('*', headerValue.Length - 5) + headerValue.Substring(headerValue.Length - 5);
                    }
                    else
                    {
                        maskedValue = headerValue;
                    }
                    Log.Debug($"{logPrefix}Header: {header.Key} = {maskedValue}");
                }
                else
                {
                    Log.Debug($"{logPrefix}Header: {header.Key} = {header.Value}");
                }
            }

            context.Request.EnableBuffering();

            string requestBody;
            if (IsBinaryContent(context.Request.ContentType))
            {
                requestBody = $"[binary content of length {context.Request.ContentLength ?? 0} bytes, not logged]";
            }
            else
            {
                requestBody = await ReadRequestBodyAsync(context.Request);
                if (requestBody.Length > MaxBodyLengthToLog)
                {
                    requestBody = requestBody.Substring(0, MaxBodyLengthToLog) + "... [truncated]";
                }
            }
            Log.Information($"{logPrefix}Request Body: {requestBody}");

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{logPrefix}An exception occurred while processing the request from {ipAddress}.");
                throw;
            }
        }

        private async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            body = MaskSensitiveFields(body);

            return body;
        }

        private string MaskSensitiveFields(string body)
        {
            var sensitiveKeys = new[] { "password", "pass", "heslo" };

            foreach (var key in sensitiveKeys)
            {
                body = System.Text.RegularExpressions.Regex.Replace(
                    body,
                    @$"(""{key}""\s*:\s*"")[^""]+(""|\s*)",
                    m => $"{m.Groups[1].Value}******{m.Groups[2].Value}",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
            }

            return body;
        }

        private bool IsBinaryContent(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            var lower = contentType.ToLowerInvariant();

            if (lower.StartsWith("text/") || lower.Contains("json") || lower.Contains("xml") || lower.Contains("urlencoded"))
            {
                return false;
            }
            return true;
        }
    }
}
