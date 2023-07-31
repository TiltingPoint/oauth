#if TP_CORE
using TiltingPoint.Events;

namespace TiltingPoint.Auth.Events
{
    #if TP_CORE_4_3_0_OR_GREATER
    public sealed class TokenExchangeDidSucceed : Event
    {
    }
    #else
    public sealed class TokenExchangeDidSucceed : Event<TokenExchangeDidSucceed> { }
    #endif
}
#endif