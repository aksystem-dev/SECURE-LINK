using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SecureLink.Shared
{
        public static class EncryptionUtility
        {
            private static readonly string EncryptionKey = "P9yBZkuwHd9lRUbxAdsatX82sx5w9KSWMWcRbennyVSeu42y8X";
            private const string Prefix = "ENC$";


        public static string Encrypt(string plainText, string salt)
        {
            using (var aes = Aes.Create())
            {
                using (var sha256 = SHA256.Create())
                {
                    aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(EncryptionKey + salt));
                }

                aes.IV = GenerateIV();

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cs))
                    {
                        writer.Write(plainText);
                    }

                    string encrypted = Convert.ToBase64String(ms.ToArray());
                    string prefixedEncrypted = $"{Prefix}{encrypted}";

                    return prefixedEncrypted;
                }
            }
        }


        public static string Decrypt(string encryptedText, string salt)
        {
            if (!encryptedText.StartsWith(Prefix))
            {
                throw new ArgumentException("Chybí prefix 'ENC$', nemůže být dešifrováno.");
            }

            string base64Text = encryptedText.Substring(Prefix.Length);
            var cipherBytes = Convert.FromBase64String(base64Text);

            using (var aes = Aes.Create())
            {
                using (var sha256 = SHA256.Create())
                {
                    aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(EncryptionKey + salt));
                }

                var iv = new byte[16];
                Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream(cipherBytes, 16, cipherBytes.Length - 16))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cs))
                {
                    string decrypted = reader.ReadToEnd();
                    return decrypted;
                }
            }
        }

        private static byte[] GenerateIV()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var iv = new byte[16];
                rng.GetBytes(iv);
                return iv;
            }
        }
    }
}

