using System;
using System.Collections;
using UnityEngine;

namespace TiltingPoint.Auth
{
    /// <summary>
    /// Helper class to avoid threading issues by using Unity's main thread.
    /// </summary>
    internal class AuthThreadDispatcher : MonoBehaviour
    {
        private static readonly SafeConcurrentQueue<Action> ExecutionQueue = new SafeConcurrentQueue<Action>();

        private static AuthThreadDispatcher _instance;

        internal static void Enqueue(IEnumerator action)
        {
            lock (ExecutionQueue)
            {
                ExecutionQueue.Enqueue(() => { _instance.StartCoroutine(action); });
            }
        }

        internal static void Enqueue(Action action)
        {
            if (action != null)
            {
                Enqueue(ActionWrapper(action));
            }
        }

        internal static void Enqueue(Action<string> action, string content)
        {
            if (action != null)
            {
                Enqueue(ActionWrapper(action, content));
            }
        }

        internal static void Enqueue(Action<TokenResponse> action, TokenResponse content)
        {
            if (action != null)
            {
                Enqueue(ActionWrapper(action, content));
            }
        }

        internal static void Enqueue(Action<LoginResponse> action, LoginResponse content)
        {
            if (action != null)
            {
                Enqueue(ActionWrapper(action, content));
            }
        }

        private static IEnumerator ActionWrapper(Action actionToWrap)
        {
            actionToWrap();
            yield return null;
        }

        private static IEnumerator ActionWrapper(Action<string> actionToWrap, string content)
        {
            actionToWrap(content);
            yield return null;
        }

        private static IEnumerator ActionWrapper(Action<TokenResponse> actionToWrap, TokenResponse content)
        {
            actionToWrap(content);
            yield return null;
        }

        private static IEnumerator ActionWrapper(Action<LoginResponse> actionToWrap, LoginResponse content)
        {
            actionToWrap(content);
            yield return null;
        }

        private void Update()
        {
            while (!ExecutionQueue.IsEmpty)
            {
                if (ExecutionQueue.TryDequeue(out var action))
                {
                    action?.Invoke();
                }
            }
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            var go = new GameObject(Consts.DISPATCHER_NAME);
            _instance = go.AddComponent<AuthThreadDispatcher>();
            DontDestroyOnLoad(_instance.gameObject);
        }
    }
}