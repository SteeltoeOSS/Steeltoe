// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.TestResources.IO;

/// <summary>
/// The base type for a temporary path, such as a file or directory.
/// </summary>
public abstract class TempPath : IDisposable
{
    /// <summary>
    /// Gets the absolute path.
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TempPath" /> class.
    /// </summary>
    /// <param name="prefix">
    /// Name prefix.
    /// </param>
    protected TempPath(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        Name = $"{prefix}{Guid.NewGuid()}";
        FullPath = Path.Combine(Path.GetTempPath(), Name);

        // ReSharper disable once VirtualMemberCallInConstructor
        Create();
    }

    /// <summary>
    /// Creates the temporary path.
    /// </summary>
    protected abstract void Create();

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
}
