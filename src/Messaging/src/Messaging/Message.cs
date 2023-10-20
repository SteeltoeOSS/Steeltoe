// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text;
using Steeltoe.Common;
using Steeltoe.Common.Util;

namespace Steeltoe.Messaging;

public static class Message
{
    public static IMessage<T> Create<T>(T payload)
    {
        return (IMessage<T>)Create(payload, typeof(T));
    }

    public static IMessage<T> Create<T>(T payload, IMessageHeaders headers)
    {
        if (headers == null)
        {
            return (IMessage<T>)Create(payload, typeof(T));
        }

        return (IMessage<T>)Create(payload, headers, typeof(T));
    }

    public static IMessage<T> Create<T>(T payload, IDictionary<string, object> headers)
    {
        return (IMessage<T>)Create(payload, new MessageHeaders(headers, null, null), typeof(T));
    }

    public static IMessage Create(object payload, Type messageType = null)
    {
        Type genParamType = GetGenericParamType(payload, messageType);
        Type typeToCreate = typeof(Message<>).MakeGenericType(genParamType);

        return (IMessage)Activator.CreateInstance(typeToCreate, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new[]
        {
            payload
        }, null, null);
    }

    public static IMessage Create(object payload, IMessageHeaders headers, Type messageType = null)
    {
        Type genParamType = GetGenericParamType(payload, messageType);
        Type typeToCreate = typeof(Message<>).MakeGenericType(genParamType);

        return (IMessage)Activator.CreateInstance(typeToCreate, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new[]
        {
            payload,
            headers
        }, null, null);
    }

    public static IMessage Create(object payload, IDictionary<string, object> headers, Type messageType = null)
    {
        Type genParamType = GetGenericParamType(payload, messageType);
        Type typeToCreate = typeof(Message<>).MakeGenericType(genParamType);

        return (IMessage)Activator.CreateInstance(typeToCreate, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new[]
        {
            payload,
            headers
        }, null, null);
    }

    private static Type GetGenericParamType(object payload, Type messageType)
    {
        if (payload == null && messageType == null)
        {
            return typeof(object);
        }

        if (messageType != null)
        {
            return messageType;
        }

        return payload.GetType();
    }
}

public class Message<TPayload> : IMessage<TPayload>
{
    protected readonly TPayload InnerPayload;

    protected readonly IMessageHeaders InnerHeaders;

    object IMessage.Payload => Payload;

    public TPayload Payload => InnerPayload;

    public IMessageHeaders Headers => InnerHeaders;

    protected internal Message(TPayload payload)
        : this(payload, new MessageHeaders())
    {
    }

    protected internal Message(TPayload payload, IDictionary<string, object> headers)
        : this(payload, new MessageHeaders(headers, null, null))
    {
    }

    protected internal Message(TPayload payload, IMessageHeaders headers)
    {
        ArgumentGuard.NotNull(payload);
        ArgumentGuard.NotNull(headers);

        InnerPayload = payload;
        InnerHeaders = headers;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not Message<TPayload> other)
        {
            return false;
        }

        return ObjectEquality.ObjectOrCollectionEquals(InnerPayload, other.InnerPayload) && InnerHeaders.Equals(other.InnerHeaders);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ObjectEquality.GetObjectOrCollectionHashCode(InnerPayload), InnerHeaders.GetHashCode());
    }

    public override string ToString()
    {
        var sb = new StringBuilder(GetType().Name);
        sb.Append(" [payload=");

        if (InnerPayload is byte[])
        {
            byte[] arr = (byte[])(object)InnerPayload;
            sb.Append("byte[").Append(arr.Length).Append(']');
        }
        else
        {
            sb.Append(InnerPayload);
        }

        sb.Append(", headers=").Append(InnerHeaders).Append(']');
        return sb.ToString();
    }
}
