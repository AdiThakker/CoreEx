﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Abstractions.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CoreEx.Localization;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides validation configuration for an item within a <see cref="CollectionRule{TEntity, TProperty}"/>.
    /// </summary>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    public sealed class CollectionRuleItem<TItem> : ICollectionRuleItem
    {
        private bool _duplicateCheck = false;
        private IPropertyExpression? _propertyExpression;
        private LText? _duplicateText = null;
        private bool _ignoreWhereKeyIsInitial = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionRuleItem{TItem}"/> class with a corresponding <paramref name="validator"/>.
        /// </summary>
        /// <param name="validator">The corresponding item <see cref="IValidatorEx{TItem}"/>.</param>
        internal CollectionRuleItem(IValidatorEx<TItem>? validator) => ItemValidator = validator;

        /// <summary>
        /// Gets the corresponding item <see cref="IValidatorEx"/>.
        /// </summary>
        IValidatorEx? ICollectionRuleItem.ItemValidator => ItemValidator;

        /// <summary>
        /// Gets the corresponding item <see cref="IValidatorEx{TItemEntity}"/>.
        /// </summary>
        public IValidatorEx<TItem>? ItemValidator { get; private set; }

        /// <summary>
        /// Gets the item <see cref="Type"/>.
        /// </summary>
        public Type ItemType => typeof(TItem);

        /// <summary>
        /// Specifies that the collection is to be checked for duplicates using the item's <see cref="IEntityKey.EntityKey"/> value.
        /// </summary>
        /// <param name="duplicateText">The duplicate text <see cref="LText"/> to be passed for the error message; defaults to <see cref="ValidatorStrings.Identifier"/> or <see cref="ValidatorStrings.PrimaryKey"/> depending on whether <see cref="IIdentifier"/> is implemented.</param>
        /// <returns>The <see cref="CollectionRuleItem{TItemEntity}"/> instance to support chaining/fluent.</returns>
        public CollectionRuleItem<TItem> DuplicateCheck(LText? duplicateText = null)
        {
            if (_duplicateCheck)
                throw new InvalidOperationException($"A {nameof(DuplicateCheck)} can only be specified once.");

            if (ItemType.GetInterface(typeof(IEntityKey).Name) == null)
                throw new InvalidOperationException($"A CollectionRuleItem ItemType '{ItemType.Name}' must implement '{nameof(IEntityKey)}' to support default expression-less {nameof(DuplicateCheck)}.");

            _duplicateText = string.IsNullOrEmpty(duplicateText) ? (ItemType.GetInterface(typeof(IIdentifier).Name) is not null ? ValidatorStrings.Identifier : ValidatorStrings.PrimaryKey) : duplicateText;
            _duplicateCheck = true;

            return this;
        }

        /// <summary>
        /// Specifies that the collection is to be checked for duplicates using the item's <see cref="IEntityKey.EntityKey"/> value with an option to <paramref name="ignoreWhereKeyIsInitial"/>.
        /// </summary>
        /// <param name="ignoreWhereKeyIsInitial">Indicates whether to ignore the <see cref="IEntityKey.EntityKey"/> when the underlying <see cref="CompositeKey.IsInitial"/>; useful where the identifier will be generated by the underlying data source on create for example.</param>
        /// <param name="duplicateText">The duplicate text <see cref="LText"/> to be passed for the error message; defaults to <see cref="ValidatorStrings.Identifier"/>.</param>
        /// <returns>The <see cref="CollectionRuleItem{TItemEntity}"/> instance to support chaining/fluent.</returns>
        public CollectionRuleItem<TItem> DuplicateCheck(bool ignoreWhereKeyIsInitial, LText? duplicateText = null)
        {
            DuplicateCheck(duplicateText);
            _ignoreWhereKeyIsInitial = ignoreWhereKeyIsInitial;
            return this;
        }

        /// <summary>
        /// Specifies that the collection is to be checked for duplicates using the specified item <paramref name="propertyExpression"/>.
        /// </summary>
        /// <typeparam name="TItemProperty">The item property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the item property that is being duplicate checked.</param>
        /// <param name="duplicateText">The duplicate text <see cref="LText"/> to be passed for the error message (default is to derive the text from the property itself where possible).</param>
        /// <returns>The <see cref="CollectionRuleItem{TItemEntity}"/> instance to support chaining/fluent.</returns>
        public CollectionRuleItem<TItem> DuplicateCheck<TItemProperty>(Expression<Func<TItem, TItemProperty>> propertyExpression, LText? duplicateText = null)
        {
            if (_duplicateCheck)
                throw new InvalidOperationException($"A {nameof(DuplicateCheck)} can only be specified once.");

            _propertyExpression = PropertyExpression.Create(propertyExpression);
            _duplicateText = duplicateText ?? _propertyExpression.Text;
            _duplicateCheck = true;

            return this;
        }

        /// <summary>
        /// Performs the duplicate validation check.
        /// </summary>
        /// <param name="context">The <see cref="IPropertyContext"/>.</param>
        /// <param name="items">The items to duplicate check.</param>
        void ICollectionRuleItem.DuplicateValidation(IPropertyContext context, IEnumerable? items) => DuplicateValidation(context, (IEnumerable<TItem>?)items);

        /// <summary>
        /// Performs the duplicate validation check.
        /// </summary>
        /// <param name="context">The <see cref="IPropertyContext"/>.</param>
        /// <param name="items">The items to duplicate check.</param>
        private void DuplicateValidation(IPropertyContext context, IEnumerable<TItem>? items)
        {
            if (!_duplicateCheck || items == null)
                return;

            if (_propertyExpression != null)
            {
                var dict = new Dictionary<object?, object?>();
                foreach (var item in items.Where(x => x != null))
                {
                    var val = _propertyExpression.GetValue(item!);
                    if (!dict.TryAdd(val, item))
                        context.CreateErrorMessage(ValidatorStrings.DuplicateValueFormat, _duplicateText!, val!);
                }
            }
            else
            {
                var dict = new Dictionary<object?, object?>();
                foreach (var item in items.Where(x => x != null).Cast<IEntityKey>())
                {
                    if (_ignoreWhereKeyIsInitial && item.EntityKey.IsInitial)
                        continue;

                    if (!dict.TryAdd(item.EntityKey, item))
                        context.CreateErrorMessage(item.EntityKey.Args.Length == 1 ? ValidatorStrings.DuplicateValueFormat : ValidatorStrings.DuplicateValue2Format, _duplicateText!, item.EntityKey.ToString());
                }
            }
        }
    }
}