// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Stream.Config;

/// <summary>
/// Common producer configuration options
/// </summary>
[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface IProducerOptions
{
    string BindingName { get; }

    bool AutoStartup { get; }

    string PartitionKeyExpression { get; }

    string PartitionKeyExtractorName { get; }

    string PartitionSelectorName { get; }

    string PartitionSelectorExpression { get; }

    int PartitionCount { get; }

    List<string> RequiredGroups { get; }

    HeaderMode HeaderMode { get; }

    bool UseNativeEncoding { get; }

    bool ErrorChannelEnabled { get; }

    bool IsPartitioned { get; }

    IProducerOptions Clone();
}