using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Discovery.Client
{
    public interface IServiceInstance
    {
        /// <summary>
        ///  The service id as register by the DiscoveryClient
        /// </summary>
        string ServiceId { get; }
        /// <summary>
        /// The hostname of the registered ServiceInstance
        /// </summary>
        string Host { get; }
        /// <summary>
        /// The port of the registered ServiceInstance
        /// </summary>
        int Port { get; }
        /// <summary>
        /// If the port of the registered ServiceInstance is https or not
        /// </summary>
        bool IsSecure { get; }
        /// <summary>
        /// the service uri address
        /// </summary>
        Uri Uri { get; }
        /// <summary>
        ///  The key value pair metadata associated with the service instance
        /// </summary>
        IDictionary<string, string> Metadata { get; }
    }
}
