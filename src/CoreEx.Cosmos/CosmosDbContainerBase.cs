﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using Microsoft.Azure.Cosmos;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Provides base <see cref="Container"/> operations for a <see cref="CosmosDb"/> container.
    /// </summary>
    /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    /// <typeparam name="TSelf">The <see cref="Type"/> itself.</typeparam>
    public abstract class CosmosDbContainerBase<T, TModel, TSelf> : ICosmosDbContainer<T, TModel> where T : class, IEntityKey, new() where TModel : class, IIdentifier, new() where TSelf : CosmosDbContainerBase<T, TModel, TSelf>
    {
        private Func<T, PartitionKey>? _partitionKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbContainerBase{T, TModel, TSelf}"/> class.
        /// </summary>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="containerId">The <see cref="Microsoft.Azure.Cosmos.Container"/> identifier.</param>
        public CosmosDbContainerBase(ICosmosDb cosmosDb, string containerId)
        {
            CosmosDb = cosmosDb ?? throw new ArgumentNullException(nameof(cosmosDb));
            Container = cosmosDb.GetCosmosContainer(containerId);
        }

        /// <inheritdoc/>
        public ICosmosDb CosmosDb { get; }

        /// <inheritdoc/>
        public Container Container { get; }

        /// <summary>
        /// Gets the <see cref="PartitionKey"/> from the <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to infer <see cref="PartitionKey"/> from.</param>
        /// <returns>The <see cref="PartitionKey"/>.</returns>
        /// <exception cref="AuthorizationException">Will be thrown where the infered <see cref="PartitionKey"/> is not equal to <see cref="ICosmosDb.PartitionKey"/> (where not <c>null</c>).</exception>
        public PartitionKey GetPartitionKey(T value)
        {
            var dbpk = CosmosDb.PartitionKey;
            var pk = _partitionKey?.Invoke(value) ?? CosmosDb.PartitionKey ?? PartitionKey.None;
            if (dbpk is not null && dbpk != PartitionKey.None && dbpk != pk)
                throw new AuthorizationException();

            return pk;
        }

        /// <summary>
        /// Sets the function to determine the <see cref="PartitionKey"/>; used for <see cref="GetPartitionKey(T)"/>.
        /// </summary>
        /// <param name="partitionKey">The function to determine the <see cref="PartitionKey"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public TSelf UsePartitionKey(Func<T, PartitionKey> partitionKey)
        {
            _partitionKey = partitionKey;
            return (TSelf)this;
        }

        /// <summary>
        /// Gets the <b>CosmosDb</b> identifier from the <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <returns>The <b>CosmosDb</b> identifier.</returns>
        /// <remarks>Only supports a single key value; therefore, the <see cref="CompositeKey.Args"/> length must be one (1) otherwise throws a <see cref="NotSupportedException"/>.</remarks>
        public string GetCosmosId(CompositeKey key) => key.Args.Length == 1 ? GetCosmosId(key.Args[0]) : throw new NotSupportedException("Only an underlying single key value that is a string is supported.");

        /// <summary>
        /// Gets the <b>CosmosDb</b> identifier from the external <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The external identifier.</param>
        /// <returns>The <b>CosmosDb</b> identifier.</returns>
        /// <remarks>Uses the <see cref="CosmosDb.FormatIdentifier(object?)"/> to format the <paramref name="id"/> as a string (as required).</remarks>
        public virtual string GetCosmosId(object? id) => CosmosDb.FormatIdentifier(id);

        /// <summary>
        ///  Gets the <b>CosmosDb</b> representation key from the <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The entity value.</param>
        /// <returns>The <b>CosmosDb</b> identifier.</returns>
        public string GetCosmosId(T value) => GetCosmosId((value ?? throw new ArgumentNullException(nameof(value))).EntityKey);

        /// <summary>
        /// Gets the entity for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<T?> GetAsync(object? id, CancellationToken cancellationToken = default) => GetAsync(id, new CosmosDbArgs(CosmosDb.DbArgs), cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="ICosmosDb.PartitionKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<T?> GetAsync(object? id, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => GetAsync(id, new CosmosDbArgs(CosmosDb.DbArgs, partitionKey), cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public abstract Task<T?> GetAsync(object? id, CosmosDbArgs dbArgs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates the entity.
        /// </summary>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public Task<T> CreateAsync(T value, CancellationToken cancellationToken = default) => CreateAsync(value, new CosmosDbArgs(CosmosDb.DbArgs), cancellationToken);

        /// <summary>
        /// Creates the entity.
        /// </summary>
        /// <param name="value">The value to create.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public abstract Task<T> CreateAsync(T value, CosmosDbArgs dbArgs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the entity.
        /// </summary>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public Task<T> UpdateAsync(T value, CancellationToken cancellationToken = default) => UpdateAsync(value, new CosmosDbArgs(CosmosDb.DbArgs), cancellationToken);

        /// <summary>
        /// Updates the entity.
        /// </summary>
        /// <param name="value">The value to update.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public abstract Task<T> UpdateAsync(T value, CosmosDbArgs dbArgs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the entity for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task DeleteAsync(object? id, CancellationToken cancellationToken = default) => DeleteAsync(id, new CosmosDbArgs(CosmosDb.DbArgs), cancellationToken);

        /// <summary>
        /// Deletes the entity for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="ICosmosDb.PartitionKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task DeleteAsync(object? id, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => DeleteAsync(id, new CosmosDbArgs(CosmosDb.DbArgs, partitionKey), cancellationToken);

        /// <summary>
        /// Deletes the entity for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public abstract Task DeleteAsync(object? id, CosmosDbArgs dbArgs, CancellationToken cancellationToken = default);
    }
}