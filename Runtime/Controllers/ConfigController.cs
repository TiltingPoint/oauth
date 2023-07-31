using System;
using UnityEngine;
#if TP_OPS
using System.Collections.Generic;
using TiltingPoint.Ops;
#endif

namespace TiltingPoint.Auth
{
    public static class ConfigController
    {
        internal static TPAuthConfig InitConfig(Action<string> initDidFail)
        {
            var config = Resources.Load<TPAuthConfig>("AuthConfig");
            const string suffix = " not set in config";
            if (config == null)
            {
                initDidFail(
                    "Error reading Auth _config. Please try setting it up using TiltingPoint > Auth _config");
                return null;
            }

            if (string.IsNullOrEmpty(config.issuer))
            {
                initDidFail($"Issuer{suffix}");
                return null;
            }

            if (string.IsNullOrEmpty(config.clientId))
            {
                initDidFail($"Client Id{suffix}");
                return null;
            }

            if (string.IsNullOrEmpty(config.callbackUrl))
            {
                initDidFail($"Callback URL{suffix}");
                return null;
            }

            if (string.IsNullOrEmpty(config.tokenUrl))
            {
                initDidFail(
                    $"Token URL{suffix}");
                return null;
            }

            if (string.IsNullOrEmpty(config.verifyEmailUrl))
            {
                initDidFail(
                    $"Verify email URL{suffix}");
                return null;
            }

            if (string.IsNullOrEmpty(config.logoutUrl))
            {
                initDidFail(
                    $"Logout URL{suffix}");
                return null;
            }

            return config;
        }

        // TODO: investigate initialization timing.
        #if TP_OPS
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeRemoteConfig() {
            OPS.AddRemoteConfig("Auth", new Dictionary<string, object> {
                {"refresh_safe_time_in_minutes", 1}
            });
        }
        #endif
    }
}