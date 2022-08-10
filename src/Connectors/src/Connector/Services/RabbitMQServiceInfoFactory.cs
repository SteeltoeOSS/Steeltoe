// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration;

namespace Steeltoe.Connector.Services;

public class RabbitMQServiceInfoFactory : ServiceInfoFactory
{
    private static readonly string[] Scheme =
    {
        RabbitMQServiceInfo.AmqpScheme,
        RabbitMQServiceInfo.AmqpSecureScheme
    };

    public static readonly Tags RabbitServiceTags = new("rabbit");

    public RabbitMQServiceInfoFactory()
        : base(RabbitServiceTags, Scheme)
    {
    }

    public override bool Accepts(Service binding)
    {
        bool result = base.Accepts(binding);

        if (result)
        {
            result = !HystrixRabbitMQServiceInfoFactory.HystrixRabbitServiceTags.ContainsOne(binding.Tags);
        }

        return result;
    }

    public override IServiceInfo Create(Service binding)
    {
        string uri = GetUriFromCredentials(binding.Credentials);
        string managementUri = GetStringFromCredentials(binding.Credentials, "http_api_uri");

        if (binding.Credentials.ContainsKey("uris"))
        {
            List<string> uris = GetListFromCredentials(binding.Credentials, "uris");
            List<string> managementUris = GetListFromCredentials(binding.Credentials, "http_api_uris");
            return new RabbitMQServiceInfo(binding.Name, uri, managementUri, uris, managementUris);
        }

        return new RabbitMQServiceInfo(binding.Name, uri, managementUri);
    }
}
