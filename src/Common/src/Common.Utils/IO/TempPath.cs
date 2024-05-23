// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Utils.IO;

/// <summary>
/// An abstraction of a temporary path, such as a file.
/// </summary>
public abstract class TempPath : IDisposable
{
    /// <summary>
    /// Gets the absolute path of the TempPath.
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    /// Gets the name of the TempPath.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TempPath" /> class.
    /// </summary>
    /// <param name="prefix">
    /// Temporary path prefix.
    /// </param>
    protected TempPath(string prefix = null)
    {
        Name = $"{prefix ?? string.Empty}{Guid.NewGuid()}";
        FullPath = Path.Combine(Path.GetTempPath(), Name);
        Initialize();
    }

    /// <summary>
    /// Ensures the temporary path is deleted.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Ensures the temporary path is deleted.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
    /// </param>
    protected abstract void Dispose(bool disposing);

    /// <summary>
    /// Subclasses should override and perform any path initialization here.
    /// </summary>
    protected virtual void InitializePath()
    {
    }

    private void Initialize()
    {
        InitializePath();
    }
}
