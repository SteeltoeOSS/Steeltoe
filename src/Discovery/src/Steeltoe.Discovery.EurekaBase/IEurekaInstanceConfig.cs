// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Discovery.Eureka.AppInfo;
using System.Collections.Generic;

namespace Steeltoe.Discovery.Eureka
{
    public interface IEurekaInstanceConfig
    {
        /// <summary>
        /// Gets or sets the unique Id (within the scope of the appName) of this instance to be registered with eureka.
        /// Configuration property: eureka:instance:instanceId
        /// </summary>
        string InstanceId { get; set; }

        /// <summary>
        /// Gets or sets the name of the application to be registered with eureka.
        /// Configuration property: eureka:instance:name
        /// </summary>
        string AppName { get; set; }

        /// <summary>
        /// Gets or sets the name of the application group to be registered with eureka.
        /// Configuration property: eureka:instance:appGroup
        /// </summary>
        string AppGroupName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether indicates whether the instance should be enabled for taking traffic as soon as it is registered with eureka.
        /// Sometimes the application might need to do some pre-processing before it is ready to take traffic.
        /// Configuration property: eureka:instance:instanceEnabledOnInit
        /// </summary>
        bool IsInstanceEnabledOnInit { get; set; }

        /// <summary>
        /// Gets or sets the <code>non-secure</code> port on which the instance should receive traffic.
        /// Configuration property: eureka:instance:port
        /// </summary>
        int NonSecurePort { get; set; }

        /// <summary>
        /// Gets or sets the <code>Secure port</code> on which the instance should receive traffic.
        /// Configuration property: eureka:instance:securePort
        /// </summary>
        int SecurePort { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether indicates whether the <code>non-secure</code> port should be enabled for traffic or not.
        /// Set true if the <code>non-secure</code> port is enabled, false otherwise.
        /// Configuration property: eureka:instance:nonSecurePortEnabled
        /// </summary>
        bool IsNonSecurePortEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether indicates whether the <code>secure</code> port should be enabled for traffic or not.
        /// Set true if the <code>secure</code> port is enabled, false otherwise.
        /// Configuration property: eureka:instance:securePortEnabled
        /// </summary>
        bool SecurePortEnabled { get; set; }

        /// <summary>
        /// Gets or sets indicates how often (in seconds) the eureka client needs to send heartbeats to eureka server
        /// to indicate that it is still alive. If the heartbeats are not received for the period specified in
        /// <see cref="LeaseExpirationDurationInSeconds" />, eureka server will removethe instance from its view,
        /// there by disallowing traffic to this instance.
        /// Note that the instance could still not take traffic if it implements HealthCheckCallback and then
        /// decides to make itself unavailable.
        /// Configuration property: eureka:instance:leaseRenewalIntervalInSeconds
        /// </summary>
        int LeaseRenewalIntervalInSeconds { get; set; }

        /// <summary>
        /// Gets or sets indicates the time in seconds that the eureka server waits since it received the last heartbeat before
        /// it can remove this instance from its view and there by disallowing traffic to this instance.
        ///
        /// Setting this value too long could mean that the traffic could be routed to the instance even though
        /// the instance is not alive. Setting this value too small could mean, the instance may be taken out
        /// of traffic because of temporary network glitches.This value to be set to atleast higher than
        /// the value specified in <see cref="LeaseRenewalIntervalInSeconds" />
        /// Configuration property: eureka:instance:leaseExpirationDurationInSeconds
        /// </summary>
        int LeaseExpirationDurationInSeconds { get; set; }

        /// <summary>
        /// Gets or sets the virtual host name defined for this instance.
        ///
        /// This is typically the way other instance would find this instance by using the virtual host name.
        /// Think of this as similar to the fully qualified domain name, that the users of your services will
        /// need to find this instance.
        /// Configuration property: eureka:instance:vipAddress
        /// </summary>
        string VirtualHostName { get; set; }

        /// <summary>
        /// Gets or sets the secure virtual host name defined for this instance.
        ///
        /// This is typically the way other instance would find this instance by using the virtual host name.
        /// Think of this as similar to the fully qualified domain name, that the users of your services will
        /// need to find this instance.
        /// Configuration property: eureka:instance:secureVipAddress
        /// </summary>
        string SecureVirtualHostName { get; set; }

        /// <summary>
        /// Gets or sets the <code>AWS autoscaling group name</code> associated with this instance. This information is
        /// specifically used in an AWS environment to automatically put an instance out of service after the instance is
        /// launched and it has been disabled for traffic..
        /// Configuration property: eureka:instance:asgName
        /// </summary>
        string ASGName { get; set; }

        /// <summary>
        /// Gets or sets the metadata name/value pairs associated with this instance. This information is sent to eureka
        /// server and can be used by other instances.
        /// Configuration property: eureka:instance:metadataMap
        /// </summary>
        IDictionary<string, string> MetadataMap { get; set; }

        /// <summary>
        /// Gets or sets the data center this instance is deployed. This information is used to get some AWS specific
        /// instance information if the instance is deployed in AWS.
        /// </summary>
        IDataCenterInfo DataCenterInfo { get; set; }

        /// <summary>
        /// Gets or sets the IPAdress of the instance. This information is for academic purposes only as the communication
        /// from other instances primarily happen using the information supplied in <see cref="GetHostName(bool)"/>
        /// </summary>
        string IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the relative status page <em>Path</em> for this instance. The status page URL is then constructed out of the
        /// <see cref="GetHostName(bool)"/> and the type of communication - secure or unsecure as specified in
        /// <see cref="SecurePort"/> and <see cref="NonSecurePort"/>.
        ///
        /// It is normally used for informational purposes for other services to findabout the status of this instance.
        /// Users can provide a simple <code>HTML</code> indicating what is the current status of the instance.
        /// Configuration property: eureka:instance:statusPageUrlPath
        /// </summary>
        string StatusPageUrlPath { get; set; }

        /// <summary>
        /// Gets or sets the absolute status page for this instance. The users can provide the StatusPageUrlPath if the status page
        /// resides in the same instance talking to eureka, else in the cases where the instance is a proxy for some other server,
        /// users can provide the full URL. If the full URL is provided it takes precedence.
        ///
        /// It is normally used for informational purposes for other services tofind about the status of this instance.
        /// Users can provide a simple<code>HTML</code> indicating what is the current status of the instance.
        /// The full URL should follow the format http://${eureka.hostname}:7001/ where the value ${eureka.hostname} is
        /// replaced at runtime.
        /// Configuration property: eureka:instance:statusPageUrl
        /// </summary>
        string StatusPageUrl { get; set; }

        /// <summary>
        /// Gets or sets gets the relative home page URL <em>Path</em> for this instance.
        /// The home page URL is then constructed out of the <see cref="GetHostName(boolean)"/> and the type of communication - secure or
        /// unsecure as specified in <see cref="SecurePort" /> and <see cref="NonSecurePort"/>
        ///
        /// It is normally used for informational purposes for other services to use it as a landing page.
        /// Configuration property: eureka:instance:homePageUrlPath
        /// </summary>
        string HomePageUrlPath { get; set; }

        /// <summary>
        /// Gets or sets gets the absolute home page URL for this instance. The users can provide the path if the home page resides in the
        /// same instance talking to eureka, else in the cases where the instance is a proxy for some other server, users can
        /// provide the full URL. If the full URL is provided it takes precedence.
        ///
        /// It is normally used for informational purposes for other services tofind about the status of this instance.
        /// Users can provide a simple<code>HTML</code> indicating what is the current status of the instance.
        /// The full URL should follow the format http://${eureka.hostname}:7001/ where the value ${eureka.hostname} is
        /// replaced at runtime.
        /// Configuration property: eureka:instance:homePageUrl
        /// </summary>
        string HomePageUrl { get; set; }

        /// <summary>
        /// Gets or sets gets the relative health check URL <em>Path</em> for this instance. The health check page URL
        /// is then constructed out of the <see cref="GetHostName(boolean)" /> and the type of communication - secure or
        /// unsecure as specified in <see cref="SecurePort"/> and <see cref="NonSecurePort"/>
        ///
        /// It is normally used for making educated decisions based on the health of the instance - for example, it can
        /// be used to determine whether to proceed deployments to an entire farm or stop the deployments without
        /// causing further damage.
        /// Configuration property: eureka:instance:healthCheckUrlPath
        /// </summary>
        string HealthCheckUrlPath { get; set; }

        /// <summary>
        /// Gets or sets gets the absolute health check page URL for this instance. The users can provide the path if the health
        /// check page resides in the same instance talking to eureka, else in the cases where the instance is a proxy
        /// for some other server, users can provide the full URL. If the full URL is provided it takes precedence.
        ///
        /// It is normally used for making educated decisions based on the health of the instance - for example, it can
        /// be used to determine whether to/ proceed deployments to an entire farm or stop the deployments without
        /// causing further damage.  The full URL should follow the format http://${eureka.hostname}:7001/ where the
        /// value ${eureka.hostname} is replaced at runtime.
        /// Configuration property: eureka:instance:healthCheckUrl
        /// </summary>
        string HealthCheckUrl { get; set; }

        /// <summary>
        /// Gets or sets gets the absolute secure health check page URL for this instance. The users can provide the path if the
        /// health check page resides in the same instance talking to eureka, else in the cases where the instance is a proxy
        /// for some other server, users can provide the full URL. If the full URL is provided it takes precedence.
        ///
        /// It is normally used for making educated decisions based on the health of the instance - for example, it can
        /// be used to determine whether to/ proceed deployments to an entire farm or stop the deployments without
        /// causing further damage.  The full URL should follow the format http://${eureka.hostname}:7001/ where the
        /// value ${eureka.hostname} is replaced at runtime.
        /// Configuration property: eureka:instance:secureHealthCheckUrl
        /// </summary>
        string SecureHealthCheckUrl { get; set; }

        /// <summary>
        /// Gets or sets an instance's network addresses should be fully expressed in it's <see cref="DataCenterInfo"/>
        /// For example for instances in AWS, this will include the publicHostname, publicIp,
        /// privateHostname and privateIp, when available. The <see cref="InstanceInfo" />
        /// will further express a "default address", which is a field that can be configured by the
        /// registering instance to advertise it's default address. This configuration allowed
        /// for the expression of an ordered list of fields that can be used to resolve the default
        /// address. The exact field values will depend on the implementation details of the corresponding
        /// implementing DataCenterInfo types.
        /// </summary>
        string[] DefaultAddressResolutionOrder { get; set; }

        /// <summary>
        /// Gets the hostname associated with this instance.
        /// This is the exact name that would be used by other instances to make calls.
        /// </summary>
        /// <param name="refresh">refresh hostname</param>
        /// <returns>hostname </returns>
        string GetHostName(bool refresh);

        string HostName { get; set; }

        bool PreferIpAddress { get; set; }
    }
}
