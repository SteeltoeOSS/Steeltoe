// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Config;

/// <summary>
/// Common consumer configuration options.
/// </summary>
public interface IConsumerOptions
{
    string BindingName { get; }

    bool AutoStartup { get; }

    int Concurrency { get; }

    bool IsPartitioned { get; }

    int InstanceCount { get; }

    int InstanceIndex { get; }

    List<int> InstanceIndexList { get; }

    int MaxAttempts { get; }

    int BackOffInitialInterval { get; }

    int BackOffMaxInterval { get; }

    double BackOffMultiplier { get; }

    bool DefaultRetryable { get; }

    List<string> RetryableExceptions { get; }

    HeaderMode HeaderMode { get; }

    bool UseNativeDecoding { get; }

    bool Multiplex { get; }

    IConsumerOptions Clone();
}
