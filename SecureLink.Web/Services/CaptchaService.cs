using SkiaSharp;
using System;

namespace SecureLink.Web.Services
{
    public class CaptchaService
    {
        private static readonly Random random = new();
        private readonly ILogger<CaptchaService> _logger;

        public CaptchaService(ILogger<CaptchaService> logger)
        {
            _logger = logger;
        }

        public (byte[], string) GenerateCaptcha()
        {
            _logger.LogInformation("[CaptchaService] Začínám generovat CAPTCHA.");

            string captchaText = GenerateRandomText();
            _logger.LogInformation("[CaptchaService] Vygenerovaný text pro CAPTCHA: {CaptchaText}", captchaText);

            byte[] imageBytes = GenerateCaptchaImage(captchaText);
            _logger.LogInformation("[CaptchaService] CAPTCHA obrázek vygenerován.");

            return (imageBytes, captchaText);
        }

        private string GenerateRandomText()
        {
            _logger.LogDebug("[CaptchaService] Generuji náhodný text pro CAPTCHA.");

            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            char[] stringChars = new char[5];

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            string captchaText = new string(stringChars);
            _logger.LogDebug("[CaptchaService] Vygenerovaný CAPTCHA text: {CaptchaText}", captchaText);

            return captchaText;
        }

        private byte[] GenerateCaptchaImage(string text)
        {
            _logger.LogDebug("[CaptchaService] Vytvářím CAPTCHA obrázek pro text: {CaptchaText}", text);

            int width = 150;
            int height = 50;

            using SKBitmap bitmap = new(width, height);
            using SKCanvas canvas = new(bitmap);
            canvas.Clear(SKColors.White);

            using SKPaint textPaint = new()
            {
                Color = SKColors.Black,
                TextSize = 30,
                IsAntialias = true
            };

            canvas.DrawText(text, 20, 35, textPaint);

            using SKImage image = SKImage.FromBitmap(bitmap);
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

            _logger.LogDebug("[CaptchaService] CAPTCHA obrázek úspěšně vytvořen.");
            return data.ToArray();
        }
    }
}
