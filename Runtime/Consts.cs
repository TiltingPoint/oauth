namespace TiltingPoint.Auth
{
    public static class Consts
    {
        public const string EXCHANGE_GRANT_TYPE = "urn:ietf:params:oauth:grant-type:token-exchange";
        public const string RPT_GRANT_TYPE = "urn:ietf:params:oauth:grant-type:uma-ticket";
        public const string REFRESH_GRANT_TYPE = "refresh_token";
        public const string DEFAULT_SCOPE = "openid";
        public const string STORAGE_KEY = "TP_AUTH_TOKENS";
        public const string UNSUPPORTED_ACTION_MSG = "This action is not supported in your current platform.";
        public const string JWT_SUBJECT_TOKEN_TYPE = "urn:ietf:params:oauth:token-type:id_token";
        public const string SERVICE_NAME = "auth_package";
        public const string MAIN_TOKEN = "main";
        public const string CONFIG_SUFFIX = ".well-known/openid-configuration";
        public const int REFRESH_TIME_IN_MINUTES = 1;
        public const string AUTH_FIRST_ERROR = "No valid credentials found, please authenticate your user.";
        public const int DEFAULT_TIME_OUT = 10;
        public const string DISPATCHER_NAME = "AuthThreadDispatcher";

    }
}