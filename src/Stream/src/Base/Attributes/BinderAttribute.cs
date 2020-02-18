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

namespace Steeltoe.Stream.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class BinderAttribute : Attribute
    {
        public BinderAttribute()
        {
            Name = string.Empty;
            ConfigureClass = string.Empty;
        }

        public BinderAttribute(string name, Type configureClass)
        {
            Name = name;
            ConfigureClass = configureClass.AssemblyQualifiedName;
        }

        public virtual string Name { get; set; }

        public virtual string ConfigureClass { get; set; }
    }
}
