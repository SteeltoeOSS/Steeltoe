// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Common;

/// <summary>
/// Provides a workaround for https://github.com/dotnet/runtime/issues/95789. This breaking change was patched in later .NET 8 versions, but users may
/// still be using an old version.
/// </summary>
internal static class ServiceDescriptorExtensions
{
    public static Type? SafeGetImplementationType(this ServiceDescriptor descriptor)
    {
        return descriptor.IsKeyedService ? descriptor.KeyedImplementationType : descriptor.ImplementationType;
    }

    public static object? SafeGetImplementationInstance(this ServiceDescriptor descriptor)
    {
        return descriptor.IsKeyedService ? descriptor.KeyedImplementationInstance : descriptor.ImplementationInstance;
    }
}
