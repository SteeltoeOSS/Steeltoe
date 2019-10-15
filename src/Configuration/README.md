# .NET Configuration Providers

[Custom configuration providers](https://docs.asp.net/en/latest/fundamentals/configuration.html#custom-config-providers) for use with Microsoft's [application configuration](https://docs.asp.net/en/latest/fundamentals/configuration.html) for accessing configuration settings for an application.

Steeltoe configuration providers can:

- Interact with [Spring Cloud Config](https://spring.io/projects/spring-cloud-config)
- Read [Cloud Foundry environment variables](https://docs.cloudfoundry.org/devguide/deploy-apps/environment-variable.html)
- Replace property placeholders
- Provide random values

For more information on how to use these components see the [Steeltoe documentation](https://steeltoe.io/).

## Sample Applications

See the `Configuration` directory inside the [Samples](https://github.com/SteeltoeOSS/Samples) repository for examples of how to use these packages.

## Known limitations with Spring Cloud Config Server

### Unstructured data files

Unlike the Java version of the configuration server client, the Steeltoe client currently only supports property and yaml files; not plain text.

### Client decryption

Steeltoe client only supports clear text communication with the configuration server. Client decryption is on our road map, but not currently supported. For now, you cannot send encrypted data to the client.

### Server initiated reload

Currently reloads must be initiated by the client, Steeltoe has not implemented handlers to listen for server change events.
