using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using System.Security.Claims;

namespace SecureLink.Api
{
    public class UserTypeAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly string[] _allowedUserTypes;

        public UserTypeAuthorizeAttribute(params string[] allowedUserTypes)
        {
            _allowedUserTypes = allowedUserTypes;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            Log.Information("User Identity: {Identity}, IsAuthenticated: {IsAuthenticated}",
                user.Identity?.Name, user.Identity?.IsAuthenticated);

            if (user.Identity == null || !user.Identity.IsAuthenticated)
            {
                Log.Warning("Unauthorized access attempt.");
                context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var userTypeClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            Log.Information("UserType Claim: {UserType}", userTypeClaim);

            if (string.IsNullOrEmpty(userTypeClaim) || !_allowedUserTypes.Contains(userTypeClaim))
            {
                Log.Warning("Access denied. UserType: {UserType}, Allowed: {Allowed}", userTypeClaim, string.Join(", ", _allowedUserTypes));
                context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Result = new Microsoft.AspNetCore.Mvc.JsonResult(new { error = "Access denied" });
            }
        }

    }
}
