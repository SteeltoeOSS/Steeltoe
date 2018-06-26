// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Steeltoe.Management.Endpoint.Mappings
{
    public class MappingDescription
    {
        private const string ALL_HTTP_METHODS = "GET || PUT || POST || DELETE || HEAD || OPTIONS";

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
            StringBuilder sb = new StringBuilder("{");

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
