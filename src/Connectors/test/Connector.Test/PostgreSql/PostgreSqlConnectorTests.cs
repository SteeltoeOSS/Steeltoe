// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;
using Steeltoe.Connector.PostgreSql;
using Xunit;

namespace Steeltoe.Connector.Test.PostgreSql;

public sealed class PostgreSqlConnectorTests
{
    private const string MultiVcapServicesJson = @"{
  ""csb-azure-postgresql"": [
    {
      ""binding_guid"": ""5457fd42-c36f-42e0-afef-eef4a44aa6b7"",
      ""binding_name"": null,
      ""credentials"": {
        ""hostname"": ""csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc.postgres.database.cloud-hostname.com"",
        ""jdbcUrl"": ""jdbc:postgresql://csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc.postgres.database.cloud-hostname.com:5432/vsbdb?user=leJdXEfOyoNsniyO%40csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc.postgres.database.cloud-hostname.com&password=T3Cg.DpMm7TPozIxb~1nEkU6S-mOBIAuuZI_RtEhqoU1IKib.SE~.__7UmsGo.dS&verifyServerCertificate=true&useSSL=true&requireSSL=false&serverTimezone=GMT"",
        ""name"": ""vsbdb"",
        ""password"": ""T3Cg.DpMm7TPozIxb~1nEkU6S-mOBIAuuZI_RtEhqoU1IKib.SE~.__7UmsGo.dS"",
        ""port"": 5432,
        ""status"": ""created db vsbdb (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DBforPostgreSQL/servers/csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc/databases/vsbdb) on server csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DBforPostgreSQL/servers/csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc) URL: https://portal.cloud-hostname.com/#@b39138ca-3cee-4b4a-a4d6-cd83d9dd62f0/resource/subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DBforPostgreSQL/servers/csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc"",
        ""uri"": ""postgresql://leJdXEfOyoNsniyO%40csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc.postgres.database.cloud-hostname.com:T3Cg.DpMm7TPozIxb~1nEkU6S-mOBIAuuZI_RtEhqoU1IKib.SE~.__7UmsGo.dS@csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc.postgres.database.cloud-hostname.com:5432/vsbdb"",
        ""use_tls"": true,
        ""username"": ""leJdXEfOyoNsniyO@csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc.postgres.database.cloud-hostname.com""
      },
      ""instance_guid"": ""37502b38-167a-4a68-833d-dd2662d7eafc"",
      ""instance_name"": ""myPostgreSqlServiceAzureOne"",
      ""label"": ""csb-azure-postgresql"",
      ""name"": ""myPostgreSqlServiceAzureOne"",
      ""plan"": ""small"",
      ""provider"": null,
      ""syslog_drain_url"": null,
      ""tags"": [
        ""azure"",
        ""postgresql"",
        ""postgres"",
        ""preview""
      ],
      ""volume_mounts"": []
    },
    {
      ""binding_guid"": ""6a302359-5d5e-400d-bad5-daf9fbd58d49"",
      ""binding_name"": null,
      ""credentials"": {
        ""hostname"": ""csb-postgresql-80597418-b6c4-481a-a3dd-eb3efe296952.postgres.database.cloud-hostname.com"",
        ""jdbcUrl"": ""jdbc:postgresql://csb-postgresql-80597418-b6c4-481a-a3dd-eb3efe296952.postgres.database.cloud-hostname.com:5432/vsbdb?user=TAwlYVWerRtnHKWI%40csb-postgresql-80597418-b6c4-481a-a3dd-eb3efe296952.postgres.database.cloud-hostname.com&password=r7sxQ~1Jcfhl2s8jh7WyXPjzg9Qwec~J47L0VHta.8NSW8JKMM3k88CJ.BZwkx1X&verifyServerCertificate=true&useSSL=true&requireSSL=false&serverTimezone=GMT"",
        ""name"": ""vsbdb"",
        ""password"": ""r7sxQ~1Jcfhl2s8jh7WyXPjzg9Qwec~J47L0VHta.8NSW8JKMM3k88CJ.BZwkx1X"",
        ""port"": 5432,
        ""status"": ""created db vsbdb (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DBforPostgreSQL/servers/csb-postgresql-80597418-b6c4-481a-a3dd-eb3efe296952/databases/vsbdb) on server csb-postgresql-80597418-b6c4-481a-a3dd-eb3efe296952 (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DBforPostgreSQL/servers/csb-postgresql-80597418-b6c4-481a-a3dd-eb3efe296952) URL: https://portal.cloud-hostname.com/#@b39138ca-3cee-4b4a-a4d6-cd83d9dd62f0/resource/subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DBforPostgreSQL/servers/csb-postgresql-80597418-b6c4-481a-a3dd-eb3efe296952"",
        ""uri"": ""postgresql://TAwlYVWerRtnHKWI%40csb-postgresql-80597418-b6c4-481a-a3dd-eb3efe296952.postgres.database.cloud-hostname.com:r7sxQ~1Jcfhl2s8jh7WyXPjzg9Qwec~J47L0VHta.8NSW8JKMM3k88CJ.BZwkx1X@csb-postgresql-80597418-b6c4-481a-a3dd-eb3efe296952.postgres.database.cloud-hostname.com:5432/vsbdb"",
        ""use_tls"": true,
        ""username"": ""TAwlYVWerRtnHKWI@csb-postgresql-80597418-b6c4-481a-a3dd-eb3efe296952.postgres.database.cloud-hostname.com""
      },
      ""instance_guid"": ""80597418-b6c4-481a-a3dd-eb3efe296952"",
      ""instance_name"": ""myPostgreSqlServiceAzureTwo"",
      ""label"": ""csb-azure-postgresql"",
      ""name"": ""myPostgreSqlServiceAzureTwo"",
      ""plan"": ""small"",
      ""provider"": null,
      ""syslog_drain_url"": null,
      ""tags"": [
        ""azure"",
        ""postgresql"",
        ""postgres"",
        ""preview""
      ],
      ""volume_mounts"": []
    }
  ],
  ""csb-google-postgres"": [
    {
      ""binding_guid"": ""f63060dd-8dcd-4cd8-89a7-52f4d5985fd9"",
      ""binding_name"": null,
      ""credentials"": {
        ""hostname"": ""10.237.48.17"",
        ""jdbcUrl"": ""jdbc:postgresql://10.237.48.17:5432/csb-db?user=HQGEmFKnZtVOTbtC&password=~YZZEwdvF5EobA1j5-ywO9z~D4Lf3UxxrGZ-7L~L-BWn_ltjH03iFp4oqTLLB7Ev&ssl=true"",
        ""name"": ""csb-db"",
        ""password"": ""~YZZEwdvF5EobA1j5-ywO9z~D4Lf3UxxrGZ-7L~L-BWn_ltjH03iFp4oqTLLB7Ev"",
        ""port"": 5432,
        ""require_ssl"": true,
        ""sslcert"": ""-----BEGIN CERTIFICATE-----\nMIIDsTCCApmgAwIBAgIEQVzbnDANBgkqhkiG9w0BAQsFADCBiDEtMCsGA1UELhMk\nMDc4MDkwN2QtNTZjMS00ZWYzLWIwM2QtZGRlMTQxYzMyNGI0MTQwMgYDVQQDEytH\nb29nbGUgQ2xvdWQgU1FMIENsaWVudCBDQSAza1ZLbXVZNkNMQktyUkozMRQwEgYD\nVQQKEwtHb29nbGUsIEluYzELMAkGA1UEBhMCVVMwHhcNMjMwMjE0MTYzOTE1WhcN\nMzMwMjExMTY0MDE1WjA+MRkwFwYDVQQDExAza1ZLbXVZNkNMQktyUkozMRQwEgYD\nVQQKEwtHb29nbGUsIEluYzELMAkGA1UEBhMCVVMwggEiMA0GCSqGSIb3DQEBAQUA\nA4IBDwAwggEKAoIBAQCxLcXEF2rKjLyeyjbAq/nW6RwJJRN25s2d9c0RoKksFoG9\nl8hoXEQWrUtdZrvjkBmKsqQpkCfyAuIaDd2fAZzneGI10UfZt38Q5/2sduQuIQXQ\nwaoxMaAohn9xiI9ObpzZfUhtZRkv2QtNkrWiT9JXOw8qrKziAgYhW2L4u8EAVCs6\nY24D4Qfuo1xG+CFh2kx5RjZN9N7r10zsPqGc+NPvJp6NlSK85MTqG2Lznd/9G+Lv\nSqX/JWzw8QV0flW0cjWP/zSghakpVNdfx9wvWmKsNtdE970jYcTN9pSsetaxUlkn\nJiJU4EoSf/CFFvll5ztjtbdzLf109S84HedmenkjAgMBAAGjbDBqMAkGA1UdEwQC\nMAAwXQYDVR0RBFYwVIFSZG90bmV0LWRldi14LXNlcnZpY2UtYWNjb3VudC0xQGRv\ndG5ldC1kZXZlbG9wZXItZXhwZXJpZW5jZS5pYW0uZ3NlcnZpY2VhY2NvdW50LmNv\nbTANBgkqhkiG9w0BAQsFAAOCAQEAOvzAh7GvLU6N6QkWNNmlDU1UsFqH3c5jOhIs\nE8UqxpNZwaALA5p8Fafl6mkRm2yZQXNs9T0JZyiNg9BsMUBrQiPDLHyR2WCATGth\noVlnExWYJcUHHyFpKYTm6Ytqs/Bs1OxLo7NCOdvkObgnGCz4iVPTrtyxNQVvScBt\npzeVRleivS2vF776tnDWGZx9Tg4GoMEOj7gU5uRJlQkPsGYLmPanss2ZMgW2yOnW\nUM9LwS5+AKKVWRqzvAUL4ovKc8DGZxSpz5KV6QojRWmpRePf9+yE8HOSDH5oWDws\niYuC/fI70WA8NNPrILYmm6IH5DhuUbOKVpoara8/gf0blakpqw==\n-----END CERTIFICATE-----"",
        ""sslkey"": ""-----BEGIN RSA PRIVATE KEY-----\nMIIEpAIBAAKCAQEAsS3FxBdqyoy8nso2wKv51ukcCSUTdubNnfXNEaCpLBaBvZfI\naFxEFq1LXWa745AZirKkKZAn8gLiGg3dnwGc53hiNdFH2bd/EOf9rHbkLiEF0MGq\nMTGgKIZ/cYiPTm6c2X1IbWUZL9kLTZK1ok/SVzsPKqys4gIGIVti+LvBAFQrOmNu\nA+EH7qNcRvghYdpMeUY2TfTe69dM7D6hnPjT7yaejZUivOTE6hti853f/Rvi70ql\n/yVs8PEFdH5VtHI1j/80oIWpKVTXX8fcL1pirDbXRPe9I2HEzfaUrHrWsVJZJyYi\nVOBKEn/whRb5Zec7Y7W3cy39dPUvOB3nZnp5IwIDAQABAoIBAD0iUwu/HLz45JhR\no6TFcjZiRqctINM0/huT1YdQjS7GMUwO1DqWI3RDpS58JTZ24SlHTHd+4xmRPxzI\nTgDwWIhRtGlnZchMlU5rDc91UnRMNqp6OFQzEFULW2v8N55TclFk9hmw+YjV9h11\nErEHo8cvfKM344s0MZNO6g5zEjwfOpFWSZd6V5B4mZlF9EVRfQT7lYfhAeHAEdm2\nNOKDfNzAgSHnMv0eOZH4ls3Hc7rg/dNR2nz52H/X/USTDpjOmbP0p6xaHAFwSLk4\nnCn8+a2ukCQfDZDkwAChhQ5aR7uaHu2j8dxjiHK02uprL2/fVuGpYDMeFqbtIwF8\ndLBog9ECgYEA8UQvMVBxcge+9Fz9nVwSvALtkc9XFrsqsWaTQtoYiX/RkZE7TVOl\npr/bP3p8YKhpZw3j7WWNfaEwsN0TsLdVWh3Ijv9bb1trTO9ZwPtwBnzKYMTncfFW\njtfhzGlA63tIZU39120Pd+4n+yf7lMZ4IfcqSQmsTMIQj9JWH4wf6ikCgYEAu/+u\nac5efZBmBLLoSGoy58xA/6gfwpyV+F4UWofG1kp0saqk4ZmkHYR84LtvQUnX9aIq\njXU3MTEFUaFdLpWxoYiIxe2VaIIdjBj+rm+ZCMyZwoj+YFIoXQOddpsyg5HDPG3t\nU9tKUZ/IkmpuOtkSQlxGCR5Gm+QKIS9VNgHOCmsCgYEArx+A6disH8sDjjgZVqlI\nZ/PwIVBQtI0y1gXQikvoV5XRtkmms+AtczX7nL35nedgao8ojF6UL0ZbI2W1LyZD\n69+GflVYNyIyZmutyGg5zluyQj9qh8hXveNxYIBdwQ+BYxcTU9UzzyetGZ7R/BF7\njZvss4sz55tNjjdskAWT/NECgYEAsvuXV5BsEWs6VVrnHppM4LZrY3ry0ds2RIF9\nKzt9KGM2ejeWRlp6DsgmA+cu4p+lBWxgytA/vYuIHtFb35AQz1MntBifWCIYc1sQ\njY4dymzQLo8ybw2I9BUPAu56xxwtHgkiG+X4+YD/+bVuQISNh7RF0USLwLr4keN4\nYrSRLwUCgYAURMaw8BXwBsp8hB8cPWJ5G7K+9eyKImSmLUdKdAhV7g9uEKOk0uYg\nbR1Bjw0NBrcC7/tryf5kzKVdYs3FAHOR3qCFIaVGg97okwhOiMP6e6j0fBENDj8f\n7BzaXnC0iPbhCQwsVrdPHU7rwocyR4oBm+BVyLe6FqCb0+LijCgXYQ==\n-----END RSA PRIVATE KEY-----"",
        ""sslrootcert"": ""-----BEGIN CERTIFICATE-----\nMIIDfzCCAmegAwIBAgIBADANBgkqhkiG9w0BAQsFADB3MS0wKwYDVQQuEyRkYjJh\nYjhjMy01M2IwLTQ1OWItOWFlZS1lNGQ0YWM2NzM0NTkxIzAhBgNVBAMTGkdvb2ds\nZSBDbG91ZCBTUUwgU2VydmVyIENBMRQwEgYDVQQKEwtHb29nbGUsIEluYzELMAkG\nA1UEBhMCVVMwHhcNMjMwMjE0MTYxMTI5WhcNMzMwMjExMTYxMjI5WjB3MS0wKwYD\nVQQuEyRkYjJhYjhjMy01M2IwLTQ1OWItOWFlZS1lNGQ0YWM2NzM0NTkxIzAhBgNV\nBAMTGkdvb2dsZSBDbG91ZCBTUUwgU2VydmVyIENBMRQwEgYDVQQKEwtHb29nbGUs\nIEluYzELMAkGA1UEBhMCVVMwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIB\nAQCA4d9RSpxQLXLF37mNhRpnJmOoEBUuRGjD7A174jzyp20XYn8vY0TggnxhBJvJ\nACk8v4pFK4Unp6+Oi0nOsRLPGzV9MVBYIQFxthPsbtrD0E0rGmNXe5bP+inn4ODP\nui5mcfhGFgCd9h53a8qZciko2rQSSWnDygaHE3cMEfY8R98phKQW6UTDVKl3qlAO\nr2Z88piValBRIAo82Ae3Em8PFGpcHhTFJgayfClPkCaVCBt1gdVtZd+zQwuTY4C9\nhOy95GiXBJqS6SsLCmIZlwmbWiXFbn1Qa7Q1VLbTrNH9v/iXZjGDfjvARn5NBwVO\nYIAdN+IA2DkABIFLqnn6bqvXAgMBAAGjFjAUMBIGA1UdEwEB/wQIMAYBAf8CAQAw\nDQYJKoZIhvcNAQELBQADggEBAHvvee/erRZwKTIQ3OlzlUD8NW7CwsMkwYHbltng\nbJzcHAdftO9PtHQfwD4j5sKYmwURPIQ5JcMEIJdK68jRLOQmj1Op/Yz7X3Pdo/5U\n1dJ7h2zLwMaIHXujaDOqd4+wg4qQ78MOThxwowZDbNYlek4WdhdkXd3wxz9p7DmY\n4/TCpM5nMUYlvzj+QPDlnJ5DIprHkTpNcC4qRxl0OpHXkesn2xA5wNUAeX3E8EdA\nKon6YwS3p/ahuWH3Sw2uixG2i42TbUiG1aR6v/3sdBg7nwefqbA/iWFsLqxfL3tN\nSL339yG+A7oLjMhwO/PU2xJcNyppN6B3iR0EoNwltM+HFIs=\n-----END CERTIFICATE-----"",
        ""uri"": ""postgresql://HQGEmFKnZtVOTbtC:~YZZEwdvF5EobA1j5-ywO9z~D4Lf3UxxrGZ-7L~L-BWn_ltjH03iFp4oqTLLB7Ev@10.237.48.17:5432/csb-db"",
        ""username"": ""HQGEmFKnZtVOTbtC""
      },
      ""instance_guid"": ""e466d283-dde3-4220-8ca5-27d458287c3a"",
      ""instance_name"": ""myPostgreSqlServiceGoogle"",
      ""label"": ""csb-google-postgres"",
      ""name"": ""myPostgreSqlServiceGoogle"",
      ""plan"": ""default"",
      ""provider"": null,
      ""syslog_drain_url"": null,
      ""tags"": [
        ""gcp"",
        ""postgresql"",
        ""postgres""
      ],
      ""volume_mounts"": []
    }
  ],
  ""p.mysql"": [
    {
      ""binding_guid"": ""672a5fe9-9b25-4343-9594-1be5491535ba"",
      ""binding_name"": null,
      ""credentials"": {
        ""hostname"": ""ff2da938-3706-4511-bf22-03ab0e331629.mysql.service.internal"",
        ""jdbcUrl"": ""jdbc:mysql://ff2da938-3706-4511-bf22-03ab0e331629.mysql.service.internal:3306/service_instance_db?user=672a5fe99b25434395941be5491535ba&password=4xs1czjgupzbpnql&useSSL=false"",
        ""name"": ""service_instance_db"",
        ""password"": ""4xs1czjgupzbpnql"",
        ""port"": 3306,
        ""uri"": ""mysql://672a5fe99b25434395941be5491535ba:4xs1czjgupzbpnql@ff2da938-3706-4511-bf22-03ab0e331629.mysql.service.internal:3306/service_instance_db?reconnect=true"",
        ""username"": ""672a5fe99b25434395941be5491535ba""
      },
      ""instance_guid"": ""ff2da938-3706-4511-bf22-03ab0e331629"",
      ""instance_name"": ""myMySqlService"",
      ""label"": ""p.mysql"",
      ""name"": ""myMySqlService"",
      ""plan"": ""db-small"",
      ""provider"": null,
      ""syslog_drain_url"": null,
      ""tags"": [
        ""mysql""
      ],
      ""volume_mounts"": []
    }
  ]
}";

    private const string SingleVcapServicesJson = @"{
  ""csb-azure-postgresql"": [
    {
      ""binding_guid"": ""5457fd42-c36f-42e0-afef-eef4a44aa6b7"",
      ""binding_name"": null,
      ""credentials"": {
        ""hostname"": ""csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc.postgres.database.cloud-hostname.com"",
        ""jdbcUrl"": ""jdbc:postgresql://csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc.postgres.database.cloud-hostname.com:5432/vsbdb?user=leJdXEfOyoNsniyO%40csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc.postgres.database.cloud-hostname.com&password=T3Cg.DpMm7TPozIxb~1nEkU6S-mOBIAuuZI_RtEhqoU1IKib.SE~.__7UmsGo.dS&verifyServerCertificate=true&useSSL=true&requireSSL=false&serverTimezone=GMT"",
        ""name"": ""vsbdb"",
        ""password"": ""T3Cg.DpMm7TPozIxb~1nEkU6S-mOBIAuuZI_RtEhqoU1IKib.SE~.__7UmsGo.dS"",
        ""port"": 5432,
        ""status"": ""created db vsbdb (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DBforPostgreSQL/servers/csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc/databases/vsbdb) on server csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DBforPostgreSQL/servers/csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc) URL: https://portal.cloud-hostname.com/#@b39138ca-3cee-4b4a-a4d6-cd83d9dd62f0/resource/subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DBforPostgreSQL/servers/csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc"",
        ""uri"": ""postgresql://leJdXEfOyoNsniyO%40csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc.postgres.database.cloud-hostname.com:T3Cg.DpMm7TPozIxb~1nEkU6S-mOBIAuuZI_RtEhqoU1IKib.SE~.__7UmsGo.dS@csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc.postgres.database.cloud-hostname.com:5432/vsbdb"",
        ""use_tls"": true,
        ""username"": ""leJdXEfOyoNsniyO@csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc.postgres.database.cloud-hostname.com""
      },
      ""instance_guid"": ""37502b38-167a-4a68-833d-dd2662d7eafc"",
      ""instance_name"": ""myPostgreSqlServiceAzureOne"",
      ""label"": ""csb-azure-postgresql"",
      ""name"": ""myPostgreSqlServiceAzureOne"",
      ""plan"": ""small"",
      ""provider"": null,
      ""syslog_drain_url"": null,
      ""tags"": [
        ""azure"",
        ""postgresql"",
        ""postgres"",
        ""preview""
      ],
      ""volume_mounts"": []
    }
  ]
}";

    private static readonly HashSet<string> TempFileKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "SSL Certificate",
        "SSL Key",
        "Root Certificate"
    };

    [Fact]
    public async Task Binds_options_without_service_bindings()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:PostgreSql:myPostgreSqlServiceOne:ConnectionString"] = "SERVER=localhost;DB=db1;UID=user1;PWD=pass1",
            ["Steeltoe:Client:PostgreSql:myPostgreSqlServiceTwo:ConnectionString"] = "SERVER=localhost;DB=db2;UID=user2;PWD=pass2"
        });

        builder.AddPostgreSql();
        builder.Services.Configure<PostgreSqlOptions>("myPostgreSqlServiceOne", options => options.ConnectionString += ";Include Error Detail=true");

        await using WebApplication app = builder.Build();
        var optionsSnapshot = app.Services.GetRequiredService<IOptionsSnapshot<PostgreSqlOptions>>();

        PostgreSqlOptions optionsOne = optionsSnapshot.Get("myPostgreSqlServiceOne");
        optionsOne.ConnectionString.Should().NotBeNull();

        ExtractConnectionStringParameters(optionsOne.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Host=localhost",
            "Database=db1",
            "Username=user1",
            "Include Error Detail=true",
            "Password=pass1"
        }, options => options.WithoutStrictOrdering());

        PostgreSqlOptions optionsTwo = optionsSnapshot.Get("myPostgreSqlServiceTwo");
        optionsTwo.ConnectionString.Should().NotBeNull();

        ExtractConnectionStringParameters(optionsTwo.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Host=localhost",
            "Database=db2",
            "Username=user2",
            "Password=pass2"
        }, options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task Binds_options_with_CloudFoundry_service_bindings()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(MultiVcapServicesJson));

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:PostgreSql:myPostgreSqlServiceAzureOne:ConnectionString"] = "Include Error Detail=true;Log Parameters=true;host=localhost"
        });

        builder.AddPostgreSql();

        await using WebApplication app = builder.Build();
        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<PostgreSqlOptions>>();

        PostgreSqlOptions optionsAzureOne = optionsMonitor.Get("myPostgreSqlServiceAzureOne");
        optionsAzureOne.Should().NotBeNull();

        ExtractConnectionStringParameters(optionsAzureOne.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Include Error Detail=True",
            "Log Parameters=True",
            "Host=csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc.postgres.database.cloud-hostname.com",
            "Port=5432",
            "Database=vsbdb",
            "Username=leJdXEfOyoNsniyO@csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc.postgres.database.cloud-hostname.com",
            "Password=T3Cg.DpMm7TPozIxb~1nEkU6S-mOBIAuuZI_RtEhqoU1IKib.SE~.__7UmsGo.dS"
        }, options => options.WithoutStrictOrdering());

        PostgreSqlOptions optionsAzureTwo = optionsMonitor.Get("myPostgreSqlServiceAzureTwo");
        optionsAzureTwo.Should().NotBeNull();

        ExtractConnectionStringParameters(optionsAzureTwo.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Host=csb-postgresql-80597418-b6c4-481a-a3dd-eb3efe296952.postgres.database.cloud-hostname.com",
            "Port=5432",
            "Database=vsbdb",
            "Username=TAwlYVWerRtnHKWI@csb-postgresql-80597418-b6c4-481a-a3dd-eb3efe296952.postgres.database.cloud-hostname.com",
            "Password=r7sxQ~1Jcfhl2s8jh7WyXPjzg9Qwec~J47L0VHta.8NSW8JKMM3k88CJ.BZwkx1X"
        }, options => options.WithoutStrictOrdering());

        PostgreSqlOptions optionsGoogle = optionsMonitor.Get("myPostgreSqlServiceGoogle");
        optionsGoogle.ConnectionString.Should().NotBeNull();

        ExtractConnectionStringParameters(optionsGoogle.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Host=10.237.48.17",
            "Port=5432",
            "Database=csb-db",
            "Username=HQGEmFKnZtVOTbtC",
            "Password=~YZZEwdvF5EobA1j5-ywO9z~D4Lf3UxxrGZ-7L~L-BWn_ltjH03iFp4oqTLLB7Ev",
            @"Root Certificate=-----BEGIN CERTIFICATE-----
MIIDfzCCAmegAwIBAgIBADANBgkqhkiG9w0BAQsFADB3MS0wKwYDVQQuEyRkYjJh
YjhjMy01M2IwLTQ1OWItOWFlZS1lNGQ0YWM2NzM0NTkxIzAhBgNVBAMTGkdvb2ds
ZSBDbG91ZCBTUUwgU2VydmVyIENBMRQwEgYDVQQKEwtHb29nbGUsIEluYzELMAkG
A1UEBhMCVVMwHhcNMjMwMjE0MTYxMTI5WhcNMzMwMjExMTYxMjI5WjB3MS0wKwYD
VQQuEyRkYjJhYjhjMy01M2IwLTQ1OWItOWFlZS1lNGQ0YWM2NzM0NTkxIzAhBgNV
BAMTGkdvb2dsZSBDbG91ZCBTUUwgU2VydmVyIENBMRQwEgYDVQQKEwtHb29nbGUs
IEluYzELMAkGA1UEBhMCVVMwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIB
AQCA4d9RSpxQLXLF37mNhRpnJmOoEBUuRGjD7A174jzyp20XYn8vY0TggnxhBJvJ
ACk8v4pFK4Unp6+Oi0nOsRLPGzV9MVBYIQFxthPsbtrD0E0rGmNXe5bP+inn4ODP
ui5mcfhGFgCd9h53a8qZciko2rQSSWnDygaHE3cMEfY8R98phKQW6UTDVKl3qlAO
r2Z88piValBRIAo82Ae3Em8PFGpcHhTFJgayfClPkCaVCBt1gdVtZd+zQwuTY4C9
hOy95GiXBJqS6SsLCmIZlwmbWiXFbn1Qa7Q1VLbTrNH9v/iXZjGDfjvARn5NBwVO
YIAdN+IA2DkABIFLqnn6bqvXAgMBAAGjFjAUMBIGA1UdEwEB/wQIMAYBAf8CAQAw
DQYJKoZIhvcNAQELBQADggEBAHvvee/erRZwKTIQ3OlzlUD8NW7CwsMkwYHbltng
bJzcHAdftO9PtHQfwD4j5sKYmwURPIQ5JcMEIJdK68jRLOQmj1Op/Yz7X3Pdo/5U
1dJ7h2zLwMaIHXujaDOqd4+wg4qQ78MOThxwowZDbNYlek4WdhdkXd3wxz9p7DmY
4/TCpM5nMUYlvzj+QPDlnJ5DIprHkTpNcC4qRxl0OpHXkesn2xA5wNUAeX3E8EdA
Kon6YwS3p/ahuWH3Sw2uixG2i42TbUiG1aR6v/3sdBg7nwefqbA/iWFsLqxfL3tN
SL339yG+A7oLjMhwO/PU2xJcNyppN6B3iR0EoNwltM+HFIs=
-----END CERTIFICATE-----",
            @"SSL Certificate=-----BEGIN CERTIFICATE-----
MIIDsTCCApmgAwIBAgIEQVzbnDANBgkqhkiG9w0BAQsFADCBiDEtMCsGA1UELhMk
MDc4MDkwN2QtNTZjMS00ZWYzLWIwM2QtZGRlMTQxYzMyNGI0MTQwMgYDVQQDEytH
b29nbGUgQ2xvdWQgU1FMIENsaWVudCBDQSAza1ZLbXVZNkNMQktyUkozMRQwEgYD
VQQKEwtHb29nbGUsIEluYzELMAkGA1UEBhMCVVMwHhcNMjMwMjE0MTYzOTE1WhcN
MzMwMjExMTY0MDE1WjA+MRkwFwYDVQQDExAza1ZLbXVZNkNMQktyUkozMRQwEgYD
VQQKEwtHb29nbGUsIEluYzELMAkGA1UEBhMCVVMwggEiMA0GCSqGSIb3DQEBAQUA
A4IBDwAwggEKAoIBAQCxLcXEF2rKjLyeyjbAq/nW6RwJJRN25s2d9c0RoKksFoG9
l8hoXEQWrUtdZrvjkBmKsqQpkCfyAuIaDd2fAZzneGI10UfZt38Q5/2sduQuIQXQ
waoxMaAohn9xiI9ObpzZfUhtZRkv2QtNkrWiT9JXOw8qrKziAgYhW2L4u8EAVCs6
Y24D4Qfuo1xG+CFh2kx5RjZN9N7r10zsPqGc+NPvJp6NlSK85MTqG2Lznd/9G+Lv
SqX/JWzw8QV0flW0cjWP/zSghakpVNdfx9wvWmKsNtdE970jYcTN9pSsetaxUlkn
JiJU4EoSf/CFFvll5ztjtbdzLf109S84HedmenkjAgMBAAGjbDBqMAkGA1UdEwQC
MAAwXQYDVR0RBFYwVIFSZG90bmV0LWRldi14LXNlcnZpY2UtYWNjb3VudC0xQGRv
dG5ldC1kZXZlbG9wZXItZXhwZXJpZW5jZS5pYW0uZ3NlcnZpY2VhY2NvdW50LmNv
bTANBgkqhkiG9w0BAQsFAAOCAQEAOvzAh7GvLU6N6QkWNNmlDU1UsFqH3c5jOhIs
E8UqxpNZwaALA5p8Fafl6mkRm2yZQXNs9T0JZyiNg9BsMUBrQiPDLHyR2WCATGth
oVlnExWYJcUHHyFpKYTm6Ytqs/Bs1OxLo7NCOdvkObgnGCz4iVPTrtyxNQVvScBt
pzeVRleivS2vF776tnDWGZx9Tg4GoMEOj7gU5uRJlQkPsGYLmPanss2ZMgW2yOnW
UM9LwS5+AKKVWRqzvAUL4ovKc8DGZxSpz5KV6QojRWmpRePf9+yE8HOSDH5oWDws
iYuC/fI70WA8NNPrILYmm6IH5DhuUbOKVpoara8/gf0blakpqw==
-----END CERTIFICATE-----",
            @"SSL Key=-----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEAsS3FxBdqyoy8nso2wKv51ukcCSUTdubNnfXNEaCpLBaBvZfI
aFxEFq1LXWa745AZirKkKZAn8gLiGg3dnwGc53hiNdFH2bd/EOf9rHbkLiEF0MGq
MTGgKIZ/cYiPTm6c2X1IbWUZL9kLTZK1ok/SVzsPKqys4gIGIVti+LvBAFQrOmNu
A+EH7qNcRvghYdpMeUY2TfTe69dM7D6hnPjT7yaejZUivOTE6hti853f/Rvi70ql
/yVs8PEFdH5VtHI1j/80oIWpKVTXX8fcL1pirDbXRPe9I2HEzfaUrHrWsVJZJyYi
VOBKEn/whRb5Zec7Y7W3cy39dPUvOB3nZnp5IwIDAQABAoIBAD0iUwu/HLz45JhR
o6TFcjZiRqctINM0/huT1YdQjS7GMUwO1DqWI3RDpS58JTZ24SlHTHd+4xmRPxzI
TgDwWIhRtGlnZchMlU5rDc91UnRMNqp6OFQzEFULW2v8N55TclFk9hmw+YjV9h11
ErEHo8cvfKM344s0MZNO6g5zEjwfOpFWSZd6V5B4mZlF9EVRfQT7lYfhAeHAEdm2
NOKDfNzAgSHnMv0eOZH4ls3Hc7rg/dNR2nz52H/X/USTDpjOmbP0p6xaHAFwSLk4
nCn8+a2ukCQfDZDkwAChhQ5aR7uaHu2j8dxjiHK02uprL2/fVuGpYDMeFqbtIwF8
dLBog9ECgYEA8UQvMVBxcge+9Fz9nVwSvALtkc9XFrsqsWaTQtoYiX/RkZE7TVOl
pr/bP3p8YKhpZw3j7WWNfaEwsN0TsLdVWh3Ijv9bb1trTO9ZwPtwBnzKYMTncfFW
jtfhzGlA63tIZU39120Pd+4n+yf7lMZ4IfcqSQmsTMIQj9JWH4wf6ikCgYEAu/+u
ac5efZBmBLLoSGoy58xA/6gfwpyV+F4UWofG1kp0saqk4ZmkHYR84LtvQUnX9aIq
jXU3MTEFUaFdLpWxoYiIxe2VaIIdjBj+rm+ZCMyZwoj+YFIoXQOddpsyg5HDPG3t
U9tKUZ/IkmpuOtkSQlxGCR5Gm+QKIS9VNgHOCmsCgYEArx+A6disH8sDjjgZVqlI
Z/PwIVBQtI0y1gXQikvoV5XRtkmms+AtczX7nL35nedgao8ojF6UL0ZbI2W1LyZD
69+GflVYNyIyZmutyGg5zluyQj9qh8hXveNxYIBdwQ+BYxcTU9UzzyetGZ7R/BF7
jZvss4sz55tNjjdskAWT/NECgYEAsvuXV5BsEWs6VVrnHppM4LZrY3ry0ds2RIF9
Kzt9KGM2ejeWRlp6DsgmA+cu4p+lBWxgytA/vYuIHtFb35AQz1MntBifWCIYc1sQ
jY4dymzQLo8ybw2I9BUPAu56xxwtHgkiG+X4+YD/+bVuQISNh7RF0USLwLr4keN4
YrSRLwUCgYAURMaw8BXwBsp8hB8cPWJ5G7K+9eyKImSmLUdKdAhV7g9uEKOk0uYg
bR1Bjw0NBrcC7/tryf5kzKVdYs3FAHOR3qCFIaVGg97okwhOiMP6e6j0fBENDj8f
7BzaXnC0iPbhCQwsVrdPHU7rwocyR4oBm+BVyLe6FqCb0+LijCgXYQ==
-----END RSA PRIVATE KEY-----"
        }, options => options.WithoutStrictOrdering());

        CleanupTempFiles(optionsAzureOne.ConnectionString, optionsAzureTwo.ConnectionString, optionsGoogle.ConnectionString);
    }

    [Fact]
    public async Task Binds_options_with_Kubernetes_service_bindings()
    {
        try
        {
            string rootDir = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "resources", "bindings");
            Environment.SetEnvironmentVariable("SERVICE_BINDING_ROOT", rootDir);

            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.Configuration.AddEnvironmentVariables();
            builder.Configuration.AddKubernetesServiceBindings();

            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Steeltoe:Client:PostgreSql:customer-profiles:ConnectionString"] = "Include Error Detail=true;Log Parameters=true;host=localhost"
            });

            builder.AddPostgreSql();

            await using WebApplication app = builder.Build();
            var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<PostgreSqlOptions>>();

            PostgreSqlOptions customerProfilesOptions = optionsMonitor.Get("customer-profiles");
            customerProfilesOptions.Should().NotBeNull();

            ExtractConnectionStringParameters(customerProfilesOptions.ConnectionString).Should().BeEquivalentTo(new List<string>
            {
                "Include Error Detail=True",
                "Log Parameters=True",
                "Host=10.194.59.205",
                "Database=steeltoe",
                "Username=testrolee93ccf859894dc60dcd53218492b37b4",
                "Password=Qp!1mB1$Zk2T!$!D85_E"
            }, options => options.WithoutStrictOrdering());
        }
        finally
        {
            Environment.SetEnvironmentVariable("SERVICE_BINDING_ROOT", null);
        }
    }

    [Fact]
    public async Task Registers_ConnectionFactory()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:PostgreSql:myPostgreSqlServiceOne:ConnectionString"] = "SERVER=localhost;DB=db1;UID=user1;PWD=pass1",
            ["Steeltoe:Client:PostgreSql:myPostgreSqlServiceTwo:ConnectionString"] = "SERVER=localhost;DB=db2;UID=user2;PWD=pass2"
        });

        builder.AddPostgreSql();

        await using WebApplication app = builder.Build();

        var connectionFactory = app.Services.GetRequiredService<ConnectionFactory<PostgreSqlOptions, NpgsqlConnection>>();

        await using NpgsqlConnection connectionOne = connectionFactory.GetNamed("myPostgreSqlServiceOne").CreateConnection();
        connectionOne.ConnectionString.Should().Be("Host=localhost;Database=db1;Username=user1;Password=pass1");

        await using NpgsqlConnection connectionTwo = connectionFactory.GetNamed("myPostgreSqlServiceTwo").CreateConnection();
        connectionTwo.ConnectionString.Should().Be("Host=localhost;Database=db2;Username=user2;Password=pass2");
    }

    [Fact]
    public async Task Applies_configuration_changes()
    {
        string tempJsonPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            await File.WriteAllTextAsync(tempJsonPath, @"{
  ""Steeltoe"": {
    ""Client"": {
      ""PostgreSql"": {
        ""examplePostgreSqlService"": {
            ""ConnectionString"": ""SERVER=localhost;DB=db1""
        }
      }
    }
  }
}
");

            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.Configuration.AddJsonFile(tempJsonPath, false, true);

            builder.AddPostgreSql();

            await using WebApplication app = builder.Build();

            var connectionFactory = app.Services.GetRequiredService<ConnectionFactory<PostgreSqlOptions, NpgsqlConnection>>();

            string connectionString = connectionFactory.GetNamed("examplePostgreSqlService").Options.ConnectionString;
            connectionString.Should().Be("Host=localhost;Database=db1");

            await File.WriteAllTextAsync(tempJsonPath, @"{
  ""Steeltoe"": {
    ""Client"": {
      ""PostgreSql"": {
        ""examplePostgreSqlService"": {
            ""ConnectionString"": ""SERVER=remote.com;DB=other""
        }
      }
    }
  }
}
");

            await Task.Delay(TimeSpan.FromSeconds(2));

            connectionString = connectionFactory.GetNamed("examplePostgreSqlService").Options.ConnectionString;
            connectionString.Should().Be("Host=remote.com;Database=other");
        }
        finally
        {
            File.Delete(tempJsonPath);
        }
    }

    [Fact]
    public async Task Registers_HealthContributors()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:PostgreSql:myPostgreSqlServiceOne:ConnectionString"] = "SERVER=localhost;DB=db1;UID=user1;PWD=pass1",
            ["Steeltoe:Client:PostgreSql:myPostgreSqlServiceTwo:ConnectionString"] = "SERVER=localhost;DB=db2;UID=user2;PWD=pass2"
        });

        builder.AddPostgreSql();

        await using WebApplication app = builder.Build();

        IHealthContributor[] healthContributors = app.Services.GetServices<IHealthContributor>().ToArray();

        healthContributors.Should().HaveCount(2);
        healthContributors[0].Id.Should().Be("PostgreSQL-myPostgreSqlServiceOne");
        healthContributors[1].Id.Should().Be("PostgreSQL-myPostgreSqlServiceTwo");
    }

    [Fact]
    public async Task Registers_default_connection_string_when_single_server_binding_and_only_default_client_binding_found()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(SingleVcapServicesJson));

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:PostgreSql:Default:ConnectionString"] = "SERVER=localhost;DB=myDb;UID=myUser;PWD=myPass;Log Parameters=True"
        });

        builder.AddPostgreSql();

        await using WebApplication app = builder.Build();

        var connectionFactory = app.Services.GetRequiredService<ConnectionFactory<PostgreSqlOptions, NpgsqlConnection>>();
        string defaultConnectionString = connectionFactory.GetDefault().Options.ConnectionString;

        ExtractConnectionStringParameters(defaultConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Host=csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc.postgres.database.cloud-hostname.com",
            "Database=vsbdb",
            "Username=leJdXEfOyoNsniyO@csb-postgresql-37502b38-167a-4a68-833d-dd2662d7eafc.postgres.database.cloud-hostname.com",
            "Password=T3Cg.DpMm7TPozIxb~1nEkU6S-mOBIAuuZI_RtEhqoU1IKib.SE~.__7UmsGo.dS",
            "Log Parameters=True",
            "Port=5432"
        }, options => options.WithoutStrictOrdering());

        string namedConnectionString = connectionFactory.GetNamed("myPostgreSqlServiceAzureOne").Options.ConnectionString;
        namedConnectionString.Should().Be(defaultConnectionString);

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(1);
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_single_server_binding_found()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(SingleVcapServicesJson));

        builder.AddPostgreSql();

        await using WebApplication app = builder.Build();

        var connectionFactory = app.Services.GetRequiredService<ConnectionFactory<PostgreSqlOptions, NpgsqlConnection>>();

        string defaultConnectionString = connectionFactory.GetDefault().Options.ConnectionString;
        defaultConnectionString.Should().NotBeNull();

        string namedConnectionString = connectionFactory.GetNamed("myPostgreSqlServiceAzureOne").Options.ConnectionString;
        namedConnectionString.Should().Be(defaultConnectionString);

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(1);
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_default_client_binding_found()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:PostgreSql:Default:ConnectionString"] = "SERVER=localhost;DB=myDb;UID=myUser;PWD=myPass"
        });

        builder.AddPostgreSql();

        await using WebApplication app = builder.Build();

        var connectionFactory = app.Services.GetRequiredService<ConnectionFactory<PostgreSqlOptions, NpgsqlConnection>>();

        string defaultConnectionString = connectionFactory.GetDefault().Options.ConnectionString;
        defaultConnectionString.Should().NotBeNull();

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(1);
    }

    [Fact]
    public async Task Registers_no_default_connection_string_when_only_single_named_client_binding_found()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:PostgreSql:myPostgreSqlServiceAzureOne:ConnectionString"] = "host=localhost"
        });

        builder.AddPostgreSql();

        await using WebApplication app = builder.Build();

        var connectionFactory = app.Services.GetRequiredService<ConnectionFactory<PostgreSqlOptions, NpgsqlConnection>>();

        string defaultConnectionString = connectionFactory.GetDefault().Options.ConnectionString;
        defaultConnectionString.Should().BeNull();

        string namedConnectionString = connectionFactory.GetNamed("myPostgreSqlServiceAzureOne").Options.ConnectionString;
        namedConnectionString.Should().NotBeNull();

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(1);
    }

    [Fact]
    public async Task Registers_no_default_connection_string_when_multiple_client_bindings_found()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:PostgreSql:myPostgreSqlServiceAzureOne:ConnectionString"] = "host=localhost",
            ["Steeltoe:Client:PostgreSql:Default:ConnectionString"] = "host=ignored"
        });

        builder.AddPostgreSql();

        await using WebApplication app = builder.Build();

        var connectionFactory = app.Services.GetRequiredService<ConnectionFactory<PostgreSqlOptions, NpgsqlConnection>>();

        string defaultConnectionString = connectionFactory.GetDefault().Options.ConnectionString;
        defaultConnectionString.Should().BeNull();

        string namedConnectionString = connectionFactory.GetNamed("myPostgreSqlServiceAzureOne").Options.ConnectionString;
        namedConnectionString.Should().NotBeNull();

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(1);
    }

    [Fact]
    public async Task Registers_no_default_connection_string_when_multiple_server_bindings_found()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(MultiVcapServicesJson));

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:PostgreSql:Default:ConnectionString"] = "host=ignored"
        });

        builder.AddPostgreSql();

        await using WebApplication app = builder.Build();

        var connectionFactory = app.Services.GetRequiredService<ConnectionFactory<PostgreSqlOptions, NpgsqlConnection>>();

        string defaultConnectionString = connectionFactory.GetDefault().Options.ConnectionString;
        defaultConnectionString.Should().BeNull();

        string namedConnectionString = connectionFactory.GetNamed("myPostgreSqlServiceAzureOne").Options.ConnectionString;
        namedConnectionString.Should().NotBeNull();

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(3);
    }

    [Fact]
    public async Task Registers_no_default_connection_string_when_single_server_binding_and_multiple_client_bindings_found()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(SingleVcapServicesJson));

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:PostgreSql:myPostgreSqlServiceAzureOne:ConnectionString"] = "host=localhost",
            ["Steeltoe:Client:PostgreSql:Default:ConnectionString"] = "host=ignored"
        });

        builder.AddPostgreSql();

        await using WebApplication app = builder.Build();

        var connectionFactory = app.Services.GetRequiredService<ConnectionFactory<PostgreSqlOptions, NpgsqlConnection>>();

        string defaultConnectionString = connectionFactory.GetDefault().Options.ConnectionString;
        defaultConnectionString.Should().BeNull();

        string namedConnectionString = connectionFactory.GetNamed("myPostgreSqlServiceAzureOne").Options.ConnectionString;
        namedConnectionString.Should().NotBeNull();

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(1);
    }

    [Fact]
    public async Task Registers_no_default_connection_string_when_service_and_client_binding_found_with_different_names()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(SingleVcapServicesJson));

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:PostgreSql:alternatePostgreSqlService:ConnectionString"] = "host=localhost",
            ["Steeltoe:Client:PostgreSql:Default:ConnectionString"] = "host=ignored"
        });

        builder.AddPostgreSql();

        await using WebApplication app = builder.Build();

        var connectionFactory = app.Services.GetRequiredService<ConnectionFactory<PostgreSqlOptions, NpgsqlConnection>>();

        string defaultConnectionString = connectionFactory.GetDefault().Options.ConnectionString;
        defaultConnectionString.Should().BeNull();

        string namedConnectionString1 = connectionFactory.GetNamed("myPostgreSqlServiceAzureOne").Options.ConnectionString;
        namedConnectionString1.Should().NotBeNull();

        string namedConnectionString2 = connectionFactory.GetNamed("alternatePostgreSqlService").Options.ConnectionString;
        namedConnectionString2.Should().NotBeNull();

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(2);
    }

    private static IEnumerable<string> ExtractConnectionStringParameters(string connectionString)
    {
        List<string> entries = new();

        if (connectionString != null)
        {
            foreach (string parameter in connectionString.Split(';'))
            {
                string[] nameValuePair = parameter.Split('=', 2);

                if (nameValuePair.Length == 2)
                {
                    string name = nameValuePair[0];
                    string value = nameValuePair[1];

                    if (TempFileKeys.Contains(name))
                    {
                        value = File.ReadAllText(value);
                    }

                    value = value.Replace("\n", Environment.NewLine, StringComparison.Ordinal);

                    entries.Add($"{name}={value}");
                }
            }
        }

        return entries;
    }

    private static void CleanupTempFiles(params string[] connectionStrings)
    {
        foreach (string connectionString in connectionStrings)
        {
            foreach (string entry in connectionString.Split(';').ToArray())
            {
                string[] pair = entry.Split('=', 2);
                string key = pair[0];
                string value = pair[1];

                if (TempFileKeys.Contains(key) && File.Exists(value))
                {
                    File.Delete(value);
                }
            }
        }
    }
}
