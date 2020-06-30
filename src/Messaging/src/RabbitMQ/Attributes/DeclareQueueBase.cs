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

using System;

namespace Steeltoe.Messaging.Rabbit.Attributes
{
    public abstract class DeclareQueueBase : Attribute
    {
        public virtual string Durable { get; set; } = string.Empty;

        public virtual string Exclusive { get; set; } = string.Empty;

        public virtual string AutoDelete { get; set; } = string.Empty;

        public virtual string IgnoreDeclarationExceptions { get; set; } = "False";

        public virtual string Declare { get; set; } = "True";

        public virtual string Admin
        {
            get
            {
                if (Admins.Length == 0)
                {
                    return null;
                }

                return Admins[0];
            }

            set
            {
                Admins = new string[] { value };
            }
        }

        public virtual string[] Admins { get; set; } = new string[0];
    }
}
