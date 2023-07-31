using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace TiltingPoint.Auth
{
    /// <summary>
    /// Handles validation via Microsoft Identity Model Library.
    /// </summary>
    public class TokenValidator
    {
        private IConfigurationManager<OpenIdConnectConfiguration> _configManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenValidator"/> class.
        /// </summary>
        public TokenValidator()
        {
            IdentityModelEventSource.ShowPII = true;

            // LogHelper.Logger = new Logger();
        }

        /// <summary>
        /// Async Task for Token validation.
        /// </summary>
        /// <param name="jwtToken">Raw jwt token.</param>
        /// <param name="audience">Token audience.</param>
        /// <param name="configUrl">Configuration url from your identity provider.</param>
        /// <param name="validateAudience">Enable audience validation.</param>
        /// <returns>Tuple with the result of the validation and errors if any.</returns>
        public async Task<(bool IsValid, string Error)> IsValidToken(string jwtToken, string audience, string configUrl, bool validateAudience = true)
        {
            try
            {
                var openIdConnectConfig = await GetMetaData(configUrl);
                return ValidateToken(jwtToken, openIdConnectConfig.Issuer, audience, openIdConnectConfig.SigningKeys, validateAudience);
            }
            catch (Exception e)
            {
                return (false, $"General error during validation. {e.Message}");
            }
        }

        private IConfigurationManager<OpenIdConnectConfiguration> GetConfigurationManager(string configUrl)
        {
            return _configManager ?? new ConfigurationManager<OpenIdConnectConfiguration>(configUrl, new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever());
        }

        private Task<OpenIdConnectConfiguration> GetMetaData(string configUrl)
        {
            _configManager = GetConfigurationManager(configUrl);
            try
            {
                var metaData = _configManager.GetConfigurationAsync(default);
                return metaData;
            }
            catch (Exception e)
            {
                AuthUtils.DebugInnerException("Error getting metadata", 0, e);
            }

            return null;
        }

        private async Task<OpenIdConnectConfiguration> LoadOpenIdConnectConfigurationAsync(string url)
        {
            _configManager = GetConfigurationManager(url);
            return await _configManager.GetConfigurationAsync(default);
        }

        private static (bool IsValid, string Error) ValidateToken(string jwtToken, string issuer, string audience, ICollection<SecurityKey> signingKeys, bool validateAudience)
        {
            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    RequireSignedTokens = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = signingKeys,
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = validateAudience,
                    ValidAudience = audience
                };

                ISecurityTokenValidator tokenValidator = new JwtSecurityTokenHandler();
                tokenValidator.ValidateToken(jwtToken, validationParameters, out var _);
                return (true, null);
            }
            catch (SecurityTokenException e)
            {
                return (false, $"Token did not validate: {e.Message}");
            }
            catch (Exception e)
            {
                return (false, $"Error during token validation: {e.Message}");
            }
        }
    }
}