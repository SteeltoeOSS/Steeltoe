// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration;

namespace Steeltoe.Connector.Services;

public class HystrixRabbitMQServiceInfoFactory : ServiceInfoFactory
{
    private static readonly string[] Scheme =
    {
        RabbitMQServiceInfo.AmqpScheme,
        RabbitMQServiceInfo.AmqpSecureScheme
    };

    public static readonly Tags HystrixRabbitServiceTags = new("hystrix-amqp");

    public HystrixRabbitMQServiceInfoFactory()
        : base(HystrixRabbitServiceTags, Scheme)
    {
    }

    public override bool Accepts(Service binding)
    {
        return TagsMatch(binding) && UriCredentialsMatchesScheme(binding.Credentials);
    }

    public override IServiceInfo Create(Service binding)
    {
        Credential amqpCredentials = binding.Credentials["amqp"];
        string uri = GetUriFromCredentials(amqpCredentials);
        bool sslEnabled = GetBoolFromCredentials(amqpCredentials, "ssl");

        if (amqpCredentials.ContainsKey("uris"))
        {
            List<string> uris = GetListFromCredentials(amqpCredentials, "uris");
            return new HystrixRabbitMQServiceInfo(binding.Name, uri, uris, sslEnabled);
        }

        return new HystrixRabbitMQServiceInfo(binding.Name, uri, sslEnabled);
    }

    private bool UriCredentialsMatchesScheme(Dictionary<string, Credential> credentials)
    {
        if (credentials.ContainsKey("amqp"))
        {
            Credential amqpDict = credentials["amqp"];
            string uri = GetStringFromCredentials(amqpDict, UriKeys);

            if (uri != null)
            {
                foreach (string uriScheme in UriSchemes)
                {
                    if (uri.StartsWith($"{uriScheme}://"))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
