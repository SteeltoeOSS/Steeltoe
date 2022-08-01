// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Utils.IO;

/// <summary>
/// A temporary directory that can be used as a sandbox.
/// Files and directories created in the sandbox are deleted when the sandbox is disposed.
/// </summary>
public class Sandbox : TempDirectory
{
    /// <summary>
    /// The default sandbox prefix ("Sandbox-").
    /// </summary>
    public const string DefaultPrefix = "Sandbox-";

    /// <inheritdoc cref="TempDirectory"/>
    /// <param name="name">Sandbox prefix, defaults to <see cref="DefaultPrefix"/>.</param>
    public Sandbox(string name = DefaultPrefix)
        : base(name)
    {
    }

    /// <summary>
    /// Resolves the specified path with the sandbox's path.
    /// </summary>
    /// <param name="path">Path to resolve.</param>
    /// <returns>Resolved path.</returns>
    public string ResolvePath(string path)
    {
        return Path.Combine(FullPath, path);
    }

    /// <summary>
    /// Creates a sandbox directory at the specified path.
    /// </summary>
    /// <param name="path">Directory path.</param>
    /// <returns>Full path name of created directory.</returns>
    public string CreateDirectory(string path)
    {
        var dirInfo = Directory.CreateDirectory(ResolvePath(path));
        return dirInfo.FullName;
    }

    /// <summary>
    /// Creates a sandbox file at the specified path.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="text">Text to write to file.</param>
    /// <returns>Full path name of created file.</returns>
    public string CreateFile(string path, string text = "")
    {
        var fullPath = ResolvePath(path);
        var parentDir = Directory.GetParent(fullPath)?.FullName;
        Directory.CreateDirectory(parentDir);
        File.WriteAllText(fullPath, text);
        return fullPath;
    }
}
