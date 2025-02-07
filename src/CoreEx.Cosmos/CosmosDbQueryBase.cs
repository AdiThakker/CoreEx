﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Enables the common <b>CosmosDb</b> query capabilities.
    /// </summary>
    /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    /// <typeparam name="TSelf">The <see cref="Type"/> itself.</typeparam>
    public abstract class CosmosDbQueryBase<T, TModel, TSelf> where T : class, new() where TModel : class, new() where TSelf : CosmosDbQueryBase<T, TModel, TSelf>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbQueryBase{T, TModel, TSelf}"/> class.
        /// </summary>
        protected CosmosDbQueryBase(ICosmosDbContainer container, CosmosDbArgs dbArgs)
        {
            Container = container ?? throw new ArgumentNullException(nameof(container));
            QueryArgs = dbArgs;
        }

        /// <summary>
        /// Gets the <see cref="ICosmosDbContainer"/>.
        /// </summary>
        public ICosmosDbContainer Container { get; }

        /// <summary>
        /// Gets the <see cref="CosmosDbArgs"/>.
        /// </summary>
        public CosmosDbArgs QueryArgs;

        /// <summary>
        /// Gets the <see cref="PagingResult"/>.
        /// </summary>
        public PagingResult? Paging { get; protected set; }

        /// <summary>
        /// Adds <see cref="PagingArgs"/> to the query.
        /// </summary>
        /// <param name="paging">The <see cref="PagingArgs"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to suport fluent-style method-chaining.</returns>
        public TSelf WithPaging(PagingArgs paging)
        {
            Paging = paging == null ? null : (paging is PagingResult pr ? pr : new PagingResult(paging));
            return (TSelf)this;
        }

        /// <summary>
        /// Adds <see cref="PagingArgs"/> to the query.
        /// </summary>
        /// <param name="skip">The specified number of elements in a sequence to bypass.</param>
        /// <param name="take">The specified number of contiguous elements from the start of a sequence.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to suport fluent-style method-chaining.</returns>
        public TSelf WithPaging(long skip, long? take = null) => WithPaging(PagingArgs.CreateSkipAndTake(skip, take));

        /// <summary>
        /// Selects a single item.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item.</returns>
        /// <remarks><see cref="Paging"/> is not supported for this operation.</remarks>
        public async Task<T> SelectSingleAsync(CancellationToken cancellationToken = default) => (await SelectArrayAsync(nameof(SelectSingleAsync), 2, cancellationToken).ConfigureAwait(false)).Single();

        /// <summary>
        /// Selects a single item or default.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        /// <remarks><see cref="Paging"/> is not supported for this operation.</remarks>
        public async Task<T?> SelectSingleOrDefaultAsync(CancellationToken cancellationToken = default) => (await SelectArrayAsync(nameof(SelectSingleOrDefaultAsync), 2, cancellationToken).ConfigureAwait(false)).SingleOrDefault();

        /// <summary>
        /// Selects first item.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The first item.</returns>
        /// <remarks><see cref="Paging"/> is not supported for this operation.</remarks>
        public async Task<T> SelectFirstAsync(CancellationToken cancellationToken = default) => (await SelectArrayAsync(nameof(SelectFirstAsync), 1, cancellationToken).ConfigureAwait(false)).First();

        /// <summary>
        /// Selects first item or default.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        /// <remarks><see cref="Paging"/> is not supported for this operation.</remarks>
        public async Task<T?> SelectFirstOrDefaultAsync(CancellationToken cancellationToken = default) => (await SelectArrayAsync(nameof(SelectFirstOrDefaultAsync), 1, cancellationToken).ConfigureAwait(false)).FirstOrDefault();

        /// <summary>
        /// Selects an array by limiting the data retrieved.
        /// </summary>
        private Task<T[]> SelectArrayAsync(string caller, long take, CancellationToken cancellationToken)
        {
            if (Paging != null)
                throw new InvalidOperationException($"The {nameof(Paging)} must be null for a {caller}; internally applied paging is needed to limit unnecessary data retrieval.");

            WithPaging(0, take);
            return ToArrayAsync(cancellationToken);
        }

        /// <summary>
        /// Executes the query command creating a resultant collection.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A resultant collection.</returns>
        /// <remarks>The <see cref="Paging"/> is also applied, including <see cref="PagingArgs.IsGetCount"/> where requested.</remarks>
        public async Task<TColl> SelectQueryAsync<TColl>(CancellationToken cancellationToken = default) where TColl : ICollection<T>, new()
        {
            var coll = new TColl();
            await SelectQueryAsync(coll, cancellationToken).ConfigureAwait(false);
            return coll;
        }

        /// <summary>
        /// Executes the query adding to the passed collection.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="coll">The collection to add items to.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>The <see cref="Paging"/> is also applied, including <see cref="PagingArgs.IsGetCount"/> where requested.</remarks>
        public abstract Task SelectQueryAsync<TColl>(TColl coll, CancellationToken cancellationToken = default) where TColl : ICollection<T>;

        /// <summary>
        /// Executes the query command creating a resultant array.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A resultant array.</returns>
        /// <remarks>The <see cref="Paging"/> is also applied, including <see cref="PagingArgs.IsGetCount"/> where requested.</remarks>
        public async Task<T[]> ToArrayAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<T>();
            await SelectQueryAsync(list, cancellationToken).ConfigureAwait(false);
            return list.ToArray();
        }

        /// <summary>
        /// Executes the query command creating a <typeparamref name="TCollResult"/>.
        /// </summary>
        /// <typeparam name="TCollResult">The <see cref="ICollectionResult{TColl, TItem}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The <see cref="ICollection{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <typeparamref name="TCollResult"/>.</returns>
        /// <remarks>The <see cref="Paging"/> is also applied, including <see cref="PagingArgs.IsGetCount"/> where requested.</remarks>
        public async Task<TCollResult> SelectResultAsync<TCollResult, TColl>(CancellationToken cancellationToken = default) where TCollResult : ICollectionResult<TColl, T>, new() where TColl : ICollection<T>, new() => new TCollResult
        {
            Paging = Paging ?? new PagingResult(),
            Items = await SelectQueryAsync<TColl>(cancellationToken).ConfigureAwait(false)
        };
    }
}