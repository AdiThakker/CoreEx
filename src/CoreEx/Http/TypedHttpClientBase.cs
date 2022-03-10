﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Http
{
    /// <summary>
    /// Represents a typed <see cref="HttpClient"/> foundation wrapper.
    /// </summary>
    public abstract class TypedHttpClientBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypedHttpClientBase{TBase}"/>.
        /// </summary>
        /// <param name="client">The underlying <see cref="HttpClient"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        public TypedHttpClientBase(HttpClient client, IJsonSerializer jsonSerializer)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            JsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        }

        /// <summary>
        /// Gets the underlying <see cref="HttpClient"/>.
        /// </summary>
        protected HttpClient Client { get; }

        /// <summary>
        /// Gets the <see cref="IJsonSerializer"/>.
        /// </summary>
        protected IJsonSerializer JsonSerializer { get; }

        /// <summary>
        /// Create an <see cref="HttpRequestMessage"/> with no specified content.
        /// </summary>
        /// <param name="method">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
        protected HttpRequestMessage CreateRequest(HttpMethod method, string requestUri, HttpRequestOptions? requestOptions = null, params HttpArg[] args) 
            => CreateRequestInternal(method, requestUri, null, requestOptions, args);

        /// <summary>
        /// Create an <see cref="HttpRequestMessage"/> with the specified <paramref name="content"/>.
        /// </summary>
        /// <param name="method">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
        protected HttpRequestMessage CreateContentRequest(HttpMethod method, string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, params HttpArg[] args)
            => CreateRequestInternal(method, requestUri, content, requestOptions, args);

        /// <summary>
        /// Create an <see cref="HttpRequestMessage"/> serializing the <paramref name="value"/> as JSON content.
        /// </summary>
        /// <typeparam name="TReq">The request <see cref="Type"/>.</typeparam>
        /// <param name="method">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value to be serialized to JSON.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
        protected HttpRequestMessage CreateJsonRequest<TReq>(HttpMethod method, string requestUri, TReq value, HttpRequestOptions? requestOptions = null, params HttpArg[] args)
            => CreateContentRequest(method, requestUri, new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, MediaTypeNames.Application.Json), requestOptions, args);

        /// <summary>
        /// Create the request applying the specified options and args.
        /// </summary>
        private HttpRequestMessage CreateRequestInternal(HttpMethod method, string requestUri, HttpContent? content, HttpRequestOptions? requestOptions = null, params HttpArg[] args)
        {
            // Replace any format placeholders within request uri.
            requestUri = FormatReplacement(requestUri, args);

            // Access the query string.
            var uri = new Uri(requestUri, UriKind.RelativeOrAbsolute);

            UriBuilder ub;
            if (uri.IsAbsoluteUri)
                ub = new UriBuilder(uri.Scheme, uri.Host, uri.Port, uri.PathAndQuery);
            else
                ub = new UriBuilder("https", "coreex", 8080, requestUri);

            var qs = QueryString.FromUriComponent(ub.Query);

            // Extend the query string from the HttpArgs.
            foreach (var arg in (args ??= Array.Empty<HttpArg>()).Where(x => x != null && !x.IsUsed && x.ArgType != HttpArgType.FromBody && !x.IsDefault))
            {
                qs = arg.AddToQueryString(qs, JsonSerializer);
                arg.IsUsed = true;
            }

            // Extend the query string to include additional options.
            if (requestOptions != null)
                qs = requestOptions.AddToQueryString(qs);

            // Create the request and include ETag if any.
            ub.Query = qs.ToUriComponent();
            var request = new HttpRequestMessage(method, uri.IsAbsoluteUri ? ub.Uri.ToString() : ub.Uri.PathAndQuery).ApplyETag(requestOptions?.ETag);
            if (content != null)
                request.Content = content;

            // Apply the body/content HttpArg.
            var bodyArgs = args.Where(x => x != null && x.ArgType == HttpArgType.FromBody).ToList();
            if (bodyArgs.Count > 1)
                throw new ArgumentException("Only a single argument can have an ArgType of FromBody.", nameof(args));

            if (bodyArgs.Count > 0)
            {
                if (content != null)
                    throw new ArgumentException("An argument with an ArgType of FromBody is not supported where HttpContent is also provided.", nameof(args));

                var value = bodyArgs[0].GetValue();
                if (value != null)
                    request.Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, MediaTypeNames.Application.Json);
            }

            return request;
        }

        /// <summary>
        /// Format replacement inspired by: https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Logging.Abstractions/src/LogValuesFormatter.cs
        /// </summary>
        private static string FormatReplacement(string requestUri, HttpArg[] args)
        {
            var sb = new StringBuilder();
            var scanIndex = 0;
            var endIndex = requestUri.Length;

            while (scanIndex < endIndex)
            {
                var openBraceIndex = FindBraceIndex(requestUri, '{', scanIndex, endIndex);
                if (scanIndex == 0 && openBraceIndex == endIndex)
                    return requestUri;  // No holes found.

                var closeBraceIndex = FindBraceIndex(requestUri, '}', openBraceIndex, endIndex);
                if (closeBraceIndex == endIndex)
                {
                    sb.Append(requestUri, scanIndex, endIndex - scanIndex);
                    scanIndex = endIndex;
                }
                else
                {
                    sb.Append(requestUri, scanIndex, openBraceIndex - scanIndex);

                    var arg = args.Where(x => x != null && MemoryExtensions.Equals(requestUri.AsSpan(openBraceIndex + 1, closeBraceIndex - openBraceIndex - 1), x.Name, StringComparison.Ordinal)).FirstOrDefault();
                    if (arg != null)
                    {
                        sb.Append(arg.ToEscapeDataString());
                        arg.IsUsed = true;
                    }

                    scanIndex = closeBraceIndex + 1;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Find the brace index within specified range.
        /// </summary>
        private static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
        {
            // Example: {{prefix{{{Argument}}}suffix}}.
            var braceIndex = endIndex;
            var scanIndex = startIndex;
            var braceOccurenceCount = 0;

            while (scanIndex < endIndex)
            {
                if (braceOccurenceCount > 0 && format[scanIndex] != brace)
                {
                    if (braceOccurenceCount % 2 == 0)
                    {
                        // Even number of '{' or '}' found. Proceed search with next occurence of '{' or '}'.
                        braceOccurenceCount = 0;
                        braceIndex = endIndex;
                    }
                    else
                    {
                        // An unescaped '{' or '}' found.
                        break;
                    }
                }
                else if (format[scanIndex] == brace)
                {
                    if (brace == '}')
                    {
                        if (braceOccurenceCount == 0)
                        {
                            // For '}' pick the first occurence.
                            braceIndex = scanIndex;
                        }
                    }
                    else
                    {
                        // For '{' pick the last occurence.
                        braceIndex = scanIndex;
                    }

                    braceOccurenceCount++;
                }

                scanIndex++;
            }

            return braceIndex;
        }

        /// <summary>
        /// Deserialize the JSON <see cref="HttpResponseMessage.Content"/> into <see cref="Type"/> of <typeparamref name="TResp"/>.
        /// </summary>
        /// <typeparam name="TResp">The response <see cref="Type"/>.</typeparam>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <returns>The deserialized response value.</returns>
        protected async Task<TResp> ReadAsJsonAsync<TResp>(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            if (response.Content == null)
                return default!;

            var str = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<TResp>(str)!;
        }

        /// <summary>
        /// Sends the <paramref name="request"/> returning the <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        protected abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether the <paramref name="response"/> or <paramref name="exception"/> result is transient in nature, and as such is a candidate for a retry.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <returns><c>true</c> indicates transient; otherwaise, <c>false</c>.</returns>
        public static bool IsTransient(HttpResponseMessage? response = null, Exception? exception = null)
        {
            if (exception != null)
            {
                if (exception is HttpRequestException)
                    return true;

                if (exception is TaskCanceledException)
                    return true;
            }

            if (response == null)
                return false;

            if ((int)response.StatusCode >= 500)
                return true;

            if (response.StatusCode == HttpStatusCode.RequestTimeout)
                return true;

            return false;
        }
    }
}