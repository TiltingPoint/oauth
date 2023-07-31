using System;
using TiltingPoint.Auth.Events;
using UnityEngine;
using static TiltingPoint.Auth.Consts;
using Object = UnityEngine.Object;

namespace TiltingPoint.Auth
{
    internal class AuthControllerBase
    {
        protected static TPAuthConfig _config;

        internal AuthEvents EventHandler;

        internal event Action OnInitDidSucceed;

        private event Action<string> OnInitDidFail;

        internal event Action<TokenResponse> OnAuthDidSucceed;

        internal event Action<string> OnAuthDidFail;

        private bool _handlersSetup;

        private bool _nativeListenerAdded;

        internal AuthControllerBase(TPAuthConfig config, Action<string> onInitDidFail)
        {
            _config = config;
            OnInitDidFail = onInitDidFail;
        }

        protected bool BaseInit()
        {
            SetupEventHandler();
            AddNativeEventsListeners();
            return _config != null;
        }

        internal virtual void Initialize()
        {
            if (!BaseInit())
            {
                return;
            }

            EventHandler.InitDidSucceed();
        }

        /// <summary>
        /// This method is only supported in Android and iOS devices.
        /// Error will get thrown on Editor and unsupported platforms.
        /// </summary>
        internal virtual void Authenticate()
        {
            EventHandler.AuthWillStart();
            EventHandler.AuthDidFail(UNSUPPORTED_ACTION_MSG);
            Debug.LogError($"[TP Auth] {UNSUPPORTED_ACTION_MSG}");
        }

        private void TriggerFailedInit(string s)
        {
            AuthEventTracker.ServiceFailedInit(SERVICE_NAME);
            if (OnInitDidFail != null)
            {
                AuthThreadDispatcher.Enqueue(OnInitDidFail, s);
            }
        }

        private void SetupEventHandler()
        {
            if (_handlersSetup)
            {
                return;
            }

            _handlersSetup = true;
            EventHandler = new GameObject("TPAuth").AddComponent<AuthEvents>();
            Object.DontDestroyOnLoad(EventHandler);
        }

        private void AddNativeEventsListeners()
        {
            /*
             * NOTES: Using the event handles its an extra step that can be avoided for some events. However, since we
             * working with events that can get triggered from native, it's good for consistency and maintenance.
             */

            if (_nativeListenerAdded)
            {
                return;
            }

            _nativeListenerAdded = true;

            EventHandler.InitDidSucceedEvent += () =>
            {
                AuthEventTracker.ServiceDidInit(SERVICE_NAME);
                if (OnInitDidSucceed != null)
                {
                    AuthThreadDispatcher.Enqueue(OnInitDidSucceed);
                }
            };

            EventHandler.InitDidFailEvent += TriggerFailedInit;

            EventHandler.AuthWillStartEvent += AuthEventTracker.AuthenticationWillStart;

            EventHandler.AuthDidSucceedEvent += async s =>
            {
                var tokens = ParseTokens(s);
                tokens.Audience = MAIN_TOKEN;
                var tokenValid = await AuthUtils.ValidateToken(tokens.IdToken, _config.clientId, _config.issuer, OnAuthDidFail);
                if (!tokenValid)
                {
                    return;
                }

                ControllerUtils.SaveTokens(tokens);
                AuthEventTracker.AuthenticationDidSucceed();
                if (OnAuthDidSucceed != null)
                {
                    AuthThreadDispatcher.Enqueue(OnAuthDidSucceed, tokens);
                }
            };

            EventHandler.AuthDidFailEvent += s =>
            {
                AuthEventTracker.AuthenticationDidFail(s);
                if (OnAuthDidFail != null)
                {
                    AuthThreadDispatcher.Enqueue(OnAuthDidFail, s);
                }
            };
        }

        private InternalTokenResponse ParseTokens(string s)
        {
            InternalTokenResponse tokens = null;
            try
            {
                tokens = JsonUtility.FromJson<InternalTokenResponse>(s);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TP Auth]: Error parsing AppAuth response: {e.Message}. Original response: {s}");
            }

            return tokens;
        }
    }
}