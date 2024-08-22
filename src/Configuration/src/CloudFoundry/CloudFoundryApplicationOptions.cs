// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Configuration.CloudFoundry;

public sealed class CloudFoundryApplicationOptions : IApplicationInstanceInfo
{
    // See https://docs.cloudfoundry.org/devguide/deploy-apps/environment-variable.html#VCAP-APPLICATION

    internal const string ConfigurationPrefix = "vcap:application";
    internal const string DefaultApplicationName = "unknown";

    /// <summary>
    /// Gets or sets the GUID identifying the app.
    /// </summary>
    [ConfigurationKeyName("application_id")]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the name assigned to the app when it was pushed.
    /// </summary>
    [ConfigurationKeyName("application_name")]
    public string ApplicationName { get; set; } = DefaultApplicationName;

    /// <summary>
    /// Gets the URIs assigned to the app.
    /// </summary>
    [ConfigurationKeyName("application_uris")]
    public IList<string> Uris { get; } = new List<string>();

    /// <summary>
    /// Gets or sets the GUID identifying a version of the app. Each time an app is pushed or restarted, this value is updated.
    /// </summary>
    [ConfigurationKeyName("application_version")]
    public string? ApplicationVersion { get; set; }

    /// <summary>
    /// Gets or sets the location of the Cloud Controller API for the Cloud Foundry deployment where the app runs.
    /// </summary>
    [ConfigurationKeyName("cf_api")]
    public string? Api { get; set; }

    /// <summary>
    /// Gets or sets the limits to disk space, number of files, and memory permitted to the app. Memory and disk space limits are supplied when the app is
    /// deployed, either on the command line or in the app manifest. The number of files allowed is operator-defined.
    /// </summary>
    public ApplicationLimits? Limits { get; set; }

    /// <summary>
    /// Gets or sets the GUID identifying the org where the app is deployed.
    /// </summary>
    [ConfigurationKeyName("organization_id")]
    public string? OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets the human-readable name of the org where the app is deployed.
    /// </summary>
    [ConfigurationKeyName("organization_name")]
    public string? OrganizationName { get; set; }

    /// <summary>
    /// Gets or sets the UID identifying the process. Only present in running application containers.
    /// </summary>
    [ConfigurationKeyName("process_id")]
    public string? ProcessId { get; set; }

    /// <summary>
    /// Gets or sets the type of process. Only present in running application containers.
    /// </summary>
    [ConfigurationKeyName("process_type")]
    public string? ProcessType { get; set; }

    /// <summary>
    /// Gets or sets the GUID identifying the space where the app is deployed.
    /// </summary>
    [ConfigurationKeyName("space_id")]
    public string? SpaceId { get; set; }

    /// <summary>
    /// Gets or sets the human-readable name of the space where the app is deployed.
    /// </summary>
    [ConfigurationKeyName("space_name")]
    public string? SpaceName { get; set; }

    /// <summary>
    /// Gets or sets the human-readable timestamp for the time the instance was started. Not provided on Diego Cells.
    /// </summary>
    [ConfigurationKeyName("start")]
    public string? Start { get; set; }

    /// <summary>
    /// Gets or sets the UNIX epoch timestamp for the time the instance was started. Not provided on Diego Cells.
    /// </summary>
    [ConfigurationKeyName("started_at_timestamp")]
    public long StartedAtTimestamp { get; set; }

    // The properties below don't actually exist in VCAP_APPLICATION, but are sourced from other environment variables.

    /// <summary>
    /// Gets or sets the UUID of the app instance. Originates from the CF_INSTANCE_GUID environment variable.
    /// </summary>
    [ConfigurationKeyName("instance_id")]
    public string? InstanceId { get; set; }

    /// <summary>
    /// Gets or sets the zero-based index number of the app instance. Originates from the CF_INSTANCE_INDEX environment variable.
    /// </summary>
    [ConfigurationKeyName("instance_index")]
    public int InstanceIndex { get; set; } = -1;

    /// <summary>
    /// Gets or sets the external (host-side) port corresponding to the internal (container-side) port. Originates from the CF_INSTANCE_PORT environment
    /// variable.
    /// </summary>
    [ConfigurationKeyName("port")]
    public int InstancePort { get; set; } = -1;

    /// <summary>
    /// Gets or sets the external IP address of the host running the app instance. Originates from the CF_INSTANCE_IP environment variable.
    /// </summary>
    [ConfigurationKeyName("instance_ip")]
    public string? InstanceIP { get; set; }

    /// <summary>
    /// Gets or sets the internal IP address of the container running the app instance. Originates from the CF_INSTANCE_INTERNAL_IP environment variable.
    /// </summary>
    [ConfigurationKeyName("internal_ip")]
    public string? InternalIP { get; set; }
}
