// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Integration.Support.Test;

public class MutableMessageTest
{
    [Fact]
    public void TestMessageIdTimestampRemains()
    {
        var uuid = Guid.NewGuid();
        var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        var payload = new object();
        var headerDictionary = new Dictionary<string, object>
        {
            { MessageHeaders.ID, uuid },
            { MessageHeaders.TIMESTAMP, timestamp }
        };

        var mutableMessage = new MutableMessage<object>(payload, headerDictionary);
        var headers = mutableMessage.Headers as MutableMessageHeaders;

        Assert.Equal(uuid.ToString(), headers.RawHeaders[MessageHeaders.ID]);
        Assert.Equal(timestamp, headers.RawHeaders[MessageHeaders.TIMESTAMP]);
    }

    [Fact]
    public void TestMessageHeaderIsSettable()
    {
        var payload = new object();
        var headerDictionary = new Dictionary<string, object>();
        var additional = new Dictionary<string, object>();

        var mutableMessage = new MutableMessage<object>(payload, headerDictionary);
        var headers = mutableMessage.Headers as MutableMessageHeaders;

        // Should not throw an UnsupportedOperationException
        headers.Add("foo", "bar");
        headers.Add("eep", "bar");
        headers.Remove("eep");
        headers.AddRange(additional);

        Assert.Equal("bar", headers.RawHeaders["foo"]);
    }

    [Fact]
    public void TestMessageHeaderIsSerializable()
    {
        var payload = new object();

        var uuid = Guid.NewGuid();
        var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        // UUID as string; timestamp as string
        var headerDictionarystrings = new Dictionary<string, object>
        {
            { MessageHeaders.ID, uuid.ToString() },
            { MessageHeaders.TIMESTAMP, timestamp.ToString() }
        };
        var mutableMessagestrings = new MutableMessage<object>(payload, headerDictionarystrings);
        Assert.Equal(uuid.ToString(), mutableMessagestrings.Headers.Id);
        Assert.Equal(timestamp, mutableMessagestrings.Headers.Timestamp);

        // UUID as byte[]; timestamp as Long
        var headerDictionaryByte = new Dictionary<string, object>();
        var uuidAsBytes = uuid.ToByteArray();

        headerDictionaryByte.Add(MessageHeaders.ID, uuidAsBytes);
        headerDictionaryByte.Add(MessageHeaders.TIMESTAMP, timestamp);
        var mutableMessageBytes = new MutableMessage<object>(payload, headerDictionaryByte);
        Assert.Equal(uuid.ToString(), mutableMessageBytes.Headers.Id);
        Assert.Equal(timestamp, mutableMessageBytes.Headers.Timestamp);
    }
}
