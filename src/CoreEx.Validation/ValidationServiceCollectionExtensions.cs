﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using CoreEx.Validation;
using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extensions.
    /// </summary>
    public static class ValidationServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="IValidatorEx{T}"/> (and as <see cref="IValidator{T}"/>) as scoped services.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddValidator<T, TValidator>(this IServiceCollection services) where TValidator : class, IValidatorEx<T>
            => (services ?? throw new ArgumentNullException(nameof(services)))
                .AddScoped<IValidatorEx<T>, TValidator>()
                .AddScoped<IValidator<T>>(sp => sp.GetRequiredService<IValidatorEx<T>>());

        /// <summary>
        /// Adds the <see cref="IValidatorEx{T}"/> (and as <see cref="IValidator{T}"/>) as scoped services.
        /// </summary>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddValidator<TValidator>(this IServiceCollection services) where TValidator : class, IValidatorEx
            => (services ?? throw new ArgumentNullException(nameof(services))).AddValidatorInternal<TValidator>();

        /// <summary>
        /// Adds the <see cref="IValidatorEx{T}"/> (and as <see cref="IValidator{T}"/>) as scoped services.
        /// </summary>
        private static IServiceCollection AddValidatorInternal<TValidator>(this IServiceCollection services) where TValidator : class, IValidatorEx
            => (services ?? throw new ArgumentNullException(nameof(services))).AddScoped<TValidator>();

        /// <summary>
        /// Adds all the <see cref="IValidatorEx{T}"/>(s) from the specified <typeparamref name="TAssembly"/> as scoped services.
        /// </summary>
        /// <typeparam name="TAssembly">The <see cref="Type"/> to infer the underlying <see cref="Type.Assembly"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="includeInternalTypes">Indicates whether to include internally defined types.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddValidators<TAssembly>(this IServiceCollection services, bool includeInternalTypes = false)
        {
            var av = typeof(ValidationServiceCollectionExtensions).GetMethod(nameof(AddValidatorInternal), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            foreach (var match in from type in includeInternalTypes ? typeof(TAssembly).Assembly.GetTypes() : typeof(TAssembly).Assembly.GetExportedTypes()
                                  where !type.IsAbstract && !type.IsGenericTypeDefinition
                                  let interfaces = type.GetInterfaces()
                                  let genericInterfaces = interfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidatorEx<>))
                                  let @interface = genericInterfaces.FirstOrDefault()
                                  where @interface != null
                                  select new { type })
            {
                av.MakeGenericMethod(match.type).Invoke(null, new object[] { services });
            }

            return services;
        }

        /// <summary>
        /// Adds the <see cref="ValidationTextProvider"/> as the <see cref="ITextProvider"/> as a singleton service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddValidationTextProvider(this IServiceCollection services)
            => (services ?? throw new ArgumentNullException(nameof(services))).AddSingleton<ITextProvider, ValidationTextProvider>();
    }
}