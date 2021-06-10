// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Steeltoe.Common.IO
{
    /// <summary>
    /// A temporary directory.
    /// </summary>
    public class TempDirectory : TempPath
    {
        /// <inheritdoc/>
        public TempDirectory()
        {
        }

        /// <inheritdoc/>
        public TempDirectory(string name) : base(name)
        {
        }

        /// <summary>
        /// Creates the temporary directory.
        /// </summary>
        protected override void InitializePath()
        {
            Directory.CreateDirectory(FullPath);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!(Directory.Exists(FullPath)))
            {
                return;
            }

            try
            {
                Directory.Delete(FullPath, true);
            }
            catch
            {
                // ignore
            }
        }
    }
}