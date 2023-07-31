using System;
using System.Collections.Generic;
using System.Linq;
using TiltingPoint.Auth.Events;
using UnityEngine;

namespace TiltingPoint.Auth
{
    internal class LoginController
    {
        internal event Action<LoginResponse> OnLoginDidSucceed;

        internal event Action<LoginResponse> OnLoginDidFail;

        private readonly Dictionary<string, BaseLogin> _loginMediators = new Dictionary<string, BaseLogin>();

        internal void RegisterLoginMediator(Type mediatorType)
        {
            var mediator = (BaseLogin) Activator.CreateInstance(mediatorType);
            var name = mediatorType.ToString();
            if (_loginMediators.ContainsKey(name))
            {
                Debug.LogError($"Login Mediator {name} was is already registered!");
                return;
            }

            _loginMediators.Add(name, mediator);
        }

        internal void Login(string mediatorName, bool performTokenExchange) {
            AuthEventTracker.LoginWillStart(mediatorName);
            if (_loginMediators.TryGetValue(mediatorName, out var mediator))
            {
                mediator.Login(performTokenExchange);
            }
            else
            {
                OnLoginDidFail?.Invoke(new LoginResponse
                {
                    ErrorMessage = $"No mediator named {mediatorName} has been registered.",
                    Provider = mediatorName
                });
            }
        }

        internal void InitializeLoginMediators()
        {
            foreach (var mediator in _loginMediators.Select(kv => kv.Value))
            {
                if (mediator == null)
                {
                    return;
                }

                mediator.Initialize();
                mediator.OnLoginDidSucceed += LoginDidSucceed;
                mediator.OnLoginDidFail += LoginDidFail;
            }
        }

        private void LoginDidSucceed(LoginResponse response)
        {
            AuthEventTracker.LoginDidSucceed(response.Provider);
            if (OnLoginDidSucceed != null)
            {
                AuthThreadDispatcher.Enqueue(OnLoginDidSucceed, response);
            }
        }

        private void LoginDidFail(LoginResponse response)
        {
            AuthEventTracker.LoginDidFail(response.Provider, response.ErrorMessage);
            AuthThreadDispatcher.Enqueue(OnLoginDidFail, response);
        }
    }
}