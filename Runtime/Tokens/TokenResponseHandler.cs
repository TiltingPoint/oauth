using UnityEngine;

namespace TiltingPoint.Auth
{
    public class TokenResponseHandler
    {
        public readonly TokenHandler AccessToken;
        public readonly string Audience;

        public readonly TokenHandler IDToken;
        public readonly TokenHandler RefreshToken;

        public TokenResponseHandler(TokenResponse response)
        {
            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                Debug.Log("[TP Auth] Could not create a TokenResponseHandler from a TokenResponse containing errors.");
            }

            IDToken = new TokenHandler(response.IdToken, "id");
            AccessToken = new TokenHandler(response.AccessToken, "access");
            RefreshToken = new TokenHandler(response.RefreshToken, "refresh");
            Audience = response.Audience;
        }
    }
}