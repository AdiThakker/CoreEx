﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Localization;
using CoreEx.Validation.Rules;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Provides dictionary validation.
    /// </summary>
    /// <typeparam name="TDict">The dictionary <see cref="Type"/>.</typeparam>
    /// <typeparam name="TKey">The key <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    public class DictionaryValidator<TDict, TKey, TValue> : ValidatorBase<TDict>
        where TDict : class, IDictionary<TKey, TValue>
    {
        private IDictionaryRuleItem? _item;
        private Func<ValidationContext<TDict>, CancellationToken, Task>? _additionalAsync;

        /// <summary>
        /// Indicates whether the underlying dictionary key can be null.
        /// </summary>
        public bool AllowNullKeys { get; set; }

        /// <summary>
        /// Indicates whether the underlying dictionary value can be null.
        /// </summary>
        public bool AllowNullValues { get; set; }

        /// <summary>
        /// Gets or sets the minimum count;
        /// </summary>
        public int MinCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum count.
        /// </summary>
        public int? MaxCount { get; set; }

        /// <summary>
        /// Gets or sets the collection item validation configuration.
        /// </summary>
        public IDictionaryRuleItem? Item
        {
            get => _item;

            set
            {
                if (value == null)
                {
                    _item = value;
                    return;
                }

                if (typeof(TKey) != value.KeyType)
                    throw new ArgumentException($"A CollectionRule TProperty Key Type '{typeof(TKey).Name}' must be the same as the Key {value.KeyType.Name}.");

                if (typeof(TValue) != value.ValueType)
                    throw new ArgumentException($"A CollectionRule TProperty Value Type '{typeof(TValue).Name}' must be the same as the Value {value.ValueType.Name}.");

                _item = value;
            }
        }

        /// <summary>
        /// Gets or sets the friendly text name used in validation messages.
        /// </summary>
        /// <remarks>Defaults to the <see cref="ValidationArgs.FullyQualifiedEntityName"/> formatted as sentence case where specified; otherwise, 'Value'.</remarks>
        public LText? Text { get; set; }

        /// <inheritdoc/>
        public override async Task<ValidationContext<TDict>> ValidateAsync(TDict? value, ValidationArgs? args = null, CancellationToken cancellationToken = default)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            args ??= new ValidationArgs();
            if (string.IsNullOrEmpty(args.FullyQualifiedEntityName))
                args.FullyQualifiedEntityName = Validation.ValueNameDefault;

            if (string.IsNullOrEmpty(args.FullyQualifiedEntityName))
                args.FullyQualifiedJsonEntityName = Validation.ValueNameDefault;

            var context = new ValidationContext<TDict>(value, args);

            var i = 0;
            var hasNullKey = false;
            var hasNullValue = false;
            foreach (var item in value)
            {
                i++;

                if (!AllowNullKeys && item.Key == null)
                    hasNullKey = true;

                if (!AllowNullValues && item.Value == null)
                    hasNullValue = true;

                if (Item?.KeyValidator == null && Item?.ValueValidator == null)
                    continue;

                // Validate and merge.
                var name = $"[{item.Key}]";

                if (item.Key != null && Item?.KeyValidator != null)
                {
                    var kc = new PropertyContext<TDict, KeyValuePair<TKey, TValue>>(context, item, name, name);
                    var ka = kc.CreateValidationArgs();
                    var kr = await Item.KeyValidator.ValidateAsync(item.Key, ka, cancellationToken).ConfigureAwait(false);
                    context.MergeResult(kr);
                }

                if (item.Value != null && Item?.ValueValidator != null)
                {
                    var vc = new PropertyContext<TDict, KeyValuePair<TKey, TValue>>(context, item, name, name);
                    var va = vc.CreateValidationArgs();
                    var vr = await Item.ValueValidator.ValidateAsync(item.Value, va, cancellationToken).ConfigureAwait(false);
                    context.MergeResult(vr);
                }
            }

            var text = new Lazy<LText>(() => Text ?? PropertyExpression.ConvertToSentenceCase(args?.FullyQualifiedEntityName) ?? Validation.ValueNameDefault);
            if (hasNullKey)
                context.AddMessage(Entities.MessageType.Error, ValidatorStrings.DictionaryNullKeyFormat, new object?[] { text.Value, null });

            if (hasNullValue)
                context.AddMessage(Entities.MessageType.Error, ValidatorStrings.DictionaryNullValueFormat, new object?[] { text.Value, null });

            // Check the length/count.
            if (i < MinCount)
                context.AddMessage(Entities.MessageType.Error, ValidatorStrings.MinCountFormat, new object?[] { text.Value, null, MinCount });
            else if (MaxCount.HasValue && i > MaxCount.Value)
                context.AddMessage(Entities.MessageType.Error, ValidatorStrings.MaxCountFormat, new object?[] { text.Value, null, MaxCount });

            await OnValidateAsync(context, cancellationToken).ConfigureAwait(false);
            if (_additionalAsync != null)
                await _additionalAsync(context, cancellationToken).ConfigureAwait(false);

            return context;
        }

        /// <summary>
        /// Validate the entity value (post all configured property rules) enabling additional validation logic to be added by the inheriting classes.
        /// </summary>
        /// <param name="context">The <see cref="ValidationContext{TEntity}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="Task"/>.</returns>
        protected virtual Task OnValidateAsync(ValidationContext<TDict> context, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Validate the entity value (post all configured property rules) enabling additional validation logic to be added.
        /// </summary>
        /// <param name="additionalAsync">The asynchronous function to invoke.</param>
        /// <returns>The <see cref="DictionaryValidator{TColl, TKey, TValue}"/>.</returns>
        public DictionaryValidator<TDict, TKey, TValue> AdditionalAsync(Func<ValidationContext<TDict>, CancellationToken, Task> additionalAsync)
        {
            if (_additionalAsync != null)
                throw new InvalidOperationException("Additional can only be defined once for a DictionaryValidator.");

            _additionalAsync = additionalAsync ?? throw new ArgumentNullException(nameof(additionalAsync));
            return this;
        }
    }
}