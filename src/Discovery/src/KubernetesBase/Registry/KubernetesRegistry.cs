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

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.KubernetesBase.Registry
{
    public class KubernetesRegistry : IServiceRegistry<IKubernetesRegistration>
    {

        public KubernetesRegistry()
        {
        }

        public void Dispose()
        {
            // empty: eventually log
        }

        public void Register(IKubernetesRegistration registration)
        {
            // empty: eventually log
        }

        public void Deregister(IKubernetesRegistration registration)
        {
            // empty: eventually log
        }

        public void SetStatus(IKubernetesRegistration registration, string status)
        {
            // empty: eventually log
        }

        public S GetStatus<S>(IKubernetesRegistration registration) where S : class
        {
            // empty: eventually log
            return null;
        }
    }
}