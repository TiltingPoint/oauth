using System;
using System.Collections.Generic;
using TiltingPoint.Auth.Events;
using UnityEngine;
using UnityEngine.Networking;
using static TiltingPoint.Auth.Consts;

namespace TiltingPoint.Auth
{
    internal class TokenRequestController
    {
        private readonly TPAuthConfig _config;

        private readonly ControllerUtils _utils;

        private readonly Dictionary<string, Queue<Action<TokenResponse>>> _tokenRequests =
            new Dictionary<string, Queue<Action<TokenResponse>>>();

        internal event Action<TokenResponse> OnTokenRequestDidSucceed;

        internal event Action<string> OnTokenRequestDidFail;

        internal TokenRequestController(TPAuthConfig config, ControllerUtils utils)
        {
            _config = config;
            _utils = utils;
        }

        internal void TokenRequest(bool forceRefresh)
        {
            TokenRequest(MAIN_TOKEN, forceRefresh);
        }

        internal void TokenRequest(Action<TokenResponse> cbk, bool forceRefresh)
        {
            TokenRequest(MAIN_TOKEN, cbk, forceRefresh);
        }

        internal void TokenRequest(string audience, bool forceRefresh)
        {
            TokenRequest(audience, response =>
            {
                // Queue default event if no callback provided.
                if (string.IsNullOrEmpty(response.ErrorMessage))
                {
                    TokenRequestDidSucceed(response);
                }
                else
                {
                    TokenRequestDidFail(response.ErrorMessage, response.Audience);
                }
            }, forceRefresh);
        }

        internal void TokenRequest(string audience, Action<TokenResponse> cbk, bool forceRefresh)
        {
            if (!_utils.IsAuthenticationValid())
            {
                AuthEventTracker.TokenRequestWillStart(audience);
                cbk(new TokenResponse
                {
                    ErrorMessage = AUTH_FIRST_ERROR
                });
                AuthEventTracker.TokenRequestDidFail(AUTH_FIRST_ERROR, audience);
                return;
            }

            if (!_tokenRequests.ContainsKey(audience))
            {
                _tokenRequests.Add(audience, new Queue<Action<TokenResponse>>());
            }

            var queue = _tokenRequests[audience];
            var first = queue.Count == 0;
            if (first)
            {
                // Add tracking to the first of the stacked requests.
                AuthEventTracker.TokenRequestWillStart(audience);
                cbk += response =>
                {
                    if (string.IsNullOrEmpty(response.ErrorMessage))
                    {
                        AuthEventTracker.TokenRequestDidSucceed(audience);
                    }
                    else
                    {
                        AuthEventTracker.TokenRequestDidFail(response.ErrorMessage, audience);
                    }
                };
            }

            queue.Enqueue(cbk);
            if (!first)
            {
                return;
            }

            if (audience == MAIN_TOKEN)
            {
                InternalMainTokenRequest(forceRefresh);
            }
            else
            {
                InternalRptTokenRequest(audience, forceRefresh);
            }
        }

        private void RefreshAccessToken(TokenResponse tokens, Action<TokenResponse> cbk)
        {
            if (_utils.IsLaterThanNow(AuthUtils.GetTokenExpirationDate(tokens.RefreshToken)))
            {
                cbk(new TokenResponse
                    { ErrorMessage = "Authentication tokens have expired. Please authenticate again." });
                return;
            }

            var form = new WWWForm();
            form.AddField("client_id", _config.clientId);
            form.AddField("grant_type", REFRESH_GRANT_TYPE);
            form.AddField("refresh_token", tokens.RefreshToken);
            form.AddField("scope", DEFAULT_SCOPE);

            var request = UnityWebRequest.Post(_config.tokenUrl, form);
            request.timeout = DEFAULT_TIME_OUT;

            var requestAction = request.SendWebRequest();

            requestAction.completed += operation =>
            {
                #if UNITY_2020_1_OR_NEWER
                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    #else
                 if (request.isNetworkError || request.isHttpError)
                {
                    #endif
                    cbk(new TokenResponse
                    {
                        ErrorMessage = $"Token request failed. Code: {request.responseCode}. " +
                                       $"Error: {request.error}. " +
                                       $"Body: {(request.downloadHandler == null ? "No error information." : request.downloadHandler.text)}"
                    });
                }
                else
                {
                    _utils.TokenSuccessResponse(request.downloadHandler.text, cbk);
                }
            };
        }

        private void ResolveTokenRequests(TokenResponse response)
        {
            var audience = response.Audience;
            var error = string.Empty;
            if (string.IsNullOrEmpty(audience))
            {
                error = response.ErrorMessage ?? "Unknown error";
                audience = "Unknown";
            }
            else if (!_tokenRequests.ContainsKey(audience))
            {
                error = $"Server response does not match audience: {audience}";
                Debug.LogError($"[TP AUTH] Internal error. {error}");
                AuthEventTracker.AuthInternalError(error);
            }

            if (!string.IsNullOrEmpty(error))
            {
                AuthEventTracker.TokenRequestDidFail(error, audience);
                TokenRequestDidFail(error, audience);
                return;
            }

            var requestQueue = _tokenRequests[audience];
            while (requestQueue.Count > 0)
            {
                var action = requestQueue.Dequeue();
                AuthThreadDispatcher.Enqueue(action, response);
            }
        }

        private void InternalMainTokenRequest(bool forceRefresh)
        {
            var savedTokens = ControllerUtils.GetSavedToken(MAIN_TOKEN);
            if (string.IsNullOrEmpty(savedTokens))
            {
                ResolveTokenRequests(new TokenResponse
                {
                    ErrorMessage = AUTH_FIRST_ERROR,
                    Audience = MAIN_TOKEN
                });
                return;
            }

            var tokenResponse = ControllerUtils.ParseInternalTokens(savedTokens);
            if (tokenResponse == null)
            {
                ResolveTokenRequests(new TokenResponse
                {
                    ErrorMessage = "Error retrieving the tokens. Please authenticate again.",
                    Audience = MAIN_TOKEN
                });
                return;
            }

            if (!forceRefresh && !_utils.IsLaterThanNow(tokenResponse.ExpirationDate))
            {
                Debug.Log("[TP AUTH] Tokens are fresh.");
                _utils.ValidateAndContinue(tokenResponse, tokenResponse.IdToken, ResolveTokenRequests);
            }
            else
            {
                Debug.Log("[TP AUTH] Refreshing tokens.");
                RefreshAccessToken(tokenResponse, ResolveTokenRequests);
            }
        }

        private void InternalRptTokenRequest(string audience, bool forceRefresh)
        {
            // Check if valid tokens are available.
            var savedTokens = ControllerUtils.GetSavedToken(audience);

            if (!string.IsNullOrEmpty(savedTokens))
            {
                var tokenResponse = ControllerUtils.ParseInternalTokens(savedTokens);
                if (!forceRefresh && !_utils.IsLaterThanNow(tokenResponse.ExpirationDate))
                {
                    Debug.Log($"[TP AUTH] {audience} tokens are fresh.");
                    ResolveTokenRequests(tokenResponse);
                    return;
                }
            }

            //Request new tokens if no tokens cached.
            TokenRequest(
                mainTokens =>
                {
                var form = new WWWForm();
                form.AddField("grant_type", RPT_GRANT_TYPE);
                form.AddField("audience", audience);

                var request = UnityWebRequest.Post(_config.tokenUrl, form);
                request.timeout = DEFAULT_TIME_OUT;
                request.SetRequestHeader("Authorization", "Bearer " + mainTokens.AccessToken);

                var requestAction = request.SendWebRequest();
                requestAction.completed += operation => {
                    #if UNITY_2020_1_OR_NEWER
                    if (request.result == UnityWebRequest.Result.ConnectionError ||
                        request.result == UnityWebRequest.Result.ProtocolError) {
                        #else
                if (request.isNetworkError || request.isHttpError) {
                        #endif
                        var error = $"Token {audience} request failed. Code: {request.responseCode}. " +
                                    $"Error: {request.error}. " +
                                    $"Body: {(request.downloadHandler == null ? "No error information." : request.downloadHandler.text)}";
                        ResolveTokenRequests(new TokenResponse() {
                            ErrorMessage = error,
                            Audience = audience
                        });
                    }
                    else
                    {
                        _utils.TokenSuccessResponse(request.downloadHandler.text, ResolveTokenRequests, audience);
                    }
                };
            }, forceRefresh);
        }

        private void TokenRequestDidSucceed(TokenResponse tokens)
        {
            if (OnTokenRequestDidSucceed != null)
            {
                AuthThreadDispatcher.Enqueue(OnTokenRequestDidSucceed, tokens);
            }
        }

        private void TokenRequestDidFail(string errorMessage, string audience)
        {
            if (OnTokenRequestDidFail != null)
            {
                AuthThreadDispatcher.Enqueue(OnTokenRequestDidFail, errorMessage);
            }
        }
    }
}