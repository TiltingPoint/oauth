using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TiltingPoint.Auth
{
    public class TokenHandler
    {
        private IEnumerable<Claim> _claims;
        private string _email;
        private bool _emailVerified;
        private string _id;
        private JwtSecurityToken _jwtToken;
        private DateTime? _tokenExpirationDate;
        private string _tpUserId;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenHandler"/> class.
        /// Provides an interface for easy access to the token main properties.
        /// </summary>
        /// <param name="token">JWT token string.</param>
        /// <param name="tokenType">Specify the token type.</param>
        public TokenHandler(string token, string tokenType)
        {
            Raw = token;
            TokenType = tokenType;
        }

        /// <summary>
        /// Gets the raw form of the token.
        /// </summary>
        public string Raw { get; }

        /// <summary>
        /// Gets the specified type of the token.
        /// </summary>
        public string TokenType { get; }

        /// <summary>
        /// Gets a IEnumerable that contains all token claims.
        /// </summary>
        public IEnumerable<Claim> Claims => JwtToken.Claims;

        /// <summary>
        /// Gets the expiration date from the token.
        /// </summary>
        public DateTime? TokenExpirationDate => _tokenExpirationDate ??= AuthUtils.GetTokenExpirationDate(Raw, JwtToken);

        /// <summary>
        /// Gets the Security Token object for easy access to token validation.
        /// </summary>
        public JwtSecurityToken JwtToken => _jwtToken ??= AuthUtils.GetTokenHandler(Raw);

        /// <summary>
        /// Gets the jti claim from the token used as an id.
        /// </summary>
        public string Id => _id ??= AuthUtils.GetTokenClaim(Raw, "jti", JwtToken);

        /// <summary>
        /// Gets the email from the token.
        /// </summary>
        public string Email => _email ??= AuthUtils.GetTokenClaim(Raw, "email", JwtToken);

        /// <summary>
        /// Gets a boolean representing if the email has been verified.
        /// </summary>
        public bool EmailVerified => Convert.ToBoolean(AuthUtils.GetTokenClaim(Raw, "email_verified", JwtToken));

        /// <summary>
        /// Gets the sub claim that represents the Tilting Point user id.
        /// </summary>
        public string TpUserId => _tpUserId ??= AuthUtils.GetTokenClaim(Raw, "sub", JwtToken);

        /// <summary>
        /// Gets an specific claim.
        /// </summary>
        /// <param name="claimToSearch">Name of the claim to search.</param>
        /// <returns>Returns the claim if found, null if not found.</returns>
        public string GetClaim(string claimToSearch)
        {
            return AuthUtils.GetTokenClaim(Raw, claimToSearch, JwtToken);
        }
    }
}