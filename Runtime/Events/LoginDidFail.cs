#if TP_CORE
using TiltingPoint.Events;

namespace TiltingPoint.Auth.Events
{
    #if TP_CORE_4_3_0_OR_GREATER
    public sealed class LoginDidFail : Event
    {
    #else
    public sealed class LoginDidFail : Event<LoginDidFail> 
    {
    #endif
        public string Provider;
        public string ErrorMessage;
    }
}
#endif