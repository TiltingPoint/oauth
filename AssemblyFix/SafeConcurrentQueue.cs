#if !TP_CORE_4_3_0_OR_GREATER
using System.Collections.Concurrent;
namespace TiltingPoint
{
    /// <summary>
    /// Helper class to avoid conflicts between ConcurrentQueue from Leanplum and Microsoft, since they share same namespace.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SafeConcurrentQueue<T> : ConcurrentQueue<T> { }
}
#endif
