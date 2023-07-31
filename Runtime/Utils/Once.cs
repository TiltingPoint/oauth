/*
 * Copyright © 2021 Adam Schlesinger
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
 * documentation files (the “Software”), to deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions
 * of the Software.
 * 
 * THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
 * THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

#if !TP_CORE

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TiltingPoint {
    /// <summary>
    /// 
    /// </summary>
    public interface IOnce {
        /// <summary>
        /// True if the Once has been Completed
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IOnce OnComplete(Action action);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapper"></param>
        /// <typeparam name="TOutputResult"></typeparam>
        /// <returns></returns>
        IOnce<TOutputResult> Map<TOutputResult>(Func<TOutputResult> mapper);
    }

    /// <inheritdoc />
    /// <typeparam name="TResult"></typeparam>
    public interface IOnce<TResult> : IOnce {
        /// <summary>
        /// 
        /// </summary>
        TResult Result { get; }
        
        /// <inheritdoc cref="IOnce.OnComplete"/>
        IOnce<TResult> OnComplete(Action<TResult> action);

        /// <inheritdoc cref="IOnce.Map{TOut}"/>
        IOnce<TOutputResult> Map<TOutputResult>(Func<TResult, TOutputResult> mapper);
    }

    /// <summary>
    /// Helper class for a Once where the result type does not matter or is unused
    /// </summary>
    /// <inheritdoc cref="IOnce"/>
    public class Once : Once<object> { }

    /// <inheritdoc />
    public class Once<TResult> : IOnce<TResult> {
        /// <summary>
        /// default constructor
        /// </summary>
        public Once() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        public Once(TResult result) => Complete(result);
        
        /// <summary>
        /// Constructor which will create a pre-completed Once with the result provided
        /// </summary>
        /// <param name="result"></param>
        public static Once PreCompleted(TResult result = default) {
            var ret = new Once();
            ret.Complete(result);
            return ret;
        }
        
        /// <summary>
        /// Will Complete this Once
        /// </summary>
        /// <param name="result">the object to pass on to any listeners</param>
        public virtual void Complete(TResult result = default) {
            if (IsComplete)
                return;
            
            Result = result;
            
            var completedAction = _action;
            _action = null;

            IsComplete = true;
            completedAction?.Invoke(Result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        public void Bind(IOnce other) => other.OnComplete(() => Complete());
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        public void Bind(IOnce<TResult> other) => other.OnComplete(Complete);
        
        /// <inheritdoc />
        public bool IsComplete { get; private set; }
        
        /// <inheritdoc />
        public TResult Result { get; private set; }

        /// <inheritdoc />
        public IOnce OnComplete(Action action) => OnComplete(result => action());
        
        /// <inheritdoc />
        public IOnce<TResult> OnComplete(Action<TResult> action) {
            if (!IsComplete)
                _action += action;

            else if (action != null)
                action(Result);

            return this;
        }

        /// <inheritdoc />
        public IOnce<TOutputResult> Map<TOutputResult>(Func<TOutputResult> mapper) {
            var proxy = new Once<TOutputResult>();
            this.OnComplete(result => proxy.Complete(mapper()));
            return proxy;
        }

        /// <inheritdoc />
        public IOnce<TOutputResult> Map<TOutputResult>(Func<TResult, TOutputResult> mapper) {
            var proxy = new Once<TOutputResult>();
            this.OnComplete(result => proxy.Complete(mapper(result)));
            return proxy;
        }
        
        private Action<TResult> _action = null;
    }
}
#endif