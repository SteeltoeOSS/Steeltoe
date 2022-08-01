// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common;

public interface IApplicationInstanceInfo
{
    string InstanceId { get; }

    /// <summary>
    /// Gets a GUID identifying the application.
    /// </summary>
    string ApplicationId { get; }

    /// <summary>
    /// Gets the name of the application, as known to the hosting platform.
    /// </summary>
    string ApplicationName { get; }

    /// <summary>
    /// Gets the URIs assigned to the app.
    /// </summary>
    IEnumerable<string> Uris { get; }

    /// <summary>
    /// Gets the index number of the app instance.
    /// </summary>
    int InstanceIndex { get; }

    /// <summary>
    /// Gets the port assigned to the app instance, on which it should listen.
    /// </summary>
    int Port { get; }

    /// <summary>
    /// Gets a GUID identifying the application instance.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the maximum amount of disk available for the app instance.
    /// </summary>
    int DiskLimit { get; }

    /// <summary>
    /// Gets the maximum amount of memory available for the app instance.
    /// </summary>
    int MemoryLimit { get; }

    /// <summary>
    /// Gets the maximum amount of file descriptors available to the app instance.
    /// </summary>
    int FileDescriptorLimit { get; }

    /// <summary>
    /// Gets the internal IP address of the container running the app instance instance.
    /// </summary>
    string InternalIp { get; }

    /// <summary>
    /// Gets the external IP address of the host running the app instance.
    /// </summary>
    string InstanceIp { get; }

    /// <summary>
    /// Gets the name to use to represent the app instance in the context of a given Steeltoe component.
    /// </summary>
    /// <param name="steeltoeComponent">The Steeltoe component requesting the app's name.</param>
    /// <param name="additionalSearchPath">One additional config key to consider, used as top priority in search.</param>
    /// <returns>The name of the application.</returns>
    string ApplicationNameInContext(SteeltoeComponent steeltoeComponent, string additionalSearchPath = null);
}
