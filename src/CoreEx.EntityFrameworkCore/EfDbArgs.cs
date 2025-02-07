﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.EntityFrameworkCore;

namespace CoreEx.EntityFrameworkCore
{
    /// <summary>
    /// Provides the extended <b>Entity Framework</b> arguments.
    /// </summary>
    public struct EfDbArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EfDbArgs"/> struct.
        /// </summary>
        public EfDbArgs() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EfDbArgs"/> struct.
        /// </summary>
        /// <param name="template">The template <see cref="EfDbArgs"/> to copy from.</param>
        public EfDbArgs(EfDbArgs template)
        {
            SaveChanges = template.SaveChanges;
            Refresh = template.Refresh;
        }

        /// <summary>
        /// Indicates that the underlying <see cref="DbContext"/> <see cref="DbContext.SaveChanges()"/> is to be performed automatically (defauls to <c>true</c>);
        /// </summary>
        public bool SaveChanges { get; set; } = true;

        /// <summary>
        /// Indicates whether the data should be refreshed (reselected where applicable) after a <b>save</b> operation (defaults to <c>true</c>); is dependent on <see cref="SaveChanges"/> being performed.
        /// </summary>
        public bool Refresh { get; set; } = true;
    }
}