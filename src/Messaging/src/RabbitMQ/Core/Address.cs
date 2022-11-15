// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Text.RegularExpressions;

namespace Steeltoe.Messaging.RabbitMQ.Core;

public class Address
{
    private const string AddressPattern = "^(?:.*://)?([^/]*)/?(.*)$";
    public const string AmqRabbitMQReplyTo = "amq.rabbitmq.reply-to";

    public string ExchangeName { get; }

    public string RoutingKey { get; }

    public Address(string exchangeName, string routingKey)
    {
        ExchangeName = exchangeName;
        RoutingKey = routingKey;
    }

    public Address(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            ExchangeName = string.Empty;
            RoutingKey = string.Empty;
        }
        else if (address.StartsWith(AmqRabbitMQReplyTo, StringComparison.Ordinal))
        {
            RoutingKey = address;
            ExchangeName = string.Empty;
        }
        else if (address.LastIndexOf('/') <= 0)
        {
            // RoutingKey = address.ReplaceFirst("/", string.Empty);
            var regex = new Regex("/");
            RoutingKey = regex.Replace(address, string.Empty, 1);
            ExchangeName = string.Empty;
        }
        else
        {
            Match matchFound = Regex.Match(address, AddressPattern);

            if (matchFound.Success)
            {
                GroupCollection groups = matchFound.Groups;
                ExchangeName = groups[1].Value;
                RoutingKey = groups[2].Value;
            }
            else
            {
                ExchangeName = string.Empty;
                RoutingKey = address;
            }
        }
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not Address other || GetType() != obj.GetType())
        {
            return false;
        }

        return ExchangeName == other.ExchangeName && RoutingKey == other.RoutingKey;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ExchangeName, RoutingKey);
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder(ExchangeName).Append('/');

        if (!string.IsNullOrEmpty(RoutingKey))
        {
            sb.Append(RoutingKey);
        }

        return sb.ToString();
    }
}
