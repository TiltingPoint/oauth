#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;

namespace TiltingPoint.Auth
{
    internal class AuthControllerAndroid : AuthControllerBase
    {
        private const string ANDROID_CLASS_NATIVE_APP_INFO = "com.tiltingpoint.android.AuthActivity";

        internal AuthControllerAndroid(TPAuthConfig config, Action<string> onInitDidFail)
            : base(config, onInitDidFail) { }

        internal override void Initialize()
        {
            if (!BaseInit())
            {
                return;
            }

            var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            var unityContext = unityActivity.Call<AndroidJavaObject>("getApplicationContext");
            CallNative("initialize", _config.issuer, _config.clientId, _config.callbackUrl, unityContext);
        }

        internal override void Authenticate()
        {
            CallNative("authenticate");
        }

        private static void CallNative(string methodName, params object[] args)
        {
            try
            {
                using var javaClass = new AndroidJavaClass(ANDROID_CLASS_NATIVE_APP_INFO);
                javaClass.CallStatic(methodName, args);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TP Auth] Error calling Android native method {methodName} Error: {e.Message}");
            }
        }
    }
}

#endif