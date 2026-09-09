// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using NSubstitute;
using StackExchange.Redis;

namespace Steeltoe.Security.DataProtection.Redis.Test;

partial class RedisDataProtectionBuilderExtensionsTest
{
    private static object GetMockedConnectionMultiplexer(string? connectionString)
    {
        Dictionary<string, byte[]> innerStore = [];

        var database = Substitute.For<IDatabase>();
        database.HashGet(Arg.Any<RedisKey>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>()).Returns(info => GetRedisValues(info.Arg<RedisKey>()));

        database.HashGetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(info => Task.FromResult(GetRedisValues(info.Arg<RedisKey>())));

        database.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]?>(), Arg.Any<RedisValue[]?>(), Arg.Any<CommandFlags>()).Returns(info =>
        {
            RedisKey[]? keys = info.Arg<RedisKey[]?>();
            RedisValue[]? values = info.Arg<RedisValue[]?>();

            innerStore[keys![0]!] = values![3]!;
            return Task.FromResult(RedisResult.Create(keys[0]));
        });

        var connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        connectionMultiplexer.Configuration.Returns(connectionString);
        connectionMultiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(database);

        database.Multiplexer.Returns(connectionMultiplexer);

        return connectionMultiplexer;

        RedisValue[] GetRedisValues(RedisKey key)
        {
            return innerStore.TryGetValue(key!, out byte[]? data)
                ?
                [
                    default,
                    default,
                    data
                ]
                :
                [
                    default,
                    default,
                    default
                ];
        }
    }
}
