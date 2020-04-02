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

using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Messaging.Rabbit.Config
{
    // TODO: AMQP class
    public abstract class AbstractDeclarable : IDeclarable
    {
        protected List<object> _declaringAdmins = new List<object>();

        protected AbstractDeclarable(Dictionary<string, object> arguments)
        {
            Declare = true;
            if (arguments != null)
            {
                Arguments = new Dictionary<string, object>(arguments);
            }
            else
            {
                Arguments = new Dictionary<string, object>();
            }
        }

        public bool Declare { get; set; }

        public Dictionary<string, object> Arguments { get; }

        public List<object> Admins
        {
            get
            {
                return _declaringAdmins;
            }
        }

        public bool IgnoreDeclarationExceptions { get; set; }

        public virtual void SetAdminsThatShouldDeclare(params object[] adminArgs)
        {
            var admins = new List<object>();
            if (adminArgs != null && adminArgs.Length > 0 && !(adminArgs.Length == 1 && adminArgs[0] == null))
            {
                admins.AddRange(adminArgs);
            }

            _declaringAdmins = admins.ToList();
        }

        public void AddArgument(string name, object value)
        {
            Arguments[name] = value;
        }

        public object RemoveArgument(string name)
        {
            Arguments.TryGetValue(name, out var result);
            return result;
        }
    }
}
