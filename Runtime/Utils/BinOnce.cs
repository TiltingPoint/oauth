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

namespace TiltingPoint {
    /// <summary>
    /// 
    /// </summary>
    public class BinResult {
        /// <summary>
        /// 
        /// </summary>
        public bool Status { get; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        public BinResult(bool status) 
            => Status = status;

        /// <summary>
        /// Helper so that BinOnce.Complete can be Completed with bool
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static implicit operator BinResult(bool source) 
            => new BinResult(source);
    }
    
    /// <inheritdoc />
    /// <typeparam name="TResult"></typeparam>
    public class BinResult<TResult> : BinResult {
        /// <summary>
        /// 
        /// </summary>
        public TResult Value { get; }

        /// <inheritdoc />
        /// <param name="status"></param>
        /// <param name="value"></param>
        public BinResult(bool status, TResult value) 
            : base(status) 
            => Value = value;
    }
    
    /// <summary>
    /// Binary Once
    /// </summary>
    /// <inheritdoc />
    public interface IBinOnce : IOnce {
        /// <summary>
        /// True if the IBinOnce has been Completed and is marked as True
        /// </summary>
        bool True { get; }
        
        /// <summary>
        /// True if the IBinOnce has been Completed and is marked as False
        /// </summary>
        bool False { get; }

        /// <summary>
        /// Action which will be called if and when the IBinOnce is marked as True
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IBinOnce OnTrue(Action action);

        /// <summary>
        /// Action which will be called if and when the IBinOnce is marked as False
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IBinOnce OnFalse(Action action);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapper"></param>
        /// <returns></returns>
        IBinOnce Map(Func<BinResult> mapper);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapper"></param>
        /// <returns></returns>
        IBinOnce Map(Func<BinResult, BinResult> mapper);
    }

    /// <inheritdoc cref="IBinOnce"/>
    public interface IBinOnce<TTrue, TFalse> : IBinOnce, IOnce<BinResult> {
        /// <inheritdoc cref="IBinOnce.OnTrue"/>
        IBinOnce<TTrue, TFalse> OnTrue(Action<TTrue> action);
        
        /// <inheritdoc cref="IBinOnce.OnFalse"/>
        IBinOnce<TTrue, TFalse> OnFalse(Action<TFalse> action);
        
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Will not be usable if TTrue and TFalse are of the same type. Instead use MapTrue</remarks>
        /// <param name="mapper"></param>
        /// <typeparam name="TOut"></typeparam>
        /// <returns></returns>
        IBinOnce<TOut> MapTrue<TOut>(Func<TTrue, TOut> mapper);
    }
    
    /// <summary>
    /// Helper interface for a BinOnce where the fail type does not matter or is unused
    /// </summary>
    /// <typeparam name="TTrue"></typeparam>
    public interface IBinOnce<TTrue> : IBinOnce<TTrue, object> { }

    /// <summary>
    /// Helper class for a BinOnce where the fail type does not matter or is not used
    /// </summary>
    /// <inheritdoc cref="IBinOnce"/>
    public class BinOnce<TTrue> : BinOnce<TTrue, object>, IBinOnce<TTrue> { }

    /// <summary>
    /// Helper class for a BinOnce where the result types do not matter or are unused
    /// </summary>
    /// <inheritdoc cref="IBinOnce"/>
    public class BinOnce : BinOnce<object, object> { }
    
    /// <inheritdoc cref="IBinOnce" />
    public class BinOnce<TTrue, TFalse> : Once<BinResult>, IBinOnce<TTrue, TFalse> {
        /// <summary>
        /// Will Complete this BinOnce as True
        /// </summary>
        /// <param name="result"></param>
        public void CompleteTrue(TTrue result = default) 
            => this.Complete(new BinResult<TTrue>(true, result));
        
        /// <summary>
        /// Will Complete this BinOnce as False
        /// </summary>
        /// <param name="result"></param>
        public void CompleteFalse(TFalse result = default) 
            => this.Complete(new BinResult<TFalse>(false, result));
        
        /// <inheritdoc />
        public override void Complete(BinResult result) {
            if (result == null)
                throw new Exception("BinOnce cannot be completed with a null BinResult");
            
            base.Complete(result);
        }
        
        /// <inheritdoc />
        public bool True => IsComplete && Result.Status;
        
        /// <inheritdoc />
        public bool False => IsComplete && !Result.Status;

        /// <inheritdoc />
        public IBinOnce OnTrue(Action action) {
            this.OnTrue(result => action());
            return this;
        }

        /// <inheritdoc />
        public IBinOnce<TTrue, TFalse> OnTrue(Action<TTrue> action) {
            this.OnComplete(result => _handleResult(true, result, action));
            return this;
        }
        
        /// <inheritdoc />
        public IBinOnce OnFalse(Action action) {
            this.OnFalse(result => action());
            return this;
        }

        /// <inheritdoc />
        public IBinOnce<TTrue, TFalse> OnFalse(Action<TFalse> action) {
            this.OnComplete(result => _handleResult(false, result, action));
            return this;
        }

        /// <inheritdoc />
        public IBinOnce Map(Func<BinResult> mapper) {
            var proxy = new BinOnce();
            this.OnComplete(result => proxy.Complete(mapper()));
            return proxy;
        }

        /// <inheritdoc />
        public IBinOnce Map(Func<BinResult, BinResult> mapper) {
            var proxy = new BinOnce();
            this.OnComplete(result => proxy.Complete(mapper(result)));
            return proxy;
        }

        /// <inheritdoc />
        public IBinOnce<TOut> MapTrue<TOut>(Func<TTrue, TOut> mapper) {
            var proxy = new BinOnce<TOut>();
            this.OnTrue(result => proxy.CompleteTrue(mapper(result)));
            this.OnFalse(result => proxy.CompleteFalse());
            return proxy;
        }
        
        private static void _handleResult<TResult>(bool expectedStatus, BinResult result, Action<TResult> action) {
            if (result.Status != expectedStatus)
                return;

            if (action != null && result is BinResult<TResult> derived) // if derived is null something seriously wrong happened
                action(derived.Value);
        }
    }
}
#endif