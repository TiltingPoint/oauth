using System;
using UnityEngine;

namespace TiltingPoint.Auth.Events {
    /// <summary>
    /// Public events for Native -> Unity communication.
    /// Do not use.
    /// The only use of this class is for it to be attached to a game object so Native can communicate with Unity through this game object.
    /// Ex:
    /// UnityPlayer.UnitySendMessage( NAME_OF_THE_GAMEOBJECT, METHOD_TO_CALL, PARAMETERS);
    /// </summary>
    public class AuthEvents : MonoBehaviour
    {
        public event Action InitDidSucceedEvent;

        public event Action<string> InitDidFailEvent;

        public event Action AuthWillStartEvent;

        public event Action<string> AuthDidSucceedEvent;

        public event Action<string> AuthDidFailEvent;

        public event Action GainedFocusEvent;

        public void InitDidSucceed()
        {
            InitDidSucceedEvent?.Invoke();
        }

        public void InitDidFail(string msg)
        {
            InitDidFailEvent?.Invoke(msg);
        }

        public void AuthWillStart()
        {
            AuthWillStartEvent?.Invoke();
        }

        public void AuthDidSucceed(string msg)
        {
            AuthDidSucceedEvent?.Invoke(msg);
        }

        public void AuthDidFail(string msg)
        {
            AuthDidFailEvent?.Invoke(msg);
        }

        public void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                GainedFocusEvent?.Invoke();
            }
        }
    }
}