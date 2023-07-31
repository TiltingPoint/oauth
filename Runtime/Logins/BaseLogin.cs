using System;
using UnityEngine;

namespace TiltingPoint.Auth
{
    public abstract class BaseLogin
    {
        /// <summary>
        /// Should be overwritten with the Login Mediator Type.
        /// </summary>
        public virtual string Issuer => string.Empty;

        /// <summary>
        /// Triggered after a successful login.
        /// </summary>
        /// <returns>Login provider token</returns>
        public abstract event Action<LoginResponse> OnLoginDidSucceed;

        /// <summary>
        /// Triggered after a failed login intent.
        /// </summary>
        /// <returns>Error if any.</returns>
        public abstract event Action<LoginResponse> OnLoginDidFail;

        /// <summary>
        /// Flag for automatically perform a token exchange after a successful login.
        /// </summary>
        protected bool PerformTokenExchange = true;

        /// <summary>
        /// Flag that indicates if a login is in progress.
        /// </summary>
        protected bool LoginInProgress;

        /// <summary>
        /// Initializes login mediator.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Starts external login flow.
        /// </summary>
        /// <param name="performTokenExchange">
        /// Performs a token exchange automatically. Defaults true.
        /// It will trigger the token exchange related events from the Authenticator.
        /// </param>
        public abstract void Login(bool performTokenExchange = true);
    }
}