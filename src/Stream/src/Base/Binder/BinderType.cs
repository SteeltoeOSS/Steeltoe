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

namespace Steeltoe.Stream.Binder
{
    public class BinderType : IBinderType
    {
        public BinderType(string name, string configurationClass, string assemblyPath)
        {
            Name = name;
            ConfigureClass = configurationClass;
            AssemblyPath = assemblyPath;
        }

        public string Name { get; }

        public string ConfigureClass { get; }

        public string AssemblyPath { get; }

        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }

            if (o == null || GetType() != o.GetType())
            {
                return false;
            }

            var that = (BinderType)o;
            if (!Name.Equals(that.Name))
            {
                return false;
            }

            return ConfigureClass == that.ConfigureClass &&
                AssemblyPath == that.AssemblyPath;
        }

        public override int GetHashCode()
        {
            var result = Name.GetHashCode();
            result = (31 * result) + ConfigureClass.GetHashCode();

            if (!string.IsNullOrEmpty(AssemblyPath))
            {
                result = (31 * result) + AssemblyPath.GetHashCode();
            }

            return result;
        }
    }
}
