# Steeltoe Discovery Eureka Client

This project contains the Steeltoe Eureka Client.  This client provides access to the Netflix Eureka Server.  

## Provider Package Name and Feeds

`Steeltoe.Discovery.Eureka.Client`

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

[Release or Release Candidate feed](https://www.nuget.org/) - https://www.nuget.org/

## Basic Usage
You should have a good understanding of how to setup and use a Spring Cloud Netflix Eureka Server.  Detailed information on its usage can be found [here](http://projects.spring.io/spring-cloud/docs/1.0.3/spring-cloud.html#spring-cloud-eureka-server).

In order to use the Eureka client you need to do the following:
```
1. Confiure the settings the client will use to register services in the Eureka server.
2. Configure the settings the client will use to discover services in the Eureka server.  
3. Initialize the `DiscoveryManager` with the configured settings.
``` 
## Eureka Settings needed to Register
You use an instance of `EurekaInstanceConfig` to hold the settings the Eureka client will use when registering with the Eureka server. Below is an example which registers an instance named `instance_id` of the service `myServiceName` with the Eureka server. There are many other configuration settings you can apply here and you should look at the documentation in [IEurekaInstanceConfig](https://github.com/SteeltoeOSS/Discovery/blob/master/src/Steeltoe.Discovery.Eureka.Client/IEurekaInstanceConfig.cs) for more details.
```
#using Steeltoe.Discovery.Eureka;

EurekaInstanceConfig instanceConfig = new EurekaInstanceConfig()
{
    AppName = "myServiceName",
    InstanceId = "instance_id",
    IsInstanceEnabledOnInit = true
}
```
## Eureka Settings needed to Discover
You use an instance of `EurekaClientConfig` to hold the settings the client will use when fetching the service registry from the server.  Below is an example which configures the Eureka server address to be `http://localhost:8761/eureka/`. There are many other configuration settings you can apply here and you should look at the documentation in [IEurekaClientConfig](https://github.com/SteeltoeOSS/Discovery/blob/master/src/Steeltoe.Discovery.Eureka.Client/IEurekaClientConfig.cs) for more details.
```
#using Steeltoe.Discovery.Eureka;

EurekaClientConfig clientConfig = new EurekaClientConfig() 
{
    EurekaServerServiceUrls = "http://localhost:8761/eureka/"
}
```

## Initialize and Use the Eureka Client 
You use the `DiscoveryManager` class to initialize the client and cause it to register services and fetch the service registry.  If you don't need to register any services, then use the `Initialize()` method supplying the client config only.
```
#using Steeltoe.Discovery.Eureka;
...

// Register services and fetch the registry
DiscoveryManager.Instance.Initialize(clientConfig, instanceConfig);

or

// No services to register
DiscoveryManager.Instance.Initialize(clientConfig);
```
Once you have initialized the client you can obtain an instance of the `IEurekaClient` and use its methods to lookup services or instances.
```
IEurekaClient client = DiscoveryManager.Instance.Client;

Application app = client.GetApplication("SomeName");

```
# Known Limitations

### Version
This is a [Eureka 1.0 client](https://github.com/Netflix/eureka/wiki), not a 2.0 client. Eureka 2.0 is expected to have significant updates to its architecture and public API.

### AWS Support 
The Eureka client for Java contains features which enable operation on AWS.  The Steeltoe version does not currently implement those features, and instead, this version has been optimized for CloudFoundry environments. We will look at adding AWS cloud features at a future point in time.

### Configuration
Not all configuration properties found in the Java client are available for configuration. See [IEurekaClientConfig](https://github.com/SteeltoeOSS/Discovery/blob/master/src/Steeltoe.Discovery.Eureka.Client/IEurekaClientConfig.cs) and [IEurekaInstanceConfig](https://github.com/SteeltoeOSS/Discovery/blob/master/src/Steeltoe.Discovery.Eureka.Client/IEurekaInstanceConfig.cs) for configuration options.

