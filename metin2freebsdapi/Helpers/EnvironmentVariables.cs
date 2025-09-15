namespace metin2freebsdapi.Helpers;

internal static class EnvironmentVariables
{
    internal static readonly string AdminPageIp;
    internal static readonly string AdminPagePort;
    internal static readonly string AdminPagePassword;
    internal static readonly string AdminPageUserId;
    internal static readonly string AdminPageUserPassword;
    internal static readonly string AdminApiSecret;
    internal static readonly string WebApiSecret;
    internal static readonly string SqlAccount;
    internal static readonly string SqlPlayer;

    static EnvironmentVariables()
    {
        AdminPageIp = GetRequired("ADMIN_PAGE_IP");
        AdminPagePort = GetRequired("ADMIN_PAGE_PORT");
        AdminPagePassword = GetRequired("ADMIN_PAGE_PASSWORD");
        AdminPageUserId = GetRequired("ADMIN_PAGE_USER_ID");
        AdminPageUserPassword = GetRequired("ADMIN_PAGE_USER_PASSWORD");
        AdminApiSecret = GetRequired("ADMIN_API_SECRET");
        WebApiSecret = GetRequired("WEB_API_SECRET");
        SqlAccount = GetRequired("SQL_ACCOUNT");
        SqlPlayer = GetRequired("SQL_PLAYER");
    }

    private static string GetRequired(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrEmpty(value))
            throw new InvalidOperationException($"environment variable {key} is required");
        return value;
    }
}