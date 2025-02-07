﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Invokers
{
    /// <summary>
    /// Wraps an <b>Invoke</b> enabling standard functionality to be added to all invocations. 
    /// </summary>
    /// <typeparam name="TInvoker">The owner (invoking) <see cref="Type"/>.</typeparam>
    /// <remarks>All public methods result in <see cref="OnInvokeAsync{TResult}(TInvoker, Func{CancellationToken, Task{TResult}}, CancellationToken)"/> being called to manage the underlying invocation. Where no result is specified 
    /// this defaults to '<c>object?</c>' for the purposes of execution.</remarks>
    public abstract class InvokerBase<TInvoker>
    {
        /// <summary>
        /// Invokes a <paramref name="func"/> with a <typeparamref name="TResult"/> asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The result.</returns>
        protected virtual Task<TResult> OnInvokeAsync<TResult>(TInvoker invoker, Func<CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
            => func(cancellationToken);

        #region Sync/NoResult

        /// <summary>
        /// Invokes an <paramref name="action"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="action">The action to invoke.</param>
        public void Invoke(TInvoker invoker, Action action)
            => Invoker.RunSync(() => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => { (action ?? throw new ArgumentNullException(nameof(action))).Invoke(); return Task.FromResult<object?>(null!); }, CancellationToken.None));

        /// <summary>
        /// Invokes an <paramref name="action"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">TParameter 1 to pass through to the action.</param>
        /// <param name="action">The action to invoke.</param>
        public void Invoke<T1>(TInvoker invoker, T1 p1, Action<T1> action)
            => Invoker.RunSync(() => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => { (action ?? throw new ArgumentNullException(nameof(action))).Invoke(p1); return Task.FromResult<object?>(null!); }, CancellationToken.None));

        /// <summary>
        /// Invokes an <paramref name="action"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the action.</param>
        /// <param name="p2">Parameter 2 to pass through to the action.</param>
        /// <param name="action">The action to invoke.</param>
        public void Invoke<T1, T2>(TInvoker invoker, T1 p1, T2 p2, Action<T1, T2> action)
            => Invoker.RunSync(() => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => { (action ?? throw new ArgumentNullException(nameof(action))).Invoke(p1, p2); return Task.FromResult<object?>(null!); }, CancellationToken.None));

        /// <summary>
        /// Invokes an <paramref name="action"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the action.</param>
        /// <param name="p2">Parameter 2 to pass through to the action.</param>
        /// <param name="p3">Parameter 3 to pass through to the action.</param>
        /// <param name="action">The action to invoke.</param>
        public void Invoke<T1, T2, T3>(TInvoker invoker, T1 p1, T2 p2, T3 p3, Action<T1, T2, T3> action)
            => Invoker.RunSync(() => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => { (action ?? throw new ArgumentNullException(nameof(action))).Invoke(p1, p2, p3); return Task.FromResult<object?>(null!); }, CancellationToken.None));

        /// <summary>
        /// Invokes an <paramref name="action"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the action.</param>
        /// <param name="p2">Parameter 2 to pass through to the action.</param>
        /// <param name="p3">Parameter 3 to pass through to the action.</param>
        /// <param name="p4">Parameter 4 to pass through to the action.</param>
        /// <param name="action">The action to invoke.</param>
        public void Invoke<T1, T2, T3, T4>(TInvoker invoker, T1 p1, T2 p2, T3 p3, T4 p4, Action<T1, T2, T3, T4> action)
            => Invoker.RunSync(() => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => { (action ?? throw new ArgumentNullException(nameof(action))).Invoke(p1, p2, p3, p4); return Task.FromResult<object?>(null!); }, CancellationToken.None));

        #endregion

        #region Sync/Result

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The result.</returns>
        public TResult Invoke<TResult>(TInvoker invoker, Func<TResult> func)
            => Invoker.RunSync(() => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => Task.FromResult(func()), CancellationToken.None));

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">TParameter 1 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The result.</returns>
        public TResult Invoke<T1, TResult>(TInvoker invoker, T1 p1, Func<T1, TResult> func)
            => Invoker.RunSync(() => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => Task.FromResult(func(p1)), CancellationToken.None));

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The result.</returns>
        public TResult Invoke<T1, T2, TResult>(TInvoker invoker, T1 p1, T2 p2, Func<T1, T2, TResult> func)
            => Invoker.RunSync(() => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => Task.FromResult(func(p1, p2)), CancellationToken.None));

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The result.</returns>
        public TResult Invoke<T1, T2, T3, TResult>(TInvoker invoker, T1 p1, T2 p2, T3 p3, Func<T1, T2, T3, TResult> func)
            => Invoker.RunSync(() => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => Task.FromResult(func(p1, p2, p3)), CancellationToken.None));

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="p4">Parameter 4 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The result.</returns>
        public TResult Invoke<T1, T2, T3, T4, TResult>(TInvoker invoker, T1 p1, T2 p2, T3 p3, T4 p4, Func<T1, T2, T3, T4, TResult> func)
            => Invoker.RunSync(() => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => Task.FromResult(func(p1, p2, p3, p4)), CancellationToken.None));

        #endregion

        #region Async/NoResult

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task InvokeAsync(TInvoker invoker, Func<CancellationToken, Task> func, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(ct).ConfigureAwait(false); return (object?)null!; }, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">TParameter 1 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task InvokeAsync<T1>(TInvoker invoker, T1 p1, Func<T1, CancellationToken, Task> func, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(p1, ct).ConfigureAwait(false); return (object?)null!; }, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task InvokeAsync<T1, T2>(TInvoker invoker, T1 p1, T2 p2, Func<T1, T2, CancellationToken, Task> func, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(p1, p2, ct).ConfigureAwait(false); return (object?)null!; }, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task InvokeAsync<T1, T2, T3>(TInvoker invoker, T1 p1, T2 p2, T3 p3, Func<T1, T2, T3, CancellationToken, Task> func, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(p1, p2, p3, ct).ConfigureAwait(false); return (object?)null!; }, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="p4">Parameter 4 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task InvokeAsync<T1, T2, T3, T4>(TInvoker invoker, T1 p1, T2 p2, T3 p3, T4 p4, Func<T1, T2, T3, T4, CancellationToken, Task> func, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(p1, p2, p3, p4, ct).ConfigureAwait(false); return (object?)null!; }, cancellationToken);

        #endregion

        #region Async/Result

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<TResult>(TInvoker invoker, Func<CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(ct), cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">TParameter 1 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<T1, TResult>(TInvoker invoker, T1 p1, Func<T1, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(p1, ct), cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<T1, T2, TResult>(TInvoker invoker, T1 p1, T2 p2, Func<T1, T2, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(p1, p2, ct), cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<T1, T2, T3, TResult>(TInvoker invoker, T1 p1, T2 p2, T3 p3, Func<T1, T2, T3, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(p1, p2, p3, ct), cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="p4">Parameter 4 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<T1, T2, T3, T4, TResult>(TInvoker invoker, T1 p1, T2 p2, T3 p3, T4 p4, Func<T1, T2, T3, T4, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(p1, p2, p3, p4, ct), cancellationToken);

        #endregion
    }
}