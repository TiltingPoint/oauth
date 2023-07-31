using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Threading.Tasks;
using UnityEngine;
using static TiltingPoint.Auth.Consts;

namespace TiltingPoint.Auth
{
    public static class AuthUtils
    {
        private static TokenValidator _tokenValidator;
        private static JwtSecurityTokenHandler _handler;

        /// <summary>
        /// Validates an id token.
        /// </summary>
        /// <param name="jwtToken">Id token in JWT form.</param>
        /// <param name="audience">Token audience.</param>
        /// <param name="configUrl">Identify provider metadata url.</param>
        /// <param name="failureEvent">Event to trigger in case of failure.</param>
        /// <param name="validateAudience">Is audience needed to be validated.</param>
        /// <returns>Is a valid token.</returns>
        public static async Task<bool> ValidateToken(string jwtToken, string audience, string configUrl, Action<string> failureEvent, bool validateAudience = false)
        {
            _tokenValidator ??= new TokenValidator();

            var (isValid, item2) = await _tokenValidator.IsValidToken(jwtToken, audience, configUrl + CONFIG_SUFFIX, validateAudience);

            if (isValid)
            {
                return true;
            }

            failureEvent(item2);
            return false;
        }

        /// <summary>
        /// Loops inside an exception logging information. Useful for internal debugging.
        /// </summary>
        /// <param name="prefix">String prefix that will get added to the log message.</param>
        /// <param name="level">Level of inner exceptions deep.</param>
        /// <param name="e">Exception to debug.</param>
        public static void DebugInnerException(string prefix, int level, Exception e)
        {
            while (true)
            {
                Debug.Log($"{prefix} Exception level: {level} \nMessage: {e.Message}\nSource: {e.Source}\nTrace: {e.StackTrace}");
                if (e.InnerException != null)
                {
                    level = ++level;
                    e = e.InnerException;
                    continue;
                }

                break;
            }
        }

        /// <summary>
        /// Easy access to a claim inside a jwt token.
        /// </summary>
        /// <param name="token">Raw JWT token.</param>
        /// <param name="claimToSearch">Claim to search.</param>
        /// <param name="jwt">Optional. Only for reusing JwtSecurityToken.</param>
        /// <returns>Content of the claim if found. Otherwise will return null.</returns>
        public static string GetTokenClaim(string token, string claimToSearch, [Optional] JwtSecurityToken jwt)
        {
            string response = null;
            try
            {
                jwt ??= GetTokenHandler(token);
                response = jwt.Claims.First(claim => claim.Type == claimToSearch).Value;
            }
            catch (Exception e)
            {
                Debug.Log($"[TP AUTH] Couldn't find claim {claimToSearch}. Error: {e.Message}");
            }

            return response;
        }

        /// <summary>
        /// Easy access the list of claims inside a jwt token.
        /// </summary>
        /// <param name="token">Raw JWT token.</param>
        /// <param name="jwt">Optional. Only for reusing JwtSecurityToken.</param>
        /// <returns>List of claims. Will return null if failed to retrieve the list.</returns>
        public static IEnumerable<Claim> GetTokenClaims(string token, [Optional] JwtSecurityToken jwt)
        {
            try
            {
                jwt ??= GetTokenHandler(token);
                return jwt.Claims;
            }
            catch (Exception e)
            {
                Debug.Log($"[TP AUTH] Couldn't get claims. Error: {e.Message}");
            }

            return null;
        }

        /// <summary>
        /// Used to access all properties from a raw token.
        /// </summary>
        /// <param name="token">Raw JWT token.</param>
        /// <returns>JwtSecurityToken from a raw token.</returns>
        public static JwtSecurityToken GetTokenHandler(string token)
        {
            try
            {
                _handler ??= new JwtSecurityTokenHandler();
                return _handler.ReadJwtToken(token);
            }
            catch (Exception e)
            {
                Debug.Log($"[TP AUTH] Couldn't create a token handler from the token. Error: {e.Message}");
            }

            return new JwtSecurityToken();
        }

        /// <summary>
        /// Easy access to the token expiration date.
        /// </summary>
        /// <param name="token">Raw JWT token.</param>
        /// <param name="jwt">Jwt Security Token to avoid re-creating the object.</param>
        /// <returns>DateTime object representing the expiration date.</returns>
        public static DateTime GetTokenExpirationDate(string token, [Optional] JwtSecurityToken jwt)
        {
            try
            {
                jwt ??= GetTokenHandler(token);
                return jwt.ValidTo;
            }
            catch (Exception e)
            {
                Debug.Log($"[TP AUTH] Couldn't retrieve token expiration date. Error: {e.Message}");
            }

            return DateTime.MinValue;
        }
    }
}