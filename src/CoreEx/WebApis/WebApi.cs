﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.Json;
using CoreEx.Json.Merge;
using CoreEx.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.WebApis
{
    /// <summary>
    /// Provides the core (<see cref="HttpMethods.Get"/>, <see cref="HttpMethods.Post"/>, <see cref="HttpMethods.Put"/> and <see cref="HttpMethods.Delete"/>) Web API execution encapsulation.
    /// </summary>
    public class WebApi : WebApiBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApi"/> class.
        /// </summary>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="invoker">The <see cref="WebApiInvoker"/>; defaults where not specified.</param>
        /// <param name="jsonMergePatch">The <see cref="IJsonMergePatch"/> to support the <see cref="HttpMethods.Patch"/> operations.</param>
        public WebApi(ExecutionContext executionContext, SettingsBase settings, IJsonSerializer jsonSerializer, ILogger<WebApi> logger, WebApiInvoker? invoker = null, IJsonMergePatch? jsonMergePatch = null)
            : base(executionContext, settings, jsonSerializer, logger, invoker) => JsonMergePatch = jsonMergePatch;

        /// <summary>
        /// Gets the <see cref="IJsonMergePatch"/>.
        /// </summary>
        public IJsonMergePatch? JsonMergePatch { get; }

        /// <summary>
        /// Indicates whether to convert a <see cref="NotFoundException"/> to the default <see cref="HttpStatusCode"/> on delete (see <see cref="DeleteAsync(HttpRequest, Func{WebApiParam, CancellationToken, Task}, HttpStatusCode, OperationType, CancellationToken)"/>.
        /// </summary>
        public bool ConvertNotfoundToDefaultStatusCodeOnDelete { get; } = true;

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> <paramref name="function"/> returning a corresponding <see cref="IActionResult"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function logic to invoke.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        /// <remarks>This is, and must be, used by all methods that process an <see cref="HttpRequest"/> to ensure that the standardized before and after, success and error, handling occurs as required.</remarks>
        public Task<IActionResult> RunAsync(HttpRequest request, Func<WebApiParam, Task<IActionResult>> function, OperationType operationType = OperationType.Unspecified)
            => RunAsync(request, (p, _) => function(p), operationType, CancellationToken.None);

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> <paramref name="function"/> returning a corresponding <see cref="IActionResult"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function logic to invoke.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>This is, and must be, used by all methods that process an <see cref="HttpRequest"/> to ensure that the standardized before and after, success and error, handling occurs as required.</remarks>
        public new Task<IActionResult> RunAsync(HttpRequest request, Func<WebApiParam, CancellationToken, Task<IActionResult>> function, OperationType operationType = OperationType.Unspecified, CancellationToken cancellationToken = default)
            => base.RunAsync(request, function, operationType, cancellationToken);

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> <paramref name="function"/> with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding <see cref="IActionResult"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function logic to invoke.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        public Task<IActionResult> RunAsync<TValue>(HttpRequest request, Func<WebApiParam<TValue>, Task<IActionResult>> function, OperationType operationType = OperationType.Unspecified,
            bool valueIsRequired = true, IValidator<TValue>? validator = null)
            => RunAsync(request, (p, ct) => function(p), operationType, valueIsRequired, validator, CancellationToken.None);

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> <paramref name="function"/> with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding <see cref="IActionResult"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function logic to invoke.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        public async Task<IActionResult> RunAsync<TValue>(HttpRequest request, Func<WebApiParam<TValue>, CancellationToken, Task<IActionResult>> function, OperationType operationType = OperationType.Unspecified,
            bool valueIsRequired = true, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async (wap, ct) =>
            {
                var vr = await request.ReadAsJsonValueAsync(JsonSerializer, valueIsRequired, validator, ct).ConfigureAwait(false);
                if (vr.IsInvalid)
                    return vr.ToBadRequestResult();

                return await function(new WebApiParam<TValue>(wap, vr.Value), ct).ConfigureAwait(false);
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        #region GetAsync

        /// <summary>
        /// Performs a <see cref="HttpMethods.Get"/> operation returning a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where result is not <c>null</c>.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> GetAsync<TResult>(HttpRequest request, Func<WebApiParam, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpStatusCode alternateStatusCode = HttpStatusCode.NotFound, OperationType operationType = OperationType.Read)
            => GetAsync(request, (p, _) => function(p), statusCode, alternateStatusCode, operationType, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Get"/> operation returning a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where result is not <c>null</c>.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public async Task<IActionResult> GetAsync<TResult>(HttpRequest request, Func<WebApiParam, CancellationToken, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK, 
            HttpStatusCode alternateStatusCode = HttpStatusCode.NotFound, OperationType operationType = OperationType.Read, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsGet(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Get}' to use {nameof(GetAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async (wap, ct) =>
            {
                var result = await function(wap, ct).ConfigureAwait(false);
                return ValueContentResult.CreateResult(result, statusCode, alternateStatusCode, JsonSerializer, wap.RequestOptions, checkForNotModified: true, location: null);
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Post

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with no request content or response value.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        public Task<IActionResult> PostAsync(HttpRequest request, Func<WebApiParam, Task> function, HttpStatusCode statusCode = HttpStatusCode.OK, OperationType operationType = OperationType.Create, Func<Uri>? locationUri = null)
            => PostAsync(request, (p, _) => function(p), statusCode, operationType, locationUri, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with no request content or response value.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public async Task<IActionResult> PostAsync(HttpRequest request, Func<WebApiParam, CancellationToken, Task> function, HttpStatusCode statusCode = HttpStatusCode.OK, OperationType operationType = OperationType.Create,
            Func<Uri>? locationUri = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPost(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PostAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async (wap, ct) =>
            {
                await function(wap, ct).ConfigureAwait(false);
                return new ExtendedStatusCodeResult(statusCode) { Location = locationUri?.Invoke() };
            }, operationType, cancellationToken).ConfigureAwait(false);                                                   
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PostAsync<TValue>(HttpRequest request, Func<WebApiParam<TValue>, Task> function, HttpStatusCode statusCode = HttpStatusCode.OK, OperationType operationType = OperationType.Create,
            bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<Uri>? locationUri = null)
            => PostAsync(request, (p, _) => function(p), statusCode, operationType, valueIsRequired, validator, locationUri, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PostAsync<TValue>(HttpRequest request, Func<WebApiParam<TValue>, CancellationToken, Task> function, HttpStatusCode statusCode = HttpStatusCode.OK, OperationType operationType = OperationType.Create,
            bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<Uri>? locationUri = null, CancellationToken cancellationToken = default)
            => PostInternalAsync(request, false, default!, function, statusCode, operationType, valueIsRequired, validator, locationUri, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PostAsync<TValue>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, Task> function, HttpStatusCode statusCode = HttpStatusCode.OK, OperationType operationType = OperationType.Create,
            bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<Uri>? locationUri = null)
            => PostAsync(request, value, (p, _) => function(p), statusCode, operationType, valueIsRequired, validator, locationUri, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PostAsync<TValue>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Create, bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<Uri>? locationUri = null, CancellationToken cancellationToken = default)
            => PostInternalAsync(request, true, value, function, statusCode, operationType, valueIsRequired, validator, locationUri, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value.
        /// </summary>
        private async Task<IActionResult> PostInternalAsync<TValue>(HttpRequest request, bool useValue, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Create, bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<Uri>? locationUri = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPost(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PostAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async (wap, ct) =>
            {
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, valueIsRequired, validator, cancellationToken).ConfigureAwait(false);
                if (vex != null)
                    return vex.ToResult();

                await function(wapv!, ct).ConfigureAwait(false);
                return new ExtendedStatusCodeResult(statusCode) { Location = locationUri?.Invoke() };
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with no request body and a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PostAsync<TResult>(HttpRequest request, Func<WebApiParam, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Create, Func<TResult, Uri>? locationUri = null)
            => PostAsync(request, (p, _) => function(p), statusCode, alternateStatusCode, operationType, locationUri, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with no request body and a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public async Task<IActionResult> PostAsync<TResult>(HttpRequest request, Func<WebApiParam, CancellationToken, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Create, Func<TResult, Uri>? locationUri = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPost(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PostAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async (wap, ct) =>
            {
                var result = await function(wap, ct).ConfigureAwait(false);
                return ValueContentResult.CreateResult(result, statusCode, alternateStatusCode, JsonSerializer, wap.RequestOptions, checkForNotModified: false, location: locationUri?.Invoke(result));
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PostAsync<TValue, TResult>(HttpRequest request, Func<WebApiParam<TValue>, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK, HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Create, bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<TResult, Uri>? locationUri = null)
            => PostAsync(request, (p, _) => function(p), statusCode, alternateStatusCode, operationType, valueIsRequired, validator, locationUri, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PostAsync<TValue, TResult>(HttpRequest request, Func<WebApiParam<TValue>, CancellationToken, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK, HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Create, bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<TResult, Uri>? locationUri = null, CancellationToken cancellationToken = default)
            => PostInternalAsync(request, false, default!, function, statusCode, alternateStatusCode, operationType, valueIsRequired, validator, locationUri, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PostAsync<TValue, TResult>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK, HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Create, bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<TResult, Uri>? locationUri = null)
            => PostAsync(request, value, (p, _) => function(p), statusCode, alternateStatusCode, operationType, valueIsRequired, validator, locationUri, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PostAsync<TValue, TResult>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK, HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Create, bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<TResult, Uri>? locationUri = null, CancellationToken cancellationToken = default)
            => PostInternalAsync(request, true, value, function, statusCode, alternateStatusCode, operationType, valueIsRequired, validator, locationUri, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        private async Task<IActionResult> PostInternalAsync<TValue, TResult>(HttpRequest request, bool useValue, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK, HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Create, bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<TResult, Uri>? locationUri = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPost(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PostAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async (wap, ct) =>
            {
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, valueIsRequired, validator, cancellationToken).ConfigureAwait(false);
                if (vex != null)
                    return vex.ToResult();

                var result = await function(wapv!, ct).ConfigureAwait(false);
                return ValueContentResult.CreateResult(result, statusCode, alternateStatusCode, JsonSerializer, wap.RequestOptions, checkForNotModified: false, location: locationUri?.Invoke(result));
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Put

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PutAsync<TValue>(HttpRequest request, Func<WebApiParam<TValue>, Task> function, HttpStatusCode statusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null)
            => PutAsync(request, (p, _) => function(p), statusCode, operationType, valueIsRequired, validator, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PutAsync<TValue>(HttpRequest request, Func<WebApiParam<TValue>, CancellationToken, Task> function, HttpStatusCode statusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PutInternalAsync(request, false, default!, function, statusCode, operationType, valueIsRequired, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PutAsync<TValue>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, Task> function, HttpStatusCode statusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null)
            => PutInternalAsync(request, true, value, (p, _) => function(p), statusCode, operationType, valueIsRequired, validator, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PutAsync<TValue>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task> function, HttpStatusCode statusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PutInternalAsync(request, true, value, function, statusCode, operationType, valueIsRequired, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value.
        /// </summary>
        private async Task<IActionResult> PutInternalAsync<TValue>(HttpRequest request, bool useValue, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task> function, HttpStatusCode statusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPut(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Put}' to use {nameof(PutAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async (wap, ct) =>
            {
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, valueIsRequired, validator, cancellationToken).ConfigureAwait(false);
                if (vex != null)
                    return vex.ToResult();

                await function(wapv!, ct).ConfigureAwait(false);
                return new ExtendedStatusCodeResult(statusCode);
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PutAsync<TValue, TResult>(HttpRequest request, Func<WebApiParam<TValue>, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null)
            => PutAsync(request, (p, _) => function(p), statusCode, alternateStatusCode, operationType, valueIsRequired, validator, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PutAsync<TValue, TResult>(HttpRequest request, Func<WebApiParam<TValue>, CancellationToken, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PutInternalAsync(request, false, default!, function, statusCode, alternateStatusCode, operationType, valueIsRequired, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PutAsync<TValue, TResult>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null)
            => PutAsync(request, value, (p, _) => function(p), statusCode, alternateStatusCode, operationType, valueIsRequired, validator, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PutAsync<TValue, TResult>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PutInternalAsync(request, true, value, function, statusCode, alternateStatusCode, operationType, valueIsRequired, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        public async Task<IActionResult> PutInternalAsync<TValue, TResult>(HttpRequest request, bool useValue, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPut(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Put}' to use {nameof(PutAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async (wap, ct) =>
            {
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, valueIsRequired, validator, cancellationToken).ConfigureAwait(false);
                if (vex != null)
                    return vex.ToResult();

                var result = await function(wapv!, ct).ConfigureAwait(false);
                return ValueContentResult.CreateResult(result, statusCode, alternateStatusCode, JsonSerializer, wap.RequestOptions, checkForNotModified: false, location: null);
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding response value.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="get">The function to execute the <i>get</i> to retrieve the value. Where this returns a <c>null</c> then this will result in a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/>.</param>
        /// <param name="put">The function to execute the <i>put</i> to replace (update) the value.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="simulatedConcurrency">Indicates whether simulated concurrency (ETag) checking/generation is performed as underlying data source does not support.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PutAsync<TValue>(HttpRequest request, Func<WebApiParam, Task<TValue?>> get, Func<WebApiParam<TValue>, Task<TValue>> put, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Update, IValidator<TValue>? validator = null, bool simulatedConcurrency = false) where TValue : class
            => PutAsync(request, (p, _) => get(p), (p, _) => put(p), statusCode, operationType, validator, simulatedConcurrency, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding response value.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="get">The function to execute the <i>get</i> to retrieve the value. Where this returns a <c>null</c> then this will result in a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/>.</param>
        /// <param name="put">The function to execute the <i>put</i> to replace (update) the value.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="simulatedConcurrency">Indicates whether simulated concurrency (ETag) checking/generation is performed as underlying data source does not support.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PutAsync<TValue>(HttpRequest request, Func<WebApiParam, CancellationToken, Task<TValue?>> get, Func<WebApiParam<TValue>, CancellationToken, Task<TValue>> put, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Update, IValidator<TValue>? validator = null, bool simulatedConcurrency = false, CancellationToken cancellationToken = default) where TValue : class
            => PutInternalAsync(request, false, default!, get, put, statusCode, operationType, validator, simulatedConcurrency, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding response value.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="get">The function to execute the <i>get</i> to retrieve the value. Where this returns a <c>null</c> then this will result in a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/>.</param>
        /// <param name="put">The function to execute the <i>put</i> to replace (update) the value.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="simulatedConcurrency">Indicates whether simulated concurrency (ETag) checking/generation is performed as underlying data source does not support.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PutAsync<TValue>(HttpRequest request, TValue value, Func<WebApiParam, Task<TValue?>> get, Func<WebApiParam<TValue>, Task<TValue>> put, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Update, IValidator<TValue>? validator = null, bool simulatedConcurrency = false) where TValue : class
            => PutAsync(request, value, (p, _) => get(p), (p, _) => put(p), statusCode, operationType, validator, simulatedConcurrency, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding response value.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="get">The function to execute the <i>get</i> to retrieve the value. Where this returns a <c>null</c> then this will result in a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/>.</param>
        /// <param name="put">The function to execute the <i>put</i> to replace (update) the value.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="simulatedConcurrency">Indicates whether simulated concurrency (ETag) checking/generation is performed as underlying data source does not support.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PutAsync<TValue>(HttpRequest request, TValue value, Func<WebApiParam, CancellationToken, Task<TValue?>> get, Func<WebApiParam<TValue>, CancellationToken, Task<TValue>> put, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Update, IValidator<TValue>? validator = null, bool simulatedConcurrency = false, CancellationToken cancellationToken = default) where TValue : class
            => PutInternalAsync(request, true, value, get, put, statusCode, operationType, validator, simulatedConcurrency, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding response value.
        /// </summary>
        private async Task<IActionResult> PutInternalAsync<TValue>(HttpRequest request, bool useValue, TValue value, Func<WebApiParam, CancellationToken, Task<TValue?>> get, Func<WebApiParam<TValue>, CancellationToken, Task<TValue>> put, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Update, IValidator<TValue>? validator = null, bool simulatedConcurrency = false, CancellationToken cancellationToken = default) where TValue : class
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPut(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Put}' to use {nameof(PutAsync)}.", nameof(request));

            if (get == null)
                throw new ArgumentNullException(nameof(get));

            if (put == null)
                throw new ArgumentNullException(nameof(put));

            return await RunAsync(request, async (wap, ct) =>
            {
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, true, validator, cancellationToken).ConfigureAwait(false);
                if (vex != null)
                    return vex.ToResult();

                // Get the current value before we perform the update.
                var cvalue = await get(wap, ct).ConfigureAwait(false);
                if (cvalue == null)
                    throw new NotFoundException();

                // Perform concurrency match.
                ConcurrencyETagMatching(wap, cvalue, wapv!.Value, simulatedConcurrency);

                // Update the value.
                var result = await put(wapv, ct).ConfigureAwait(false);
                return ValueContentResult.CreateResult(result, statusCode, null, JsonSerializer, wap.RequestOptions, checkForNotModified: false, location: null);
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Where etags are supported or automatic concurrency then we need to make sure one was provided up-front and match.
        /// </summary>
        private void ConcurrencyETagMatching<TValue>(WebApiParam wap, TValue getValue, TValue putValue, bool autoConcurrency)
        {
            var et = putValue as IETag;
            if (et != null || autoConcurrency)
            {
                string? etag = et?.ETag ?? wap.RequestOptions.ETag;
                if (string.IsNullOrEmpty(etag))
                    throw new ConcurrencyException($"An 'If-Match' header is required for an HTTP {wap.Request.Method} where the underlying entity supports concurrency (ETag).");

                if (etag != null)
                {
                    if (!ValueContentResult.TryGetETag(getValue!, out var getEt))
                        getEt = ValueContentResult.GenerateETag(wap.RequestOptions, getValue!, null, JsonSerializer);

                    if (etag != getEt)
                        throw new ConcurrencyException();
                }
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Performs a <see cref="HttpMethods.Delete"/> operation.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where result is not <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> DeleteAsync(HttpRequest request, Func<WebApiParam, Task> function, HttpStatusCode statusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Delete)
            => DeleteAsync(request, (p, _) => function(p), statusCode, operationType, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Delete"/> operation.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where result is not <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public async Task<IActionResult> DeleteAsync(HttpRequest request, Func<WebApiParam, CancellationToken, Task> function, HttpStatusCode statusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Delete, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsDelete(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Delete}' to use {nameof(DeleteAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async (wap, ct) =>
            {
                try
                {
                    await function(wap, ct).ConfigureAwait(false);
                }
                catch (NotFoundException) when (ConvertNotfoundToDefaultStatusCodeOnDelete) { /* Return default status code. */ }

                return new ExtendedStatusCodeResult(statusCode);
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Patch

        /// <summary>
        /// Performs a <see cref="HttpMethods.Patch"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding response value.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="get">The function to execute the <i>get</i> to retrieve the value to patch into. Where this returns a <c>null</c> then this will result in a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/>.</param>
        /// <param name="put">The function to execute the <i>put</i> to replace (update) the patched value.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="simulatedConcurrency">Indicates whether simulated concurrency (ETag) checking/generation is performed as underlying data source does not support.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>Currently on the <see cref="JsonMergePatch"/> is supported.</remarks>
        public Task<IActionResult> PatchAsync<TValue>(HttpRequest request, Func<WebApiParam, Task<TValue?>> get, Func<WebApiParam<TValue>, Task<TValue>> put, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Update, IValidator<TValue>? validator = null, bool simulatedConcurrency = false) where TValue : class
            => PatchAsync(request, (p, _) => get(p), (p, _) => put(p), statusCode, operationType, validator, simulatedConcurrency, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Patch"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding response value.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="get">The function to execute the <i>get</i> to retrieve the value to patch into. Where this returns a <c>null</c> then this will result in a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/>.</param>
        /// <param name="put">The function to execute the <i>put</i> to replace (update) the patched value.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="simulatedConcurrency">Indicates whether simulated concurrency (ETag) checking/generation is performed as underlying data source does not support.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>Currently on the <see cref="JsonMergePatch"/> is supported.</remarks>
        public async Task<IActionResult> PatchAsync<TValue>(HttpRequest request, Func<WebApiParam, CancellationToken, Task<TValue?>> get, Func<WebApiParam<TValue>, CancellationToken, Task<TValue>> put, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Update, IValidator<TValue>? validator = null, bool simulatedConcurrency = false, CancellationToken cancellationToken = default) where TValue : class
        {
            if (JsonMergePatch == null)
                throw new InvalidOperationException($"To use the '{nameof(PatchAsync)}' methods the '{nameof(JsonMergePatch)}' object must be passed in the constructor. Where using dependency injection consider using '{nameof(Microsoft.Extensions.DependencyInjection.IServiceCollectionExtensions.AddJsonMergePatch)}' to add and configure the supported options.");

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPatch(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Patch}' to use {nameof(PatchAsync)}.", nameof(request));

            if (get == null)
                throw new ArgumentNullException(nameof(get));

            if (put == null)
                throw new ArgumentNullException(nameof(put));

            return await RunAsync(request, async (wap, ct) =>
            {
                // Make sure that the only the support content types are used.
                var hct = request.GetTypedHeaders()?.ContentType?.MediaType.Value;
                if (StringComparer.OrdinalIgnoreCase.Compare(hct, HttpConsts.MergePatchMediaTypeName) != 0 && StringComparer.OrdinalIgnoreCase.Compare(hct, MediaTypeNames.Application.Json) != 0)
                    return new ContentResult 
                    { 
                        StatusCode = (int)HttpStatusCode.UnsupportedMediaType,
                        ContentType = MediaTypeNames.Text.Plain, 
                        Content = $"Unsupported 'Content-Type' for a PATCH; only JSON Merge Patch is supported using either: 'application/merge-patch+json' or '{MediaTypeNames.Application.Json}'."
                    };

                // Retrieve the JSON content string; there must be some content of some type.
                var json = await request.ReadAsStringAsync(true, ct).ConfigureAwait(false);
                if (json.Exception != null)
                    return json.Exception.ToResult();

                // Note: the JsonMergePatch will throw JsonMergePatchException on error which will be automatically converted to an appropriate IActionResult by the invoking RunAsync method.
                var (HasChanges, Value) = await JsonMergePatch.MergeAsync<TValue>(json.Content!, async (jpv, ct2) =>
                {
                    // Get the current value before we perform the merge.
                    var value = await get(wap, ct2).ConfigureAwait(false);
                    if (value == null)
                        throw new NotFoundException();

                    // Perform concurrency match.
                    ConcurrencyETagMatching(wap, value, jpv, simulatedConcurrency);

                    return value;
                }, ct).ConfigureAwait(false);

                // Only invoke the put function where something was *actually* changed.
                if (HasChanges)
                {
                    if (validator != null)
                    {
                        var vr = await validator.ValidateAsync(Value!, ct).ConfigureAwait(false);
                        if (vr.HasErrors)
                            return vr.ToValidationException()!.ToResult();
                    }

                    Value = await put(new WebApiParam<TValue>(wap, Value!), ct).ConfigureAwait(false);
                }

                return ValueContentResult.CreateResult(Value, statusCode, null, JsonSerializer, wap.RequestOptions, checkForNotModified: false, location: null);
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}