// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Connectors.RuntimeTypeAccess;

namespace Steeltoe.Connectors.Redis.RuntimeTypeAccess;

internal sealed class RedisCacheShim : Shim
{
    public RedisCacheShim(MicrosoftRedisPackageResolver packageResolver, object instance)
        : this(new InstanceAccessor(packageResolver.RedisCacheClass, instance))
    {
    }

    private RedisCacheShim(InstanceAccessor instanceAccessor)
        : base(instanceAccessor)
    {
    }

    public static RedisCacheShim CreateInstance(MicrosoftRedisPackageResolver packageResolver, RedisCacheOptionsShim redisCacheOptions)
    {
        ArgumentGuard.NotNull(packageResolver);
        ArgumentGuard.NotNull(redisCacheOptions);

        Type optionsWrapperType = typeof(OptionsWrapper<>).MakeGenericType(redisCacheOptions.DeclaredType);
        object optionsWrapperInstance = Activator.CreateInstance(optionsWrapperType, redisCacheOptions.Instance)!;

        InstanceAccessor instanceAccessor = packageResolver.RedisCacheClass.CreateInstance(optionsWrapperInstance);
        return new RedisCacheShim(instanceAccessor);
    }
}
