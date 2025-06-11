using SecureLink.Shared;

namespace SecureLink.Web.Services
{
    public class AuthConfigService
    {
        public string Username { get; }
        public string Password { get; }

        public AuthConfigService(IConfiguration configuration)
        {
            var authSettings = configuration.GetSection("AuthSettings");

            Username = EncryptionUtility.Decrypt(authSettings["Username"], authSettings["Salt"]);
            Password = EncryptionUtility.Decrypt(authSettings["Password"], authSettings["Salt"]);
        }
    }
}
