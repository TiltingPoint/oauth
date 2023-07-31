using static TiltingPoint.Auth.Consts;
namespace TiltingPoint.Auth
{
    /// <summary>
    /// Class containing the tokens after a successful login.
    /// </summary>
    public class TokenResponse
    {
        public string AccessToken;
        public string IdToken;
        public string RefreshToken;
        public string Audience;
        public string ErrorMessage;

        public bool IsMain => string.IsNullOrEmpty(Audience) || Audience == MAIN_TOKEN;
    }
}