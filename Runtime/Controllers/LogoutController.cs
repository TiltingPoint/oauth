using System;
using TiltingPoint.Auth.Events;
using UnityEngine;
using UnityEngine.Networking;
using static TiltingPoint.Auth.Consts;

namespace TiltingPoint.Auth
{
    internal class LogoutController
    {
        internal event Action OnLogoutDidSucceed;

        internal event Action<string> OnLogoutDidFail;

        private readonly Action<string, Action<TokenResponse>, bool> _tokenRequest;

        private readonly TPAuthConfig _config;

        internal LogoutController(TPAuthConfig config, Action<string, Action<TokenResponse>, bool> tokenRequest) {
            _config = config;
            _tokenRequest = tokenRequest;
        }

        internal void Logout()
        {
            AuthEventTracker.LogoutWillStart();
            _tokenRequest(MAIN_TOKEN, response =>
            {
                if (!string.IsNullOrEmpty(response.ErrorMessage))
                {
                    LogoutDidFail("Unable to find valid credentials. Cleaning up credential storage.");
                    EncryptedPlayerPrefs.DeleteKey(STORAGE_KEY);
                    return;
                }

                var form = new WWWForm();
                form.AddField("client_id", _config.clientId);
                form.AddField("refresh_token", response.RefreshToken);

                var request = UnityWebRequest.Post(_config.logoutUrl, form);
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
                        LogoutDidFail(
                            "Logout request error.\n" +
                            $"Error: {request.error}. " +
                            $"Body: {(request.downloadHandler == null ? "No error information." : request.downloadHandler.text)}");
                        EncryptedPlayerPrefs.DeleteKey(STORAGE_KEY);
                    }
                    else
                    {
                        LogoutDidSucceed();
                        EncryptedPlayerPrefs.DeleteKey(STORAGE_KEY);
                    }
                };
            }, false);
        }

        private void LogoutDidSucceed()
        {
            AuthEventTracker.LogoutDidSucceed();
            AuthThreadDispatcher.Enqueue(OnLogoutDidSucceed);
        }

        private void LogoutDidFail(string errorMessage)
        {
            AuthEventTracker.LogoutDidFail(errorMessage);
            AuthThreadDispatcher.Enqueue(OnLogoutDidFail, errorMessage);
        }
    }
}