using System;
using TiltingPoint.Auth.Events;
using UnityEngine;
using UnityEngine.Networking;
using static TiltingPoint.Auth.Consts;

namespace TiltingPoint.Auth
{
    internal class TokenExchangeController
    {
        private readonly TPAuthConfig _config;

        private readonly ControllerUtils _utils;

        internal event Action<TokenResponse> OnTokenExchangeDidSucceed;

        internal event Action<string> OnTokenExchangeDidFail;

        internal TokenExchangeController(TPAuthConfig config, ControllerUtils utils)
        {
            _config = config;
            _utils = utils;
        }

        internal void TokenExchange(string token, string issuer)
        {
            AuthEventTracker.TokenExchangeWillStart(issuer);

            var form = new WWWForm();
            form.AddField("client_id", _config.clientId);
            form.AddField("grant_type", EXCHANGE_GRANT_TYPE);
            form.AddField("subject_token", token);
            form.AddField("subject_issuer", issuer);
            form.AddField("scope", DEFAULT_SCOPE);

            //TODO: Improve this
            if (issuer == "apple")
            {
                form.AddField("subject_token_type", JWT_SUBJECT_TOKEN_TYPE);
            }

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
                    TokenExchangeDidFail($"Token exchange failed. Code: {request.responseCode}. " +
                                         $"Error: {request.error}. " +
                                         $"Body: {(request.downloadHandler == null ? "No error information." : request.downloadHandler.text)}");
                }
                else
                {
                    _utils.TokenSuccessResponse(request.downloadHandler.text, TokenExchangeResponseHandler);
                }
            };
        }

        private void TokenExchangeResponseHandler(TokenResponse response)
        {
            if (string.IsNullOrEmpty(response.ErrorMessage))
            {
                TokenExchangeDidSucceed(response);
            }
            else
            {
                TokenExchangeDidFail(response.ErrorMessage);
            }
        }

        private void TokenExchangeDidSucceed(TokenResponse tokens)
        {
            AuthEventTracker.TokenExchangeDidSucceed();
            if (OnTokenExchangeDidSucceed != null)
            {
                AuthThreadDispatcher.Enqueue(OnTokenExchangeDidSucceed, tokens);
            }
        }

        private void TokenExchangeDidFail(string errorMessage)
        {
            AuthEventTracker.TokenExchangeDidFail(errorMessage);
            if (OnTokenExchangeDidFail != null)
            {
                AuthThreadDispatcher.Enqueue(OnTokenExchangeDidFail, errorMessage);
            }
        }
    }
}