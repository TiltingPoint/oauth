#if TP_CORE
using TiltingPoint.Events;

namespace TiltingPoint.Auth.Events
{
    #if TP_CORE_4_3_0_OR_GREATER
    public sealed class LogoutWillStart : Event
    {
    #else
    public sealed class LogoutWillStart : Event<LogoutWillStart> 
    {
    #endif
        public string Provider;
    }
}
#endif