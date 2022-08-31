// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Net;

namespace Steeltoe.Discovery.Eureka;

public class EurekaInstanceOptions : EurekaInstanceConfiguration, IDiscoveryRegistrationOptions
{
    public const string EurekaInstanceConfigurationPrefix = "eureka:instance";
    public new const string DefaultStatusPageUrlPath = "/info";
    public new const string DefaultHealthCheckUrlPath = "/health";

    private string _ipAddress;

    private string _hostName;

    // eureka:instance:appGroup
    public virtual string AppGroup
    {
        get => AppGroupName;
        set => AppGroupName = value;
    }

    // eureka:instance:instanceEnabledOnInit
    public virtual bool InstanceEnabledOnInit
    {
        get => IsInstanceEnabledOnInit;
        set => IsInstanceEnabledOnInit = value;
    }

    // eureka:instance:port
    public virtual int Port
    {
        get => NonSecurePort;
        set => NonSecurePort = value;
    }

    // eureka:instance:nonSecurePortEnabled
    public virtual bool NonSecurePortEnabled
    {
        get => IsNonSecurePortEnabled;
        set => IsNonSecurePortEnabled = value;
    }

    // eureka:instance:vipAddress
    public virtual string VipAddress
    {
        get => VirtualHostName;
        set => VirtualHostName = value;
    }

    // eureka:instance:secureVipAddress
    public virtual string SecureVipAddress
    {
        get => SecureVirtualHostName;
        set => SecureVirtualHostName = value;
    }

    // spring:cloud:discovery:registrationMethod changed to  eureka:instance:registrationMethod
    public virtual string RegistrationMethod { get; set; }

    public override string IpAddress
    {
        get => _ipAddress ?? thisHostAddress;
        set => _ipAddress = value;
    }

    public override string HostName
    {
        get => GetHostName(false);
        set
        {
            if (!value.Equals(base.HostName))
            {
                _hostName = value;
            }
        }
    }

    public EurekaInstanceOptions()
    {
        StatusPageUrlPath = DefaultStatusPageUrlPath;
        HealthCheckUrlPath = DefaultHealthCheckUrlPath;
        IsInstanceEnabledOnInit = true;
        VirtualHostName = null;
        SecureVirtualHostName = null;
        InstanceId = $"{GetHostName(false)}:{AppName}:{NonSecurePort}";
    }

    public override string GetHostName(bool refresh)
    {
        if (_hostName != null)
        {
            return _hostName;
        }

        if (refresh || string.IsNullOrEmpty(base.HostName))
        {
            base.HostName = DnsTools.ResolveHostName();
        }

        return base.HostName;
    }

    public void ApplyConfigUrls(List<Uri> addresses, string wildcardHostname)
    {
        // only use addresses from configuration if there are any and registration method hasn't been set
        // if registration method has been set, the user probably wants to define their own behavior
        if (addresses.Any() && string.IsNullOrEmpty(RegistrationMethod))
        {
            foreach (Uri address in addresses)
            {
                if (address.Scheme == "http" && Port == DefaultNonSecurePort)
                {
                    Port = address.Port;
                }
                else if (address.Scheme == "https" && SecurePort == DefaultSecurePort)
                {
                    SecurePort = address.Port;
                    SecurePortEnabled = true;
                    NonSecurePortEnabled = false;
                }

                if (!address.Host.Equals(wildcardHostname) && !address.Host.Equals("0.0.0.0"))
                {
                    HostName = address.Host;
                }
            }
        }
    }
}
