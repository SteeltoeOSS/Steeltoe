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

using Steeltoe.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public class Declarables : IServiceNameAware
    {
        public Declarables(string name, params IDeclarable[] declarables)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (declarables == null)
            {
                throw new ArgumentNullException(nameof(declarables));
            }

            DeclarableList = new List<IDeclarable>(declarables);
            ServiceName = name;
        }

        public Declarables(string name, List<IDeclarable> declarables)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (declarables == null)
            {
                throw new ArgumentNullException(nameof(declarables));
            }

            DeclarableList = new List<IDeclarable>(declarables);
            ServiceName = name;
        }

        public List<IDeclarable> DeclarableList { get; }

        public string ServiceName { get; set; }

        public IEnumerable<T> GetDeclarablesByType<T>()
        {
            return DeclarableList.OfType<T>();
        }
    }
}
