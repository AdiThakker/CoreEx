﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides mandatory validation; determined as mandatory when it contains its default value.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <remarks>A value will be determined as mandatory when it contains its default value. For example an <see cref="int"/> will trigger when the value is zero; however, a
    /// <see cref="Nullable{Int32}"/> will trigger when null only (a zero is considered a value in this instance).</remarks>
    public class MandatoryRule<TEntity, TProperty> : ValueRuleBase<TEntity, TProperty> where TEntity : class
    {
        /// <inheritdoc/>
        public override Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Compare the value against its default.
            if (Comparer<TProperty?>.Default.Compare(context.Value, default!) == 0)
            {
                CreateErrorMessage(context);
                return Task.CompletedTask;
            }

            // Also check for empty strings.
            if (context.Value is string val && val.Length == 0)
                CreateErrorMessage(context);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Create the error message.
        /// </summary>
        private void CreateErrorMessage(PropertyContext<TEntity, TProperty> context) => context.CreateErrorMessage(ErrorText ?? ValidatorStrings.MandatoryFormat);
    }
}