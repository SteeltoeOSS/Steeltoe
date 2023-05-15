// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connectors.Redis;

internal sealed class RedisConnectionStringPostProcessor : ConnectionStringPostProcessor
{
    protected override string BindingType => "redis";

    protected override IConnectionStringBuilder CreateConnectionStringBuilder()
    {
        return new RedisConnectionStringBuilder();
    }
}
