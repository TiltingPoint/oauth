using System;
using TiltingPoint.Auth.Events;
using UnityEngine;
using UnityEngine.Networking;
using static TiltingPoint.Auth.Consts;

namespace TiltingPoint.Auth
{
    internal class EmailController
    {
        private static AuthEvents _eventHandler;

        private readonly TPAuthConfig _config;

        private readonly Action<string, Action<TokenResponse>, bool> _tokenRequest;

        private readonly ControllerUtils _utils;

        private bool _listeningToEmailChanges;

        internal event Action OnEmailVerificationDidSend;

        internal event Action OnEmailVerificationDidSucceed;

        internal event Action<string> OnEmailVerificationDidFail;

        internal EmailController(TPAuthConfig config, ControllerUtils utils, Action<string, Action<TokenResponse>, bool> tokenRequest, AuthEvents eventHandler)
        {
            _config = config;
            _utils = utils;
            _tokenRequest = tokenRequest;
            _eventHandler = eventHandler;
        }

        internal void SendVerificationEmail()
        {
            if (!_utils.IsAuthenticationValid())
            {
                EmailVerificationDidFail(AUTH_FIRST_ERROR);
                return;
            }

            _tokenRequest(MAIN_TOKEN, response =>
            {
                var form = new WWWForm();
                var handler = new TokenResponseHandler(response);
                if (handler.AccessToken.EmailVerified)
                {
                    EmailVerificationDidFail("User email is already verified.");
                    return;
                }

                AddEmailVerificationListener();

                var request = UnityWebRequest.Post(_config.verifyEmailUrl, form);
                request.SetRequestHeader("Authorization", "Bearer " + response.AccessToken);

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
                        EmailVerificationDidFail(
                            "Error sending the verification email.\n" +
                            $"Error: {request.error}. " +
                            $"Body: {(request.downloadHandler == null ? "No error information." : request.downloadHandler.text)}");
                    }
                    else
                    {
                        EmailVerificationDidSend();
                    }
                };
            }, true);
        }

        internal void CheckEmailVerification(InternalTokenResponse updatedTokens)
        {
            if (updatedTokens.Audience != MAIN_TOKEN)
            {
                return;
            }

            var savedTokens = ControllerUtils.GetSavedToken(MAIN_TOKEN);
            if (string.IsNullOrEmpty(savedTokens))
            {
                return;
            }

            var savedTokensParsed = ControllerUtils.ParseInternalTokens(savedTokens);
            if (savedTokensParsed == null)
            {
                return;
            }

            // Trigger the event after a change of the email verification state in the main token.
            if (!savedTokensParsed.EmailVerified && updatedTokens.EmailVerified)
            {
                RemoveVerificationListener();
                EmailVerificationDidSucceed();
            }
        }

        internal void IsUserEmailVerified(Action<bool> cbk, bool refresh)
        {
            _tokenRequest(MAIN_TOKEN, response => { cbk(IsUserEmailVerified(response)); }, refresh);
        }

        internal bool IsUserEmailVerified(TokenResponse response)
        {
            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                return false;
            }

            var accessToken = new TokenResponseHandler(response).AccessToken;
            return accessToken.EmailVerified;
        }

        private void EmailVerificationDidSend()
        {
            AuthEventTracker.EmailVerificationDidSend();
            if (OnEmailVerificationDidSend != null)
            {
                AuthThreadDispatcher.Enqueue(OnEmailVerificationDidSend);
            }
        }

        private void EmailVerificationDidSucceed()
        {
            AuthEventTracker.EmailVerificationDidSucceed();
            if (OnEmailVerificationDidSucceed != null)
            {
                AuthThreadDispatcher.Enqueue(OnEmailVerificationDidSucceed);
            }
        }

        private void EmailVerificationDidFail(string errorMessage)
        {
            AuthEventTracker.EmailVerificationDidFail(errorMessage);
            if (OnEmailVerificationDidFail != null)
            {
                AuthThreadDispatcher.Enqueue(OnEmailVerificationDidFail, errorMessage);
            }
        }

        private void AddEmailVerificationListener()
        {
            if (_listeningToEmailChanges)
            {
                return;
            }

            _listeningToEmailChanges = true;
            _eventHandler.GainedFocusEvent += RefreshSavedTokens;
        }

        private void RemoveVerificationListener()
        {
            if (!_listeningToEmailChanges)
            {
                return;
            }

            _eventHandler.GainedFocusEvent -= RefreshSavedTokens;
            _listeningToEmailChanges = false;
        }

        private void RefreshSavedTokens()
        {
            _tokenRequest(MAIN_TOKEN, response => { }, true);
        }
    }
}