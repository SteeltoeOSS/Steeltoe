// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using System.Text;

namespace Steeltoe.Integration.Support;

public class MutableMessage : IMessage
{
    protected readonly object InnerPayload;

    protected readonly MutableMessageHeaders InnerHeaders;

    public MutableMessage(object payload)
        : this(payload, (Dictionary<string, object>)null)
    {
    }

    public MutableMessage(object payload, IDictionary<string, object> headers)
        : this(payload, new MutableMessageHeaders(headers))
    {
    }

    public MutableMessage(object payload, MutableMessageHeaders headers)
    {
        this.InnerPayload = payload ?? throw new ArgumentNullException(nameof(payload));
        this.InnerHeaders = headers ?? throw new ArgumentNullException(nameof(headers));
    }

    public IMessageHeaders Headers => InnerHeaders;

    public object Payload => InnerPayload;

    public override string ToString()
    {
        var sb = new StringBuilder(GetType().Name);
        sb.Append(" [payload=");
        if (InnerPayload is byte[] v)
        {
            sb.Append("byte[").Append(v.Length).Append(']');
        }
        else
        {
            sb.Append(InnerPayload);
        }

        sb.Append(", headers=").Append(InnerHeaders).Append(']');
        return sb.ToString();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(InnerHeaders, InnerPayload);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not MutableMessage other)
        {
            return false;
        }

        var thisId = InnerHeaders.Id;
        var otherId = other.InnerHeaders.Id;

        return ObjectUtils.NullSafeEquals(thisId, otherId) && InnerHeaders.Equals(other.InnerHeaders) && InnerPayload.Equals(other.InnerPayload);
    }

    protected internal IDictionary<string, object> RawHeaders => InnerHeaders.RawHeaders;
}

public class MutableMessage<T> : MutableMessage, IMessage<T>
{
    public MutableMessage(T payload)
        : this(payload, (Dictionary<string, object>)null)
    {
    }

    public MutableMessage(T payload, IDictionary<string, object> headers)
        : this(payload, new MutableMessageHeaders(headers))
    {
    }

    public MutableMessage(T payload, MutableMessageHeaders headers)
        : base(payload, headers)
    {
    }

    public new T Payload => (T)InnerPayload;

    public override int GetHashCode() => base.GetHashCode();

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not MutableMessage<T> other)
        {
            return false;
        }

        var thisId = InnerHeaders.Id;
        var otherId = other.InnerHeaders.Id;

        return ObjectUtils.NullSafeEquals(thisId, otherId) && InnerHeaders.Equals(other.InnerHeaders) && InnerPayload.Equals(other.InnerPayload);
    }
}
