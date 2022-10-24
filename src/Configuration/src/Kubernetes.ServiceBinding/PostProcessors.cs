// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding;

internal sealed class ArtemisPostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "artemis";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.artemis.... and spring.rabbitmq.pool....
                // Steeltoe -> steeltoe:artemis:binding-name:....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("host", "host");
                mapper.MapFromTo("mode", "mode");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("port", "port");
                mapper.MapFromTo("user", "user");

                mapper.MapFromTo("embedded.cluster-password", "embeddedClusterPassword");
                mapper.MapFromTo("embedded.data-directory", "embeddedDataDirectory");
                mapper.MapFromTo("embedded.enabled", "embeddedEnabled");
                mapper.MapFromTo("embedded.persistent", "embeddedPersistent");
                mapper.MapFromTo("embedded.queues", "embeddedQueues");
                mapper.MapFromTo("embedded.server-id", "embeddedServerId");
                mapper.MapFromTo("embedded.topics", "embeddedTopics");

                // Note: Spring adds spring.rabbitmq.pool configuration settings also
                // 
                // mapper.MapFromTo("pool.block-if-full","spring.rabbitmq.pool.block-if-full");
                // mapper.MapFromTo("pool.block-if-full-timeout","spring.rabbitmq.pool.block-if-full-timeout");
                // mapper.MapFromTo("pool.enabled","spring.rabbitmq.pool.enabled");
                // mapper.MapFromTo("pool.idle-timeout","spring.rabbitmq.pool.idle-timeout");
                // mapper.MapFromTo("pool.max-connections","spring.rabbitmq.pool.max-connections");
                // mapper.MapFromTo("pool.max-sessions-per-connection","spring.rabbitmq.pool.max-sessions-per-connection");
                // mapper.MapFromTo("pool.time-between-expiration-check","spring.rabbitmq.pool.time-between-expiration-check");
                // mapper.MapFromTo("pool.use-anonymous-producers","spring.rabbitmq.pool.use-anonymous-producers");
            });
    }
}

internal sealed class CassandraPostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "cassandra";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.data.cassandra....
                // Steeltoe -> steeltoe:cassandra:binding-name:....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("cluster-name", "clusterName");
                mapper.MapFromTo("compression", "compression");
                mapper.MapFromTo("contact-points","contactPoints");
                mapper.MapFromTo("keyspace-name","keyspaceName");
                mapper.MapFromTo("password","password");
                mapper.MapFromTo("port","port");
                mapper.MapFromTo("ssl","ssl");
                mapper.MapFromTo("username","username");
            });
    }
}

internal sealed class ConfigServerPostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "config";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.cloud.config....
                // Steeltoe -> steeltoe:config:binding-name...
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("uri", "uri");
                mapper.MapFromTo("client-id", "oauth2", "clientId");
                mapper.MapFromTo("client-secret",  "oauth2", "clientSecret");
                mapper.MapFromTo("access-token-uri", "oauth2", "accessTokenUri");
            });
    }
}

internal sealed class CouchbasePostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "couchbase";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.couchbase....
                // Steeltoe -> steeltoe:couchbase:binding-name:....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("bootstrap-hosts", "bootstrapHosts");
                mapper.MapFromTo("bucket.name", "bucketName");
                mapper.MapFromTo("bucket.password", "bucketPassword");
                mapper.MapFromTo("env.bootstrap.http-direct-port", "envBootstrapHttpDirectPort");
                mapper.MapFromTo("env.bootstrap.http-ssl-port", "envBootstrapHttpSslPort");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("username", "username");
            });
    }
}

internal sealed class DB2PostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "db2";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.datasource....
                // Steeltoe -> steeltoe:db2:binding-name....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("username", "username");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("host", "host");
                mapper.MapFromTo("port", "port");
                mapper.MapFromTo("database", "database");

                // Spring indicates this takes precedence over above
                mapper.MapFromTo("jdbc-url", "jdbcUrl");

                // Note: Spring also adds spring.r2dbc.... properties as well
            });
    }
}

internal sealed class ElasticSearchPostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "elasticsearch";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.data.elasticsearch.client... and spring.elasticsearch.jest.... and spring.elasticsearch.rest....
                // Steeltoe -> steeltoe:elasticsearch:binding-name:....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("endpoints","endpoints");
                mapper.MapFromTo("password","password");
                mapper.MapFromTo("use-ssl","useSsl");
                mapper.MapFromTo("username","username");
                mapper.MapFromTo("proxy.host","proxyHost");
                mapper.MapFromTo("proxy.port","proxyPort");
                mapper.MapFromTo("uris","uris");
            });
    }
}

internal sealed class EurekaPostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "eureka";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> eureka.client....
                // Steeltoe -> steeltoe:eureka:binding-name:....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.AddKeyValue("region", "default");
                mapper.MapFromTo("client-id", "oauth2", "clientId");
                mapper.MapFromTo("client-secret", "oauth2", "clientSecret");
                mapper.MapFromTo("access-token-uri", "oauth2", "accessTokenUri");

                // Note: Spring -> eureka.client.serviceUrl.defaultZone and remaps underlying uri value to "%s/eureka/"
                mapper.MapFromTo("uri", "uri");

                // Note: Spring also adds eureka.client.region == default
            });
    }
}

internal sealed class KafkaPostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "kafka";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.kafka....
                // Steeltoe -> steeltoe:kafka:binding-name:....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("bootstrap-servers", "bootstrapServers");
                mapper.MapFromTo("consumer.bootstrap-servers","consumerBootstrapServers");
                mapper.MapFromTo("producer.bootstrap-servers","producerBootstrapServers");
                mapper.MapFromTo("streams.bootstrap-servers", "streamsBootstrapServers");
            });
    }
}

internal sealed class LdapPostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "ldap";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.ldap....
                // Steeltoe -> steeltoe:ldap:binding-name:....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("base", "base");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("urls", "urls");
                mapper.MapFromTo("username", "username");
            });
    }
}

internal sealed class MongoDbPostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "mongodb";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.data.mongodb....
                // Steeltoe -> steeltoe:mongodb....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("authentication-database", "authenticationDatabase");
                mapper.MapFromTo("database", "database");
                mapper.MapFromTo("grid-fs-database", "gridfsDatabase");
                mapper.MapFromTo("host", "host");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("port", "port");
                mapper.MapFromTo("uri", "uri");
                mapper.MapFromTo("username", "username");
            });
    }
}

internal sealed class MySqlPostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "mysql";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.datasource....
                // Steeltoe -> steeltoe:mysql:binding-name....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("username", "username");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("host", "host");
                mapper.MapFromTo("port", "port");
                mapper.MapFromTo("database", "database");

                // Spring indicates this takes precedence over above
                mapper.MapFromTo("jdbc-url", "jdbcUrl"); 

                // Note: Spring also adds spring.r2dbc.... properties as well
            });
    }
}

internal sealed class Neo4JPostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "neo4j";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.data.neo4j....
                // Steeltoe -> steeltoe:neo4j:binding-name....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("username", "username");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("uri", "uri");
            });
    }
}

internal sealed class OraclePostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "oracle";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.datasource....
                // Steeltoe -> steeltoe:oracle:binding-name....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("username", "username");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("host", "host");
                mapper.MapFromTo("port", "port");
                mapper.MapFromTo("database", "database");

                // Spring indicates this takes precedence over above
                mapper.MapFromTo("jdbc-url", "jdbcUrl");

                // Note: Spring also adds spring.r2dbc.... properties as well
            });
    }
}

internal sealed class PostgreSqlPostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "postgresql";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.datasource....
                // Steeltoe -> steeltoe:postgresql:binding-name....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("username", "username");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("host", "host");
                mapper.MapFromTo("port", "port");
                mapper.MapFromTo("database", "database");

                // Note, look at the Spring PostgreSqlBindingsPropertiesProcessor for format
                // of the below key values
                mapper.MapFromTo("sslmode", "sslmode");
                mapper.MapFromTo("sslrootcert", "sslrootcert");
                mapper.MapFromTo("options", "options");

                // Spring indicates this takes precedence over above
                mapper.MapFromTo("jdbc-url", "jdbcUrl");

                // Note: Spring also adds spring.r2dbc.... properties as well
            });
    }
}

internal sealed class RabbitMQPostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "rabbitmq";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.rabbitmq....
                // Steeltoe -> steeltoe:rabbitmq:binding-name:....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("addresses", "addresses");
                mapper.MapFromTo("host", "host");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("port", "port");
                mapper.MapFromTo("username", "username");
                mapper.MapFromTo("virtual-host", "virtualHost");
            });
    }
}

internal sealed class RedisPostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "redis";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.redis....
                // Steeltoe -> steeltoe:redis:binding-name:....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("client-name", "clientName");
                mapper.MapFromTo("cluster.max-redirects", "clusterMaxRedirects");
                mapper.MapFromTo("cluster.nodes", "clusterNodes");
                mapper.MapFromTo("database", "database");
                mapper.MapFromTo("host", "host");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("port", "port");
                mapper.MapFromTo("sentinel.master", "sentinelMaster");
                mapper.MapFromTo("sentinel.nodes", "sentinelNodes");
                mapper.MapFromTo("ssl", "ssl");
                mapper.MapFromTo("url","url");
            });
    }
}

internal sealed class SapHanaPostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "hana";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.datasource....
                // Steeltoe -> steeltoe:hana:binding-name:....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("username", "username");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("host", "host");
                mapper.MapFromTo("port", "port");
                mapper.MapFromTo("database", "database");

                // Spring indicates this takes precedence over above
                mapper.MapFromTo("jdbc-url", "jdbcUrl");

                // Note: Spring also adds spring.r2dbc.... properties as well
            });
    }
}

internal sealed class SpringSecurityOAuth2PostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "oauth2";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.security.oauth2.client....
                // Steeltoe -> steeltoe:oauth2:binding-name:....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", "oauth2", ConfigurationPath.GetSectionKey(bindingNameKey));
                string provider = mapper.BindingProvider;
                if (provider == null)
                {
                    // Log
                    return;
                }

                mapper.AddKeyValue($"registration:provider", provider);

                mapper.MapFromTo("client-id", "registration", "clientId");
                mapper.MapFromTo("client-secret","registration", "clientSecret");
                mapper.MapFromTo("client-authentication-method", "registration", "clientAuthenticationMethod");
                mapper.MapFromTo("authorization-grant-type","registration", "authorizationGrantType");

                // Look at Springs SpringSecurityOAuth2BindingsPropertiesProcessor for details on this
                mapper.MapFromTo("authorization-grant-types", "registration", "authorizationGrantTypes");
        
                mapper.MapFromTo("redirect-uri","registration", "redirectUri");

                // Look at Springs SpringSecurityOAuth2BindingsPropertiesProcessor for details on this
                mapper.MapFromTo("redirect-uris", "registration","redirectUris");
                mapper.MapFromTo("scope", "registration","scope");
                mapper.MapFromTo("client-name","registration", "clientName");

                mapper.MapFromTo("issuer-uri", "provider", "issuerUri");
                mapper.MapFromTo("authorization-uri","provider", "authorizationUri");
                mapper.MapFromTo("token-uri","provider", "tokenUri");
                mapper.MapFromTo("user-info-uri","provider", "userInfoUri");
                mapper.MapFromTo("user-info-authentication-method", "provider", "userInfoAuthenticationMethod");
                mapper.MapFromTo("jwk-set-uri","provider", "jwkSetUri");
                mapper.MapFromTo("user-name-attribute","provider", "userNameAttribute");

            });
    }
}

internal sealed class SqlServerPostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "sqlserver";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.datasource....
                // Steeltoe -> steeltoe:sqlserver:binding-name:....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("username", "username");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("host", "host");
                mapper.MapFromTo("port", "port");
                mapper.MapFromTo("database", "database");

                // Spring indicates this takes precedence over above
                mapper.MapFromTo("jdbc-url", "jdbcUrl");

                // Note: Spring also adds spring.r2dbc.... properties as well
            });
    }
}

internal sealed class VaultPostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "vault";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.cloud.vault....
                // Steeltoe -> steeltoe:vault:binding-name:....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("uri", "uri");
                mapper.MapFromTo("namespace", "namespace");
                string authenticationMethod = mapper.Get("authentication-method");
                if (authenticationMethod == null)
                {
                    // Log
                    return;
                }
                mapper.AddKeyValue("authentication", authenticationMethod);
                switch (authenticationMethod.ToUpperInvariant())
                {
                    case "TOKEN":
                    case "CUBBYHOLE":
                        mapper.MapFromTo("token", "token");
                        break;
                    case "APPROLE":
                        mapper.MapFromTo("role-id","approle", "roleId");
                        mapper.MapFromTo("secret-id","approle", "secretId");
                        mapper.MapFromTo("role","approle", "role");
                        mapper.MapFromTo("app-role-path","approle","appRolePath");
                        break;
                    case "AWS_EC2":
                        mapper.MapFromTo("role","awsEc2", "role");
                        mapper.MapFromTo("aws-ec2-path","awsEc2", "awsEc2Path");
                        mapper.MapFromTo("aws-ec2-instance-identity-document","awsEc2", "identityDocument");
                        mapper.MapFromTo("nonce","awsEc2", "nonce");
                        break;
                    case "AWS_IAM":
                        mapper.MapFromTo("role","awsIam", "role");
                        mapper.MapFromTo("aws-path","awsIam", "awsPath");
                        mapper.MapFromTo("aws-iam-server-id","awsIam", "serverId");
                        mapper.MapFromTo("aws-sts-endpoint-uri","awsIam", "endpointUri");
                        break;
                    case "AZURE_MSI":
                        mapper.MapFromTo("role","azureMsi", "role");
                        mapper.MapFromTo("azure-path","azureMsi", "azurePath");
                        break;
                    case "CERT":
                        // Note: Spring adds the keystore.jks file path as the value of the key
                        // spring.cloud.vault.ssl.key-store. This needs to be revisted to see what
                        // Steeltoe should do. For now, look for key-store key and just map it
                        mapper.MapFromTo("key-store", "ssl", "keyStore");
                        mapper.MapFromTo("key-store-password","ssl", "keyStorePassword");
                        mapper.MapFromTo("cert-auth-path","ssl", "certAuthPath");
                        break;
                    case "GCP_GCE":
                        mapper.MapFromTo("role","gcpGce", "role");
                        mapper.MapFromTo("gcp-path", "gcpGce", "gcpPath");
                        mapper.MapFromTo("gcp-service-account", "gcpGce", "gcpServiceAccount");
                        break;
                    case "GCP_IAM":
                        // Note: Spring adds file path for credentials.json file as the value of the key
                        // spring.cloud.vault.gcp-iam.credentials.location. This needs to be revisted to see what
                        // Steeltoe should do. For now, look for credentials.json key and just map it
                        mapper.MapFromTo("credentials.json", "gcpIam", "credentialsJson");
                        mapper.MapFromTo("role","gcpIam", "role");
                        mapper.MapFromTo("encoded-key","gcpIam", "encodedKey");
                        mapper.MapFromTo("gcp-path","gcpIam", "gcpPath");
                        mapper.MapFromTo("jwt-validity","gcpIam", "jwtValidity");
                        mapper.MapFromTo("gcp-project-id","gcpIam", "gcpProjectId");
                        mapper.MapFromTo("gcp-service-account","gcpIam", "gcpServiceAccount");
                        break;
                    case "KUBERNETES":
                        mapper.MapFromTo("role","kubernetes","role");
                        mapper.MapFromTo("kubernetes-path","kubernetes", "kubernetesPath");
                        break;
                    default:
                        // log 
                        break;
                }
            });
    }
}

internal sealed class WavefrontPostProcessor : IConfigurationPostProcessor
{
     internal const string BindingTypeKey = "wavefront";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> management.metrics.export.....
                // Steeltoe -> steeltoe:wavefront:binding-name: ...
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "steeltoe", BindingTypeKey, ConfigurationPath.GetSectionKey(bindingNameKey));
                mapper.MapFromTo("api-token", "apiToken");
                mapper.MapFromTo("uri", "uri");
            });
    }
}
