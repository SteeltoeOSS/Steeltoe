// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.TestResources.IO;

/// <summary>
/// A temporary directory that can be used as a sandbox. Files and directories created in the sandbox are deleted when the sandbox is disposed.
/// </summary>
/// <remarks>
/// Beware this isn't a true sandbox, because it doesn't guard against IO above its root.
/// </remarks>
public sealed class Sandbox : TempDirectory
{
    private const string DefaultPrefix = "Sandbox-";

    /// <inheritdoc cref="TempDirectory" />
    public Sandbox()
        : base(DefaultPrefix)
    {
    }

    /// <summary>
    /// Returns the physical path for the specified path within the sandbox.
    /// </summary>
    /// <param name="path">
    /// The path to resolve.
    /// </param>
    /// <returns>
    /// The physical path.
    /// </returns>
    public string ResolvePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return Path.Combine(FullPath, path);
    }

    /// <summary>
    /// Creates a directory at the specified path within the sandbox.
    /// </summary>
    /// <param name="path">
    /// The directory path.
    /// </param>
    /// <returns>
    /// The physical path of the created directory.
    /// </returns>
    public string CreateDirectory(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        DirectoryInfo directoryInfo = Directory.CreateDirectory(ResolvePath(path));
        return directoryInfo.FullName;
    }

    /// <summary>
    /// Creates a file at the specified path within the sandbox.
    /// </summary>
    /// <param name="path">
    /// The file path.
    /// </param>
    /// <param name="content">
    /// The text to write to the file.
    /// </param>
    /// <returns>
    /// The physical path of the created file.
    /// </returns>
    public string CreateFile(string path, string content)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(content);

        string absolutePath = ResolvePath(path);
        string? parentDirectory = Directory.GetParent(absolutePath)?.FullName;

        if (parentDirectory != null)
        {
            Directory.CreateDirectory(parentDirectory);
        }

        File.WriteAllText(absolutePath, content);
        return absolutePath;
    }
}
