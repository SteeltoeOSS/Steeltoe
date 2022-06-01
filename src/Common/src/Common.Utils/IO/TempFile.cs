// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Steeltoe.Common.Utils.IO;

/// <summary>
/// A temporary directory.
/// </summary>
public class TempFile : TempPath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TempFile"/> class.
    /// </summary>
    /// <param name="prefix">Temporary file prefix.</param>
    public TempFile(string prefix = null)
        : base(prefix)
    {
    }

    /// <summary>
    /// Creates the temporary file.
    /// </summary>
    protected override void InitializePath()
    {
        File.Create(FullPath).Dispose();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (!File.Exists(FullPath))
        {
            return;
        }

        try
        {
            File.Delete(FullPath);
        }
        catch
        {
            // ignore
        }
    }
}
