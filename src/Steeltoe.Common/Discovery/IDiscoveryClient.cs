using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Discovery.Client
{
    public interface IDiscoveryClient
    {
        /// <summary>
        /// A human readable description of the implementation
        /// </summary>
        string Description { get; }

        /// <summary>
        /// All known service Ids
        /// </summary>
        IList<string> Services { get; }

        /// <summary>
        ///  ServiceInstance with information used to register the local service
        /// </summary>
        /// <returns></returns>
        IServiceInstance GetLocalServiceInstance();

        /// <summary>
        ///  Get all ServiceInstances associated with a particular serviceId
        /// </summary>
        /// <param name="serviceId">the serviceId to lookup</param>
        /// <returns></returns>
        IList<IServiceInstance> GetInstances(String serviceId);


        Task ShutdownAsync();

    }
}
