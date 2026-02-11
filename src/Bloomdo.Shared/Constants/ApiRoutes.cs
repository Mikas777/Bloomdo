namespace Bloomdo.Shared.Constants;

public static class ApiRoutes
{
    public const string Base = "api";

    public static class Auth
    {
        private const string BaseRoute = $"{Base}/auth";
        
        public const string Register = $"{BaseRoute}/register";
        public const string Login = $"{BaseRoute}/login";
        public const string Refresh = $"{BaseRoute}/refresh";
        public const string Revoke = $"{BaseRoute}/revoke";
        public const string Me = $"{BaseRoute}/me";
    }
}
