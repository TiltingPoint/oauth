using System;

namespace TiltingPoint.Auth {
    /// <summary>
    /// Class containing the tokens after a successful login.
    /// </summary>
    internal class InternalTokenResponse : TokenResponse
    {
        public DateTime ExpirationDate;
        public string UTCExpirationDate;
        public bool EmailVerified;
    }
}