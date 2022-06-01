// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Text.RegularExpressions;

namespace Steeltoe.Messaging.RabbitMQ.Core;

public class Address
{
    public const string AMQ_RABBITMQ_REPLY_TO = "amq.rabbitmq.reply-to";

    private const string ADDRESS_PATTERN = "^(?:.*://)?([^/]*)/?(.*)$";

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
        else if (address.StartsWith(AMQ_RABBITMQ_REPLY_TO))
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
            var matchFound = Regex.Match(address, ADDRESS_PATTERN);
            if (matchFound.Success)
            {
                var groups = matchFound.Groups;
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

    public string ExchangeName { get; }

    public string RoutingKey { get; }

    public override bool Equals(object obj)
    {
        if (this == obj)
        {
            return true;
        }

        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        var address = (Address)obj;

        return !(!ExchangeName?.Equals(ExchangeName) ?? address.ExchangeName != null) &&
               !(!RoutingKey?.Equals(address.RoutingKey) ?? address.RoutingKey != null);
    }

    public override int GetHashCode()
    {
        var result = ExchangeName != null ? ExchangeName.GetHashCode() : 0;
        var prime = 31; // NOSONAR magic #
        result = (prime * result) + (RoutingKey != null ? RoutingKey.GetHashCode() : 0);
        return result;
    }

    public override string ToString()
    {
        var sb = new StringBuilder(ExchangeName).Append('/');
        if (!string.IsNullOrEmpty(RoutingKey))
        {
            sb.Append(RoutingKey);
        }

        return sb.ToString();
    }
}
