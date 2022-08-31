// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Stream.Configuration;

public class ProducerOptions : IProducerOptions
{
    private const bool AutoStartupDefault = true;
    private const int PartitionCountDefault = 1;
    private const bool UseNativeEncodingDefault = false;
    private const bool IsErrorChannelEnabledDefault = false;

    bool IProducerOptions.AutoStartup => AutoStartup.Value;

    HeaderMode IProducerOptions.HeaderMode => HeaderMode.Value;

    bool IProducerOptions.UseNativeEncoding => UseNativeEncoding.Value;

    bool IProducerOptions.ErrorChannelEnabled => ErrorChannelEnabled.Value;

    public string BindingName { get; set; }

    public bool? AutoStartup { get; set; }

    public string PartitionKeyExpression { get; set; }

    public string PartitionKeyExtractorName { get; set; }

    public string PartitionSelectorName { get; set; }

    public string PartitionSelectorExpression { get; set; }

    public int PartitionCount { get; set; } = int.MinValue;

    public List<string> RequiredGroups { get; set; }

    public HeaderMode? HeaderMode { get; set; }

    public bool? UseNativeEncoding { get; set; }

    public bool? ErrorChannelEnabled { get; set; }

    public bool IsPartitioned => PartitionKeyExpression != null || PartitionCount > 1 || PartitionKeyExtractorName != null;

    public ProducerOptions()
    {
    }

    public ProducerOptions(string bindingName)
    {
        ArgumentGuard.NotNull(bindingName);

        BindingName = bindingName;
    }

    public IProducerOptions Clone()
    {
        var clone = (ProducerOptions)MemberwiseClone();
        clone.RequiredGroups = new List<string>(RequiredGroups);
        return clone;
    }

    internal void PostProcess(string name, ProducerOptions @default = null)
    {
        BindingName = name;
        ErrorChannelEnabled ??= @default != null ? @default.ErrorChannelEnabled : IsErrorChannelEnabledDefault;
        UseNativeEncoding ??= @default != null ? @default.UseNativeEncoding : UseNativeEncodingDefault;
        HeaderMode ??= @default != null ? @default.HeaderMode : Configuration.HeaderMode.None;
        RequiredGroups ??= @default != null ? @default.RequiredGroups : new List<string>();

        if (PartitionCount == int.MinValue)
        {
            PartitionCount = @default?.PartitionCount ?? PartitionCountDefault;
        }

        PartitionSelectorExpression ??= @default?.PartitionSelectorExpression;
        PartitionSelectorName ??= @default?.PartitionSelectorName;
        PartitionKeyExtractorName ??= @default?.PartitionKeyExtractorName;
        PartitionKeyExpression ??= @default?.PartitionKeyExpression;
        AutoStartup ??= @default != null ? @default.AutoStartup : AutoStartupDefault;
    }
}
