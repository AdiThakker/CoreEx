﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides a custom validation against a specified property.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="System.Type"/>.</typeparam>
    public class CustomRule<TEntity, TProperty> : ValueRuleBase<TEntity, TProperty> where TEntity : class
    {
        private readonly Action<PropertyContext<TEntity, TProperty>>? _custom;
        private readonly Func<PropertyContext<TEntity, TProperty>, CancellationToken, Task>? _customAsync;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomRule{TEntity, TProperty}"/> class specifying the corresponding <paramref name="custom"/>.
        /// </summary>
        /// <param name="custom">The action to invoke to perform the custom validation.</param>
        public CustomRule(Action<PropertyContext<TEntity, TProperty>> custom) => _custom = custom ?? throw new ArgumentNullException(nameof(custom));

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomRule{TEntity, TProperty}"/> class specifying the corresponding <paramref name="customAsync"/>.
        /// </summary>
        /// <param name="customAsync">The action to invoke to perform the custom validation.</param>
        public CustomRule(Func<PropertyContext<TEntity, TProperty>, CancellationToken, Task> customAsync) => _customAsync = customAsync ?? throw new ArgumentNullException(nameof(customAsync));

        /// <summary>
        /// Validate the property value.
        /// </summary>
        /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public override async Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (_customAsync == null)
                _custom!(context);
            else
                await _customAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }
}