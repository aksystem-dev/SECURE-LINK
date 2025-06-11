using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using SecureLink.Api.Services;
using Serilog;

namespace SecureLink.Api
{
    public class NonceAuthorizeAttribute : TypeFilterAttribute
    {
        public NonceAuthorizeAttribute() : base(typeof(NonceAuthorizeFilter))
        {
        }
    }

    public class NonceAuthorizeFilter : IAuthorizationFilter
    {
        private readonly NonceCache _nonceCache;

        public NonceAuthorizeFilter(NonceCache nonceCache)
        {
            _nonceCache = nonceCache;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();

            if (!user.Identity.IsAuthenticated)
            {
                Log.Warning("[NonceAuthorizeAttribute - OnAuthorization] - Unauthorized access: User is not authorized");
                context.Result = new UnauthorizedResult();
                return;
            }

            var nonce = user.Claims.FirstOrDefault(c => c.Type == "nonce")?.Value;

            if (string.IsNullOrEmpty(nonce) || string.IsNullOrEmpty(ipAddress))
            {
                Log.Warning("[NonceAuthorizeAttribute - OnAuthorization] - Unauthorized access: missing nonce or IP address. nonce: {Nonce}, IP: {IPAddress}", nonce, ipAddress);
                context.Result = new UnauthorizedResult();
                return;
            }

            if (!_nonceCache.ValidateNonce(ipAddress, nonce))
            {
                Log.Warning("[NonceAuthorizeAttribute - OnAuthorization] - Validation failed for IP: {IPAddress}, nonce: {Nonce}", ipAddress, nonce);
                context.Result = new UnauthorizedResult();
            }
            else
            {
                Log.Information("[NonceAuthorizeAttribute - OnAuthorization] - Nonce validation was successfull for IP: {IPAddress}, nonce: {Nonce}", ipAddress, nonce);
            }
        }
    }
}
