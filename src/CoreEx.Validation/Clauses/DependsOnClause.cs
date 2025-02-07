﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using System;
using System.Linq.Expressions;

namespace CoreEx.Validation.Clauses
{
    /// <summary>
    /// Represents a depends on test clause; in that another specified property of the entity must have a non-default value (and not have a validation error) to continue.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public class DependsOnClause<TEntity, TProperty> : IPropertyRuleClause<TEntity, TProperty> where TEntity : class
    {
        private readonly PropertyExpression<TEntity, TProperty> _dependsOn;

        /// <summary>
        /// Initializes a new instance of the <see cref="DependsOnClause{TEntity, TProperty}"/> class.
        /// </summary>
        /// <param name="dependsOnExpression">The <see cref="Expression"/> to reference the depends on entity property.</param>
        public DependsOnClause(Expression<Func<TEntity, TProperty>> dependsOnExpression)
            => _dependsOn = PropertyExpression.Create(dependsOnExpression ?? throw new ArgumentNullException(nameof(dependsOnExpression)));

        /// <summary>
        /// Checks the clause.
        /// </summary>
        /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
        /// <returns><c>true</c> where validation is to continue; otherwise, <c>false</c> to stop.</returns>
        public bool Check(IPropertyContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Do not continue where the depends on property is in error.
            if (context.Parent.HasError(context.CreateFullyQualifiedPropertyName(_dependsOn.Name)))
                return false;

            // Check depends on value to continue.
            object? value = _dependsOn.GetValue((TEntity)context.Parent.Value!);
            return !(System.Collections.Comparer.Default.Compare(value, default(TProperty)!) == 0);
        }
    }
}