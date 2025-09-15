namespace metin2freebsdapi.Helpers;

public static class ValidateSecrets
{
    public static bool ValidateWebSecret(HttpContext httpContext)
    {
        var secret = httpContext.Request.Headers["WEB_API_SECRET"].FirstOrDefault();
        return secret == EnvironmentVariables.WebApiSecret;
    }
    
    public static bool ValidateAdminSecret(HttpContext httpContext)
    {
        var secret = httpContext.Request.Headers["ADMIN_API_SECRET"].FirstOrDefault();
        return secret == EnvironmentVariables.AdminApiSecret;
    }
}