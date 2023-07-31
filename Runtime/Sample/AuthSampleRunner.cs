using System;
using System.Text;
//using Facebook.Unity;
//using TiltingPoint.Auth.Facebook;
//using TiltingPoint.Auth.Google;
//using TiltingPoint.Auth.SIWA;
using UnityEngine;
using UnityEngine.UI;

namespace TiltingPoint.Auth
{
    /// <summary>
    /// Class to showcase a basic implementation and testing.
    /// </summary>
    public class AuthSampleRunner : MonoBehaviour
    {
        [SerializeField]
        private Text debugText;
        [SerializeField]
        private InputField tokenInput;
        [SerializeField]
        private InputField rptInput;
        [SerializeField]
        private InputField issuerInput;

        /// <summary>
        /// Initializes Authenticator.
        /// </summary>
        public void InitializeBtn()
        {
            Log("Initializing...");
            Authenticator.Initialize();
        }

        /// <summary>
        /// Triggers the authentication flow.
        /// </summary>
        public void AuthorizeBtn()
        {
            Log("Authenticating...");
            Authenticator.Authenticate();
        }

        /// <summary>
        /// Performs a token request.
        /// </summary>
        public void TokenBtn()
        {
            Log("Requesting tokens...");
            Authenticator.TokenRequest();
        }

        /// <summary>
        /// Performs an RPT token request.
        /// </summary>
        public void RptTokenBtn()
        {
            var service = rptInput.text;
            if (string.IsNullOrEmpty(service))
            {
                Log("Please fill the Token audience before requesting one.");
            }
            else
            {
                Log($"Requesting {service} tokens...");
                Authenticator.TokenRequest(service);
            }
        }

        /// <summary>
        /// Triggers the token exchange flow.
        /// </summary>
        public void TokenExchangeBtn()
        {
            Log("Exchanging tokens...");

            var fbToken = tokenInput.text;
            if (string.IsNullOrEmpty(fbToken)) {
                Log("Please fill the Token input before exchanging.");
                return;
            }

            var issuer = issuerInput.text;
            if (string.IsNullOrEmpty(issuer)) Log("Please fill the Issuer input before exchanging.");

            Authenticator.TokenExchange(fbToken, issuer);
        }

        /// <summary>
        /// Logs if authentication is needed.
        /// </summary>
        public void IsAuthNeededBtn()
        {
            Log($"Do I need to authenticate the user? {Authenticator.NeedToAuth}");
        }

        /// <summary>
        /// Logs if the user email is verified.
        /// </summary>
        public void IsEmailVerifiedBtn()
        {
            Authenticator.IsUserEmailVerified(verified => { Log($"Is User Email verified? {verified}"); });
        }

        /// <summary>
        /// Triggers the email verification flow.
        /// </summary>
        public void SendVerificationEmailButton()
        {
            Authenticator.SendVerificationEmail();
        }

        /// <summary>
        /// Clears scene logs.
        /// </summary>
        public void ClearLog()
        {
            debugText.text = string.Empty;
        }

        /// <summary>
        /// Triggers login with Google flow.
        /// </summary>
        public void LoginGoogle()
        {
            Log("Login in with Google...");
            //Authenticator.Login(GoogleLogin.Identifier);
        }

        /// <summary>
        /// Triggers login with FB flow.
        /// </summary>
        public void LoginFacebook()
        {
            Log("Login in with Facebook...");
            //Authenticator.Login(FacebookLogin.Identifier);
        }

        /// <summary>
        /// Triggers login with Apple flow.
        /// </summary>
        public void LoginSIWA()
        {
            Log("Login in with SIWA...");
            //Authenticator.Login(SIWALogin.Identifier);
        }

        /// <summary>
        /// Logs out the user.
        /// </summary>
        public void LogOutBtn()
        {
            Log("LogOut button pressed.");
            Authenticator.Logout();
        }

        private void OnInitialized()
        {
            Log($"Auth initialized successfully.Do I need to auth? {Authenticator.NeedToAuth}");
        }

        private void Start()
        {
            #if TP_CORE
            SDK.DataConsent.Value = true;
            #endif

            // Subscribe to all Authenticator callbacks before initializing.
            Authenticator.OnInitDidSucceed += OnInitialized;
            Authenticator.OnInitDidFail += OnInitializedFailed;
            Authenticator.OnAuthDidSucceed += OnAuthSucceeded;
            Authenticator.OnAuthDidFail += OnAuthFailed;
            Authenticator.OnTokenExchangeDidSucceed += OnExchangeSucceeded;
            Authenticator.OnTokenExchangeDidFail += OnExchangeFailed;
            Authenticator.OnTokenRequestDidSucceed += OnRequestSucceeded;
            Authenticator.OnTokenRequestDidFail += OnRequestFailed;
            Authenticator.OnLoginDidSucceed += OnLoginSucceeded;
            Authenticator.OnLoginDidFail += OnLoginFailed;
            Authenticator.OnEmailVerificationDidSucceed += OnEmailVerificationDidSucceed;
            Authenticator.OnEmailVerificationDidSend += OnEmailVerificationDidSend;
            Authenticator.OnEmailVerificationDidFail += OnEmailVerificationDidFail;
            Authenticator.OnLogoutDidSucceed += OnLogoutDidSucceed;
            Authenticator.OnLogoutDidFail += OnLogoutDidFail;
        }

        private void OnInitializedFailed(string error)
        {
            Log($"Auth initialization failed: {error}");
        }

        private void OnAuthSucceeded(TokenResponse response)
        {
            Log("Auth Succeeded.\n" +
                $"Access Token: {response.AccessToken}\n" +
                $"Id Token: {response.IdToken}\n" +
                $"Refresh Token: {response.RefreshToken}\n");
        }

        private void OnRequestSucceeded(TokenResponse response)
        {
            Log("Token Request Succeeded");
            PrintAccessTokenDetails(response);
            PrintRefreshTokenDetails(response);
            PrintIdTokenDetails(response);
        }

        private void OnRequestFailed(string error)
        {
            Log($"Token Request failed: {error}");
        }

        private void OnAuthFailed(string error)
        {
            Log($"Auth failed: {error}");
        }

        private void OnExchangeSucceeded(TokenResponse response)
        {
            try
            {
                Log("Token exchange succeeded.");
                PrintAccessTokenDetails(response);
            }
            catch (Exception e)
            {
                Debug.Log("exception " + e.Message);
            }
        }

        private void OnExchangeFailed(string error)
        {
            Log($"Token exchange failed: {error}");
        }

        private void OnLoginSucceeded(LoginResponse response)
        {
            Log("Login succeeded");
            Log($"Provider: {response.Provider}\n" +
                $"Name: {response.Name}\n" +
                $"Email: {response.Email}\n" +
                $"AccessToken: {response.AccessToken}\n" +
                $"AuthCode: {response.AuthCode}\n" +
                $"UserId: {response.UserId}\n" +
                $"IdToken: {response.IdToken}\n");
        }

        private void OnLoginFailed(LoginResponse error)
        {
            Log("Login has failed");
            Log($"Provider: {error.Provider}\n" +
                $"Error: {error.ErrorMessage}");
        }

        private void OnEmailVerificationDidFail(string error)
        {
            Log($"Email verification has failed: {error}");
        }

        private void OnEmailVerificationDidSucceed()
        {
            Log("Email has been verified correctly.");
        }

        private void OnEmailVerificationDidSend()
        {
            Log("An email has been sent to the user to verify the email.");
        }

        private void OnLogoutDidFail(string error)
        {
            Log($"Logout has failed: {error}");
        }

        private void OnLogoutDidSucceed()
        {
            Log("Successful logout.");
        }

        private void Log(string message)
        {
            Debug.Log(message);
            LogToUI(message);
        }

        private void LogToUI(string message)
        {
            debugText.text += message + "\n";
        }

        private void PrintIdTokenDetails(TokenResponse response)
        {
            var handler = new TokenResponseHandler(response);
            var idToken = handler.IDToken;
            var sb = new StringBuilder();
            sb.AppendLine("------ ID Token ----");
            sb.AppendLine($"Token request audience {response.Audience}");
            sb.AppendLine("Id Token details:");
            sb.AppendLine($"- Expiration: {idToken.TokenExpirationDate}");
            sb.AppendLine($"- jti: {idToken.Id}");
            sb.AppendLine($"- Raw Token: {idToken.Raw}");
            sb.AppendLine($"- TP User Id: {idToken.TpUserId}");
            sb.AppendLine("----------");
            Log(sb.ToString());
        }

        private void PrintAccessTokenDetails(TokenResponse response)
        {
            var handler = new TokenResponseHandler(response);
            var accessToken = handler.AccessToken;
            var sb = new StringBuilder();
            sb.AppendLine("----- Access Token -----");
            sb.AppendLine($"Token request audience {response.Audience}");
            sb.AppendLine("Access Token details:");
            sb.AppendLine($"- Expiration: {accessToken.TokenExpirationDate}");
            sb.AppendLine($"- jti: {accessToken.Id}");
            sb.AppendLine($"- Email verified: {accessToken.EmailVerified}");
            sb.AppendLine($"- Raw Token: {accessToken.Raw}");
            sb.AppendLine($"- TP User Id: {accessToken.TpUserId}");
            sb.AppendLine("----------");
            Log(sb.ToString());
        }

        private void PrintRefreshTokenDetails(TokenResponse response)
        {
            var handler = new TokenResponseHandler(response);
            var refreshToken = handler.RefreshToken;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("----- Refresh Token -----");
            sb.AppendLine($"Token request audience {response.Audience}");
            sb.AppendLine("Refresh Token details:");
            sb.AppendLine($"- Expiration: {refreshToken.TokenExpirationDate}");
            sb.AppendLine($"- jti: {refreshToken.Id}");
            sb.AppendLine($"- Email verified: {refreshToken.EmailVerified}");
            sb.AppendLine($"- Raw Token: {refreshToken.Raw}");
            sb.AppendLine($"- TP User Id: {refreshToken.TpUserId}");
            sb.AppendLine("----------");
            Log(sb.ToString());
        }
    }
}