﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Encapsulates a <b>CosmosDb</b> query enabling all select-like capabilities.
    /// </summary>
    /// <typeparam name="T">The resultant <see cref="CosmosDbValue{T}"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    public class CosmosDbValueQuery<T, TModel> : CosmosDbQueryBase<T, TModel, CosmosDbValueQuery<T, TModel>> where T : class, IEntityKey, new() where TModel : class, IIdentifier, new()
    {
        private readonly Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? _query;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbValueQuery{T, TModel}"/> class.
        /// </summary>
        /// <param name="container">The <see cref="CosmosDbValueContainer{T, TModel}"/>.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="query">A function to modify the underlying <see cref="IQueryable{T}"/>.</param>
        public CosmosDbValueQuery(CosmosDbValueContainer<T, TModel> container, CosmosDbArgs dbArgs, Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query) : base(container, dbArgs) => _query = query;

        /// <summary>
        /// Gets the <see cref="CosmosDbValueContainer{T, TModel}"/>.
        /// </summary>
        public new CosmosDbValueContainer<T, TModel> Container => (CosmosDbValueContainer<T, TModel>)base.Container;

        /// <summary>
        /// Instantiates the <see cref="IQueryable"/>.
        /// </summary>
        private IQueryable<CosmosDbValue<TModel>> AsQueryable(bool allowSynchronousQueryExecution, bool pagingSupported)
        {
            if (!pagingSupported && Paging is not null)
                throw new NotSupportedException("Paging is not supported when accessing AsQueryable directly; paging must be applied directly to the resulting IQueryable instance.");

            IQueryable<CosmosDbValue<TModel>> query = Container.Container.GetItemLinqQueryable<CosmosDbValue<TModel>>(allowSynchronousQueryExecution: allowSynchronousQueryExecution, requestOptions: Container.CosmosDb.GetQueryRequestOptions<T, TModel>(QueryArgs));
            query = (_query == null ? query : _query(query)).WhereType(typeof(TModel));

            var filter = Container.CosmosDb.GetAuthorizeFilter<TModel>(Container.Container.Id);
            if (filter != null)
                query = (IQueryable<CosmosDbValue<TModel>>)filter(query);

            return query;
        }

        /// <summary>
        /// Gets a pre-prepared <see cref="IQueryable"/> with filtering applied as applicable.
        /// </summary>
        /// <returns>The <see cref="IQueryable"/>.</returns>
        /// <remarks>The <see cref="CosmosDbQueryBase{T, TModel, TSelf}.Paging"/> is not supported. The query will <i>not</i> be automatically included within an <see cref="CosmosDb.Invoker"/> execution.</remarks>
        public IQueryable<CosmosDbValue<TModel>> AsQueryable() => AsQueryable(true, false);

        /// <inheritdoc/>
        public override Task SelectQueryAsync<TColl>(TColl coll, CancellationToken cancellationToken = default) => Container.CosmosDb.Invoker.InvokeAsync(Container.CosmosDb, coll, async (items, ct) =>
        {
            var q = AsQueryable(false, true);

            using var iterator = q.WithPaging(Paging).ToFeedIterator();
            while (iterator.HasMoreResults)
            {
                foreach (var item in await iterator.ReadNextAsync(ct).ConfigureAwait(false))
                {
                    items.Add(Container.GetValue(item));
                }
            }

            if (Paging != null && Paging.IsGetCount)
                Paging.TotalCount = (await q.CountAsync(cancellationToken).ConfigureAwait(false)).Resource;
        }, cancellationToken);
    }
}