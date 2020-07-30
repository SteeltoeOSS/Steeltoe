// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Discovery.Kubernetes.Discovery
{
    public class Input
    {
        private readonly int? _port;
        private readonly string _serviceName;
        private readonly IDictionary<string, string> _serviceLabels;
        private readonly IDictionary<string, string> _serviceAnnotations;

        public Input(string serviceName, int? port = null, IDictionary<string, string> serviceLabels = null, IDictionary<string, string> serviceAnnotations = null)
        {
            _port = port;
            _serviceName = serviceName;
            _serviceLabels = serviceLabels ?? new Dictionary<string, string>();
            _serviceAnnotations = serviceAnnotations ?? new Dictionary<string, string>();
        }

        public string GetServiceName()
        {
            return _serviceName;
        }

        public IDictionary<string, string> GetServiceLabels()
        {
            return _serviceLabels;
        }

        public IDictionary<string, string> GetServiceAnnotations()
        {
            return _serviceAnnotations;
        }

        public int? GetPort()
        {
            return _port;
        }
    }
}