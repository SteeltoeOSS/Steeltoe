// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Stream.Config;

public class ConsumerOptions : IConsumerOptions
{
    private const bool AutoStartupDefault = true;
    private const int ConcurrencyDefault = 1;
    private const bool IsPartitionedDefault = false;
    private const int InstanceCountDefault = -1;
    private const int InstanceIndexDefault = -1;
    private const int MaxAttemptsDefault = 3;
    private const int BackOffInitialIntervalDefault = 1000;
    private const int BackOffMaxIntervalDefault = 10000;
    private const double BackOffMultiplierDefault = 2.0;
    private const bool DefaultRetryableDefault = true;
    private const bool UseNativeDecodingDefault = false;
    private const bool MultiplexDefault = false;

    public ConsumerOptions()
    {
    }

    public ConsumerOptions(string bindingName)
    {
        BindingName = bindingName ?? throw new ArgumentNullException(nameof(bindingName));
    }

    public string BindingName { get; set; }

    public bool? AutoStartup { get; set; }

    public int Concurrency { get; set; } = int.MinValue;

    public bool? Partitioned { get; set; }

    public int InstanceCount { get; set; } = int.MinValue;

    public int InstanceIndex { get; set; } = int.MinValue;

    public List<int> InstanceIndexList { get; set; }

    public int MaxAttempts { get; set; } = int.MinValue;

    public int BackOffInitialInterval { get; set; } = int.MinValue;

    public int BackOffMaxInterval { get; set; } = int.MinValue;

    public double BackOffMultiplier { get; set; } = double.NaN;

    public bool? DefaultRetryable { get; set; }

    public List<string> RetryableExceptions { get; set; }

    public HeaderMode? HeaderMode { get; set; }

    public bool? UseNativeDecoding { get; set; }

    public bool? Multiplex { get; set; }

    bool IConsumerOptions.AutoStartup => AutoStartup.Value;

    bool IConsumerOptions.IsPartitioned => Partitioned.Value;

    bool IConsumerOptions.DefaultRetryable => DefaultRetryable.Value;

    HeaderMode IConsumerOptions.HeaderMode => HeaderMode.Value;

    bool IConsumerOptions.UseNativeDecoding => UseNativeDecoding.GetValueOrDefault();

    bool IConsumerOptions.Multiplex => Multiplex.GetValueOrDefault();

    public IConsumerOptions Clone()
    {
        var clone = (ConsumerOptions)MemberwiseClone();
        clone.RetryableExceptions = new List<string>(RetryableExceptions);
        clone.InstanceIndexList = new List<int>(InstanceIndexList);
        return clone;
    }

    internal void PostProcess(string name, ConsumerOptions @default = null)
    {
        BindingName = name;
        Multiplex ??= @default != null ? @default.Multiplex : MultiplexDefault;
        UseNativeDecoding ??= @default != null ? @default.UseNativeDecoding : UseNativeDecodingDefault;
        HeaderMode ??= @default != null ? @default.HeaderMode : Config.HeaderMode.None;
        RetryableExceptions ??= @default != null ? @default.RetryableExceptions : new List<string>();
        DefaultRetryable ??= @default != null ? @default.DefaultRetryable : DefaultRetryableDefault;

        if (double.IsNaN(BackOffMultiplier))
        {
            BackOffMultiplier = @default?.BackOffMultiplier ?? BackOffMultiplierDefault;
        }

        if (BackOffMaxInterval == int.MinValue)
        {
            BackOffMaxInterval = @default?.BackOffMaxInterval ?? BackOffMaxIntervalDefault;
        }

        if (BackOffInitialInterval == int.MinValue)
        {
            BackOffInitialInterval = @default?.BackOffInitialInterval ?? BackOffInitialIntervalDefault;
        }

        if (MaxAttempts == int.MinValue)
        {
            MaxAttempts = @default?.MaxAttempts ?? MaxAttemptsDefault;
        }

        if (InstanceIndex == int.MinValue)
        {
            InstanceIndex = @default?.InstanceIndex ?? InstanceIndexDefault;
        }

        if (InstanceCount == int.MinValue)
        {
            InstanceCount = @default?.InstanceCount ?? InstanceCountDefault;
        }

        InstanceIndexList ??= @default != null ? @default.InstanceIndexList : new List<int>();
        Partitioned ??= @default != null ? @default.Partitioned : IsPartitionedDefault;

        if (Concurrency == int.MinValue)
        {
            Concurrency = @default?.Concurrency ?? ConcurrencyDefault;
        }

        AutoStartup ??= @default != null ? @default.AutoStartup : AutoStartupDefault;
    }
}
