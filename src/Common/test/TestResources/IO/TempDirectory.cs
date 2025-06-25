// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.TestResources.IO;

/// <summary>
/// A temporary directory, which is created on construction and deleted when disposed.
/// </summary>
public class TempDirectory : TempPath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TempDirectory" /> class.
    /// </summary>
    public TempDirectory()
        : base(string.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TempDirectory" /> class.
    /// </summary>
    /// <param name="prefix">
    /// Directory name prefix.
    /// </param>
    public TempDirectory(string prefix)
        : base(prefix)
    {
    }

    /// <summary>
    /// Creates the temporary directory.
    /// </summary>
    protected override void Create()
    {
        _ = Directory.CreateDirectory(FullPath);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!Directory.Exists(FullPath))
            {
                return;
            }

            try
            {
                Directory.Delete(FullPath, true);
            }
            catch
            {
                // Intentionally left empty.
            }
        }
    }
}
