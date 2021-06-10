// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Steeltoe.Common.IO
{
    /// <summary>
    /// An abstraction of a temporary path, such as a file.
    /// </summary>
    public class TempPath : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TempPath"/> class.
        /// </summary>
        public TempPath() : this(Guid.NewGuid().ToString())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TempPath"/> class.
        /// </summary>
        /// <param name="name">Temporary path name.</param>
        public TempPath(string name)
        {
            Name = name;
            FullPath = $"{Path.GetTempPath()}{Path.DirectorySeparatorChar}{Name}";
            Initialize();
        }
        
        ~TempPath()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets the absolute path of the TempPath.
        /// </summary>
        public string FullPath { get; }

        /// <summary>
        /// Gets the name of the TempPath.
        /// </summary>
        public string Name { get; }

        protected void Initialize()
        {
            InitializePath();
        }

        protected virtual void InitializePath()
        {
        }

        /// <summary>
        /// Ensures the temporary path is deleted.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Ensures the temporary path is deleted.
        /// </summary>
        /// <param name="disposing">If disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}