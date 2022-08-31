// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Binder;

namespace Steeltoe.Stream.Configuration;

public interface IBinderConfigurations
{
    Dictionary<string, BinderConfiguration> Configurations { get; }

    List<string> FindMatchingConfigurationsIfAny(IBinder binder);
}
