// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Steeltoe.Management.Endpoint.Mappings
{
    public class MappingDescription
    {
        public const string ALL_HTTP_METHODS = "GET || PUT || POST || DELETE || HEAD || OPTIONS";

        public MappingDescription(string routeHandler, IRouteDetails routeDetails)
        {
            if (routeHandler == null)
            {
                throw new ArgumentNullException(nameof(routeHandler));
            }

            if (routeDetails == null)
            {
                throw new ArgumentNullException(nameof(routeDetails));
            }

            Predicate = CreatePredicateString(routeDetails);
            Handler = routeHandler;
        }

        public MappingDescription(MethodInfo routeHandler, IRouteDetails routeDetails)
        {
            if (routeHandler == null)
            {
                throw new ArgumentNullException(nameof(routeHandler));
            }

            if (routeDetails == null)
            {
                throw new ArgumentNullException(nameof(routeDetails));
            }

            Predicate = CreatePredicateString(routeDetails);
            Handler = CreateHandlerString(routeHandler);
        }

        [JsonProperty("handler")]
        public string Handler { get; }

        [JsonProperty("predicate")]
        public string Predicate { get; }

        [JsonProperty("details")]
        public object Details { get; } // Always null for .NET

        private string CreateHandlerString(MethodInfo actionHandlerMethod)
        {
            return actionHandlerMethod.ToString();
        }

        private string CreatePredicateString(IRouteDetails routeDetails)
        {
            var sb = new StringBuilder("{");

            sb.Append("[" + routeDetails.RouteTemplate + "]");

            sb.Append(",methods=");
            sb.Append("[" + CreateRouteMethods(routeDetails.HttpMethods) + "]");

            if (!IsEmpty(routeDetails.Produces))
            {
                sb.Append(",produces=");
                sb.Append("[" + string.Join(" || ", routeDetails.Produces) + "]");
            }

            if (!IsEmpty(routeDetails.Consumes))
            {
                sb.Append(",consumes=");
                sb.Append("[" + string.Join(" || ", routeDetails.Consumes) + "]");
            }

            sb.Append("}");
            return sb.ToString();
        }

        private string CreateRouteMethods(IList<string> httpMethods)
        {
            if (IsEmpty(httpMethods))
            {
                return ALL_HTTP_METHODS;
            }

            return string.Join(" || ", httpMethods);
        }

        private bool IsEmpty(IList<string> list)
        {
            if (list == null || list.Count == 0)
            {
                return true;
            }

            return false;
        }
    }
}
