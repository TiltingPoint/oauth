#if TP_CORE
using TiltingPoint.Events;
#endif

namespace TiltingPoint.Auth.Events
{
    internal static class AuthEventTracker
    {
        internal static void ServiceWillInit(string serviceName)
        {
            #if TP_CORE
            Track(new ServiceWillInit
            {
                ServiceName = serviceName
            });
            #endif
        }

        internal static void ServiceDidInit(string serviceName)
        {
            #if TP_CORE
            Track(new ServiceDidInit
            {
                ServiceName = serviceName
            });
            #endif
        }

        internal static void ServiceFailedInit(string serviceName)
        {
            #if TP_CORE
            Track(new ServiceFailedInit
            {
                ServiceName = serviceName
            });
            #endif
        }

        internal static void AuthenticationDidFail(string errorMessage)
        {
            #if TP_CORE
            Track(new AuthenticationDidFail
            {
                ErrorMessage = errorMessage
            });
            #endif
        }

        internal static void AuthenticationDidSucceed()
        {
            #if TP_CORE
            Track(new AuthenticationDidSucceed());
            #endif
        }

        internal static void AuthenticationWillStart()
        {
            #if TP_CORE
            Track(new AuthenticationWillStart());
            #endif
        }

        internal static void TokenExchangeDidFail(string errorMessage)
        {
            #if TP_CORE
            Track(new TokenExchangeDidFail
            {
                ErrorMessage = errorMessage
            });
            #endif
        }

        internal static void TokenExchangeDidSucceed()
        {
            #if TP_CORE
            AuthThreadDispatcher.Enqueue(() => Track(new TokenExchangeDidSucceed()));
            #endif
        }

        internal static void TokenExchangeWillStart(string issuer)
        {
            #if TP_CORE
            Track(new TokenExchangeWillStart
            {
                Issuer = issuer
            });
            #endif
        }

        internal static void TokenRequestDidFail(string errorMessage, string audience)
        {
            #if TP_CORE
            Track(new TokenRequestDidFail
            {
                ErrorMessage = errorMessage,
                Audience = audience
            });
            #endif
        }

        internal static void TokenRequestDidSucceed(string audience)
        {
            #if TP_CORE
            Track(new TokenRequestDidSucceed
            {
                Audience = audience
            });
            #endif
        }

        internal static void TokenRequestWillStart(string audience)
        {
            #if TP_CORE
            Track(new TokenRequestWillStart
            {
                Audience = audience
            });
            #endif
        }

        internal static void AuthInternalError(string errorMessage)
        {
            #if TP_CORE
            Track(new AuthInternalError
            {
                ErrorMessage = errorMessage
            });
            #endif
        }

        internal static void LoginWillStart(string provider)
        {
            #if TP_CORE
            Track(new LoginWillStart
            {
                Provider = provider
            });
            #endif
        }

        internal static void LoginDidSucceed(string provider)
        {
            #if TP_CORE
            Track(new LoginDidSucceed
            {
                Provider = provider
            });
            #endif
        }

        internal static void LoginDidFail(string provider, string errorMessage)
        {
            #if TP_CORE
            Track(new LoginDidFail
            {
                Provider = provider,
                ErrorMessage = errorMessage
            });
            #endif
        }

        internal static void EmailVerificationDidSend()
        {
            #if TP_CORE
            Track(new EmailVerificationDidSend {});
            #endif
        }

        internal static void EmailVerificationDidSucceed()
        {
            #if TP_CORE
            Track(new EmailVerificationDidSucceed {});
            #endif
        }

        internal static void EmailVerificationDidFail(string s)
        {
            #if TP_CORE
            Track(new EmailVerificationDidFail
            {
                ErrorMessage = s
            });
            #endif
        }

        internal static void LogoutWillStart()
        {
            #if TP_CORE
            Track(new LogoutWillStart {});
            #endif
        }

        internal static void LogoutDidSucceed()
        {
            #if TP_CORE
            Track(new LogoutDidSucceed {});
            #endif
        }

        internal static void LogoutDidFail(string s)
        {
            #if TP_CORE
            Track(new LogoutDidFail
            {
                ErrorMessage = s
            });
            #endif
        }

        #if TP_CORE
        private static void Track(TiltingPoint.Events.Event e)
        {
            AuthThreadDispatcher.Enqueue(() => SDK.Track(e));
        }
        #endif
    }
}