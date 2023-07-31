using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using static TiltingPoint.Auth.Consts;
#if TP_OPS
using TiltingPoint.Ops;
#endif

namespace TiltingPoint.Auth
{
    internal class ControllerUtils
    {
        private TPAuthConfig _config;

        private Func<TokenResponse, bool> _isUserEmailVerified;

        private Action<InternalTokenResponse> _checkEmailVerification;

        private readonly Lazy<Dictionary<string, object>> _authSettings = new Lazy<Dictionary<string, object>>(() =>
        {
            #if TP_OPS
            return OPS.GetRemoteConfig("Auth");
            #else
            return new Dictionary<string, object>
            {
                ["refresh_safe_time_in_minutes"] = REFRESH_TIME_IN_MINUTES
            };
            #endif
        });

        internal ControllerUtils(TPAuthConfig config)
        {
            _config = config;
        }

        internal bool IsAuthenticationValid()
        {
            var savedTokens = GetSavedToken(MAIN_TOKEN);
            if (string.IsNullOrEmpty(savedTokens))
            {
                return false;
            }

            var tokenResponse = ParseInternalTokens(savedTokens);
            if (tokenResponse == null)
            {
                return false;
            }

            if (!IsLaterThanNow(tokenResponse.ExpirationDate))
            {
                return true;
            }

            var refreshExpiration = AuthUtils.GetTokenExpirationDate(tokenResponse.RefreshToken);
            return !IsLaterThanNow(refreshExpiration);
        }

        internal bool IsLaterThanNow(DateTime expiration)
        {
            var safeTime = Convert.ToInt32(_authSettings.Value["refresh_safe_time_in_minutes"]);
            return DateTime.Compare(expiration, DateTime.Now.AddMinutes(safeTime)) < 0;
        }

        internal void ValidateAndContinue(TokenResponse tokens, string tokenToValidate, Action<TokenResponse> cbk)
        {
            var tokenValid = AuthUtils.ValidateToken(tokenToValidate, _config.clientId, _config.issuer, error =>
            {
                cbk(new TokenResponse
                {
                    ErrorMessage = error,
                    Audience = tokens.Audience
                });
            }, tokens.IsMain);

            tokenValid.ContinueWith(task =>
            {
                if (task.Result)
                {
                    cbk(tokens);
                }
                else
                {
                    cbk(new TokenResponse
                    {
                        ErrorMessage = "Validation failed",
                        Audience = tokens.Audience
                    });
                }
            });
        }

        internal void SetupEmailCallbacks(Func<TokenResponse, bool> isUserEmailVerified, Action<InternalTokenResponse> checkEmailVerification)
        {
            _isUserEmailVerified = isUserEmailVerified;
            _checkEmailVerification = checkEmailVerification;
        }

        internal void TokenSuccessResponse(string responseText, Action<TokenResponse> cbk, string audience = MAIN_TOKEN)
        {
            var tokenDict = CheckIfValidResponse(responseText);

            if (tokenDict == null)
            {
                cbk(new TokenResponse
                {
                    ErrorMessage = $"Malformed exchange response: {responseText}",
                    Audience = audience
                });
                return;
            }

            var expirationDate = DateTime.Now.AddSeconds(Convert.ToDouble(tokenDict["expires_in"].ToString()));
            var utcExpirationDate = expirationDate.ToUniversalTime()
                .ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            var tokenResponse = new InternalTokenResponse
            {
                AccessToken = tokenDict["access_token"].ToString(),
                RefreshToken = tokenDict["refresh_token"].ToString(),
                IdToken = tokenDict.ContainsKey("id_token") ? tokenDict["id_token"].ToString() : string.Empty,
                ExpirationDate = expirationDate,
                UTCExpirationDate = utcExpirationDate,
                Audience = audience
            };

            if (tokenResponse.IsMain)
            {
                tokenResponse.EmailVerified = _isUserEmailVerified(tokenResponse);
                _checkEmailVerification(tokenResponse);
            }

            SaveTokens(tokenResponse);
            var tokenToValidate = tokenResponse.IsMain ? tokenResponse.IdToken : tokenResponse.AccessToken;
            ValidateAndContinue(tokenResponse, tokenToValidate, cbk);
        }

        internal static InternalTokenResponse ParseInternalTokens(string s)
        {
            InternalTokenResponse tokens = null;
            try
            {
                tokens = JsonUtility.FromJson<InternalTokenResponse>(s);
                if (!string.IsNullOrEmpty(tokens.UTCExpirationDate))
                {
                    tokens.ExpirationDate = DateTime.ParseExact(tokens.UTCExpirationDate, "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TP Auth]: Error parsing AppAuth response: {e.Message}. Original response: {s}");
            }

            return tokens;
        }

        internal static void SaveTokens(InternalTokenResponse tokens)
        {
            var tokenDict = GetTokenStorage();
            tokenDict[tokens.Audience] = JsonUtility.ToJson(tokens);
            var stringyfiedDict = Json.Serialize(tokenDict);
            EncryptedPlayerPrefs.SetString(STORAGE_KEY, stringyfiedDict);
        }

        private static Dictionary<string, object> CheckIfValidResponse(string response)
        {
            return Json.Deserialize(response) is Dictionary<string, object> tokenDict &&
                   (tokenDict.ContainsKey("access_token") || tokenDict.ContainsKey("refresh_token"))
                ? tokenDict
                : null;
        }

        internal static string GetSavedToken(string audience)
        {
            var tokenDict = GetTokenStorage();
            if (tokenDict == null)
            {
                return null;
            }

            if (tokenDict.ContainsKey(audience))
            {
                return tokenDict[audience] != null ? tokenDict[audience].ToString() : null;
            }

            return null;
        }

        private static Dictionary<string, object> GetTokenStorage()
        {
            var savedTokens = EncryptedPlayerPrefs.GetString(STORAGE_KEY);
            if (string.IsNullOrEmpty(savedTokens))
            {
                return new Dictionary<string, object>();
            }

            if (!(Json.Deserialize(savedTokens) is Dictionary<string, object> tokenDict))
            {
                return new Dictionary<string, object>();
            }

            return tokenDict;
        }
    }
}