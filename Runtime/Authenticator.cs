using System;
using TiltingPoint.Auth.Events;
using UnityEngine;
#if TP_OPS
using System.Collections.Generic;
using TiltingPoint.Ops;
#endif

namespace TiltingPoint.Auth
{
    public static class Authenticator
    {
        /// <summary>
        /// Initialization flag.
        /// </summary>
        public static bool Initialized;

        private static readonly BinOnce OnControllerReady = new BinOnce();
        private static readonly BinOnce OnControllerInitialized = new BinOnce();
        private static AuthControllerBase _authController;
        private static TokenRequestController _tokenRequestController;
        private static TokenExchangeController _tokenExchangeController;
        private static LoginController _loginController;
        private static LogoutController _logoutController;
        private static EmailController _emailController;
        private static ControllerUtils _utilsController;
        private static bool _initCalled;
        private static bool _initInProgress;

        /// <summary>
        /// Called after a successful initialization.
        /// </summary>
        public static event Action OnInitDidSucceed
        {
            add { OnControllerReady.OnTrue(() => _authController.OnInitDidSucceed += value); }
            remove { OnControllerReady.OnTrue(() => _authController.OnInitDidSucceed -= value); }
        }

        /// <summary>
        /// Called after a failed initialization.
        /// </summary>
        /// <returns> Returns the cause of error. </returns>
        public static event Action<string> OnInitDidFail;

        /// <summary>
        /// Called after a successful authentication.
        /// </summary>
        /// <returns> Returns a TokenResponse object containing the token id, access token and refresh token. </returns>
        public static event Action<TokenResponse> OnAuthDidSucceed
        {
            add { OnControllerInitialized.OnTrue(() => _authController.OnAuthDidSucceed += value); }
            remove { OnControllerInitialized.OnTrue(() => _authController.OnAuthDidSucceed -= value); }
        }

        /// <summary>
        /// Called after a failed authentication.
        /// </summary>
        /// <returns> Returns an error message. </returns>
        public static event Action<string> OnAuthDidFail
        {
            add { OnControllerInitialized.OnTrue(() => _authController.OnAuthDidFail += value); }
            remove { OnControllerInitialized.OnTrue(() => _authController.OnAuthDidFail -= value); }
        }

        /// <summary>
        /// Called after a successful fresh token request.
        /// </summary>
        /// <returns> Returns a TokenResponse object containing the token id, access token and refresh token. </returns>
        public static event Action<TokenResponse> OnTokenRequestDidSucceed
        {
            add { OnControllerInitialized.OnTrue(() => _tokenRequestController.OnTokenRequestDidSucceed += value); }
            remove { OnControllerInitialized.OnTrue(() => _tokenRequestController.OnTokenRequestDidSucceed -= value); }
        }

        /// <summary>
        /// Called after a failed fresh token request.
        /// </summary>
        /// <returns> Returns an error message. </returns>
        public static event Action<string> OnTokenRequestDidFail
        {
            add { OnControllerInitialized.OnTrue(() => _tokenRequestController.OnTokenRequestDidFail += value); }
            remove { OnControllerInitialized.OnTrue(() => _tokenRequestController.OnTokenRequestDidFail -= value); }
        }

        /// <summary>
        /// Called after a successful token exchange.
        /// </summary>
        /// <returns> Returns a TokenResponse object containing the token id, access token and refresh token. </returns>
        public static event Action<TokenResponse> OnTokenExchangeDidSucceed
        {
            add { OnControllerInitialized.OnTrue(() => _tokenExchangeController.OnTokenExchangeDidSucceed += value); }
            remove { OnControllerInitialized.OnTrue(() => _tokenExchangeController.OnTokenExchangeDidSucceed -= value); }
        }

        /// <summary>
        /// Called after a failed token exchange.
        /// </summary>
        /// <returns> > Returns an error message. </returns>
        public static event Action<string> OnTokenExchangeDidFail
        {
            add { OnControllerInitialized.OnTrue(() => _tokenExchangeController.OnTokenExchangeDidFail += value); }
            remove { OnControllerInitialized.OnTrue(() => _tokenExchangeController.OnTokenExchangeDidFail -= value); }
        }

        /// <summary>
        /// Called after a failed login.
        /// </summary>
        /// <returns> > Returns an error message. </returns>
        public static event Action<LoginResponse> OnLoginDidSucceed
        {
            add { OnControllerInitialized.OnTrue(() => _loginController.OnLoginDidSucceed += value); }
            remove { OnControllerInitialized.OnTrue(() => _loginController.OnLoginDidSucceed -= value); }
        }

        /// <summary>
        /// Called after a successful token login.
        /// </summary>
        /// <returns> > Returns an error message. </returns>
        public static event Action<LoginResponse> OnLoginDidFail
        {
            add { OnControllerInitialized.OnTrue(() => { _loginController.OnLoginDidFail += value; }); }
            remove { OnControllerInitialized.OnTrue(() => _loginController.OnLoginDidFail -= value); }
        }

        /// <summary>
        /// Called after a failed login.
        /// </summary>
        /// <returns> > Returns an error message. </returns>
        public static event Action OnLogoutDidSucceed
        {
            add { OnControllerInitialized.OnTrue(() => _logoutController.OnLogoutDidSucceed += value); }
            remove { OnControllerInitialized.OnTrue(() => _logoutController.OnLogoutDidSucceed -= value); }
        }

        /// <summary>
        /// Called after a successful token login.
        /// </summary>
        /// <returns> > Returns an error message. </returns>
        public static event Action<string> OnLogoutDidFail
        {
            add { OnControllerInitialized.OnTrue(() => { _logoutController.OnLogoutDidFail += value; }); }
            remove { OnControllerInitialized.OnTrue(() => _logoutController.OnLogoutDidFail -= value); }
        }

        /// <summary>
        /// Called when the email verification has been sent correctly.
        /// </summary>
        public static event Action OnEmailVerificationDidSend
        {
            add { OnControllerInitialized.OnTrue(() => _emailController.OnEmailVerificationDidSend += value); }
            remove { OnControllerInitialized.OnTrue(() => _emailController.OnEmailVerificationDidSend -= value); }
        }

        /// <summary>
        /// Called when the email verification state of the user changes to verified.
        /// </summary>
        public static event Action OnEmailVerificationDidSucceed
        {
            add { OnControllerInitialized.OnTrue(() => { _emailController.OnEmailVerificationDidSucceed += value; }); }
            remove { OnControllerInitialized.OnTrue(() => _emailController.OnEmailVerificationDidSucceed -= value); }
        }

        /// <summary>
        /// Called when there has been problems sending the verification email.
        /// </summary>
        /// <returns> > Returns an error message. </returns>
        public static event Action<string> OnEmailVerificationDidFail
        {
            add { OnControllerInitialized.OnTrue(() => { _emailController.OnEmailVerificationDidFail += value; }); }
            remove { OnControllerInitialized.OnTrue(() => _emailController.OnEmailVerificationDidFail -= value); }
        }

        /// <summary>
        /// Gets a value indicating whether does the user need to get authenticated. Only valid after a successful package initialization.
        /// </summary>
        public static bool NeedToAuth => CheckAuthValidity();

        /// <summary>
        /// Initializes the service and pulls the server configuration.
        /// </summary>
        public static void Initialize()
        {
            if (_initInProgress)
            {
                return;
            }

            _initInProgress = true;

            if (!_initCalled)
            {
                _initCalled = true;
                AuthEventTracker.ServiceWillInit(Consts.SERVICE_NAME);
                SetupControllers();

                OnInitDidFail += s =>
                {
                    _initInProgress = false;
                    OnControllerInitialized.CompleteFalse();
                };

                _authController.OnInitDidSucceed += () =>
                {
                    _loginController.InitializeLoginMediators();
                    _tokenRequestController.TokenRequest(response => { }, true);
                    Initialized = true;
                    OnControllerInitialized.CompleteTrue();
                };

                OnControllerReady.CompleteTrue();
            }

            _authController.Initialize();
        }

        /// <summary>
        /// Prompts the login screen for users to login or register. They can also login using the supported providers.
        /// </summary>
        public static void Authenticate()
        {
            if (!CheckInit())
            {
                return;
            }

            _authController.Authenticate();
        }

        /// <summary>
        /// Requests fresh tokens to the server. Only works if the user has been previously authenticated.
        /// </summary>
        public static void TokenRequest()
        {
            if (!CheckInit())
            {
                return;
            }

            _tokenRequestController.TokenRequest(false);
        }

        /// <summary>
        /// Requests fresh tokens to the server. Only works if the user has been previously authenticated.
        /// </summary>
        /// <param name="forceRefresh">Forces your tokens to get refreshed.</param>
        public static void TokenRequest(bool forceRefresh)
        {
            if (!CheckInit())
            {
                return;
            }

            _tokenRequestController.TokenRequest(forceRefresh);
        }

        /// <summary>
        /// Requests fresh tokens to the server. Only works if the user has been previously authenticated.
        /// </summary>
        /// <param name="cbk">Callback triggered after the token request gets performed.</param>
        /// <param name="forceRefresh">Forces your tokens to get refreshed.</param>
        /// <remarks>
        /// This method variant will only trigger the callback attached to it and not the OnTokenRequestDidSucceed event.
        /// </remarks>
        public static void TokenRequest(Action<TokenResponse> cbk, bool forceRefresh = false)
        {
            if (!CheckInit())
            {
                return;
            }

            _tokenRequestController.TokenRequest(cbk, forceRefresh);
        }

        /// <summary>
        /// Requests an RPT token for a specific service to the server. Only works if the user has been previously
        /// authenticated.
        /// </summary>
        /// <param name="service">Service name of the RPT tokens.</param>
        /// <param name="forceRefresh">Forces your tokens to get refreshed.</param>
        public static void TokenRequest(string service, bool forceRefresh = false)
        {
            if (!CheckInit())
            {
                return;
            }

            _tokenRequestController.TokenRequest(service, forceRefresh);
        }

        /// <summary>
        /// Requests an RPT token for a specific service to the server. Only works if the user has been previously
        /// authenticated.
        /// </summary>
        /// <param name="service">Service name of the RPT tokens.</param>
        /// <param name="cbk">Callback triggered after the token request gets performed.</param>
        /// <param name="forceRefresh">Forces your tokens to get refreshed.</param>
        /// <remarks>
        /// This method variant will only trigger the callback attached to it and not the OnTokenRequestDidSucceed event.
        /// </remarks>
        public static void TokenRequest(string service, Action<TokenResponse> cbk, bool forceRefresh = false)
        {
            if (!CheckInit())
            {
                return;
            }

            _tokenRequestController.TokenRequest(service, cbk, forceRefresh);
        }

        /// <summary>
        /// Exchange tokens with an external provider.
        /// </summary>
        /// <param name="token">Token to be exchanged.</param>
        /// <param name="issuer">Token issuer.</param>
        public static void TokenExchange(string token, string issuer)
        {
            if (!CheckInit())
            {
                return;
            }

            _tokenExchangeController.TokenExchange(token, issuer);
        }

        /// <summary>
        /// Triggers the login flow with the specified login mediator.
        /// </summary>
        /// <param name="mediator">Login Mediator identifier.</param>
        /// <param name="performTokenRequest">Automatically trigger a token request after login.</param>
        public static void Login(string mediator, bool performTokenRequest = true)
        {
            OnControllerInitialized.OnTrue(() => _loginController.Login(mediator, performTokenRequest));
        }

        /// <summary>
        /// Refreshes local tokens and checks if the user email has been verified.
        /// </summary>
        /// <param name="cbk">Action that returns if the user email is verified.</param>
        public static void IsUserEmailVerified(Action<bool> cbk)
        {
            if (!CheckInit())
            {
                return;
            }

            OnControllerReady.OnTrue(() => _emailController.IsUserEmailVerified(cbk, true));
        }

        /// <summary>
        /// Checks if the stored user email has been verified.
        /// </summary>
        /// <param name="cbk">Action that returns if the user email is verified.</param>
        public static void IsCachedUserEmailVerified(Action<bool> cbk)
        {
            if (!CheckInit())
            {
                return;
            }

            OnControllerReady.OnTrue(() => _emailController.IsUserEmailVerified(cbk, false));
        }

        /// <summary>
        /// Starts the verification flow by sending the user an email to verify his email address.
        /// </summary>
        public static void SendVerificationEmail()
        {
            if (!CheckInit())
            {
                return;
            }

            OnControllerReady.OnTrue(() => _emailController.SendVerificationEmail());
        }

        /// <summary>
        /// Deletes user credentials.
        /// </summary>
        public static void Logout()
        {
            if (!CheckInit())
            {
                return;
            }

            OnControllerReady.OnTrue(() => _logoutController.Logout());
        }

        /// <summary>
        /// Used by Login Mediators to register themselves.
        /// </summary>
        /// <param name="mediator">Mediator Type. Type is used as an identifier for the mediator.</param>
        public static void RegisterLoginMediator(Type mediator)
        {
            OnControllerReady.OnTrue(() => _loginController.RegisterLoginMediator(mediator));
        }

        private static bool CheckAuthValidity()
        {
            return !(Initialized && _utilsController.IsAuthenticationValid());
        }

        private static bool CheckInit()
        {
            if (Initialized)
            {
                return true;
            }

            Debug.LogError("[TP Auth] Initialize Auth Package before accessing it.");
            return false;
        }

        private static void SetupControllers()
        {
            var config = ConfigController.InitConfig(OnInitDidFail);

            _utilsController = new ControllerUtils(config);
            _loginController = new LoginController();

            #if UNITY_ANDROID && !UNITY_EDITOR
            _authController = new AuthControllerAndroid(config, OnInitDidFail);
            #elif UNITY_IOS && !UNITY_EDITOR
            _authController = new AuthControllerIOS(config, OnInitDidFail);
            #else
            _authController = new AuthControllerBase(config, OnInitDidFail);
            #endif

            _tokenRequestController = new TokenRequestController(config, _utilsController);
            _emailController = new EmailController(config, _utilsController, _tokenRequestController.TokenRequest, _authController.EventHandler);
            _utilsController.SetupEmailCallbacks(_emailController.IsUserEmailVerified, _emailController.CheckEmailVerification);
            _tokenExchangeController = new TokenExchangeController(config, _utilsController);
            _logoutController = new LogoutController(config, _tokenRequestController.TokenRequest);
        }
    }
}