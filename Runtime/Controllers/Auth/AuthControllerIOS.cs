#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;

namespace TiltingPoint.Auth
{
    internal class AuthControllerIOS : AuthControllerBase
    {

        internal AuthControllerIOS(TPAuthConfig config, Action<string> onInitDidFail)
            : base(
            config, onInitDidFail) { }

        [DllImport ("__Internal")]
        private static extern void InitializeInternal(string issuer, string clientId, string callback);

        [DllImport ("__Internal")]
        private static extern void AuthenticateInternal();

        internal override void Initialize()
        {
            if (!BaseInit())
            {
                return;
            }

            InitializeInternal(_config.issuer, _config.clientId, _config.callbackUrl);
        }

        internal override void Authenticate()
        {
            AuthenticateInternal();
        }
    }
}

#endif