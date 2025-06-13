// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using StackExchange.Redis;

namespace Steeltoe.Security.DataProtection.Redis.Test;

partial class RedisDataProtectionBuilderExtensionsTest
{
    private static object GetMockedConnectionMultiplexer(string? connectionString)
    {
        Dictionary<string, byte[]> innerStore = [];
        var databaseMock = new Mock<IDatabase>();

        databaseMock.Setup(database => database.HashGet(It.IsAny<RedisKey>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Returns((RedisKey key, RedisValue[] _, CommandFlags _) => GetRedisValues(key));

        databaseMock.Setup(database => database.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Returns((RedisKey key, RedisValue[] _, CommandFlags _) => Task.FromResult(GetRedisValues(key)));

        databaseMock.Setup(database => database.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), It.IsAny<CommandFlags>()))
            .Returns((RedisKey key, HashEntry[] hashFields, CommandFlags _) =>
            {
                byte[] data = hashFields[2].Value!;
                innerStore[key!] = data;
                return Task.CompletedTask;
            });

        var connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        connectionMultiplexerMock.Setup(connectionMultiplexer => connectionMultiplexer.Configuration).Returns(connectionString!);

        connectionMultiplexerMock.Setup(connectionMultiplexer => connectionMultiplexer.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
            .Returns(databaseMock.Object);

        databaseMock.Setup(database => database.Multiplexer).Returns(connectionMultiplexerMock.Object);

        return connectionMultiplexerMock.Object;

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
