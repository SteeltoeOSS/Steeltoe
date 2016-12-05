//
// Copyright 2015 the original author or authors.
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
//
#if NET451

using System;
using Steeltoe.CloudFoundry.Connector.Services;
using System.Reflection;
using System.Data.Entity;

namespace Steeltoe.CloudFoundry.Connector.MySql.EF6
{
    public class MySqlDbContextConnectorFactory : MySqlProviderConnectorFactory
    {
        private Type dbContextType;
        private ConstructorInfo constructor;

        internal MySqlDbContextConnectorFactory() 
        {

        }

        public MySqlDbContextConnectorFactory(MySqlServiceInfo info, MySqlProviderConnectorOptions config, Type dbContextType) :
            base(info, config)
        {
            if (dbContextType == null)
            {
                throw new ArgumentNullException(nameof(dbContextType));
            }
   
            this.dbContextType = dbContextType;
            this.constructor = FindConstructor(dbContextType);
            if (this.constructor == null)
            {
                throw new ConnectorException(string.Format("Missing 'public {0}(string connectionString)' constructor", dbContextType));
            }
        }

        public override object Create(IServiceProvider arg)
        {
            var connectionString = base.CreateConnectionString();
            if (connectionString != null)
                return CreateDbContext(connectionString);
            return null;
        }
        internal protected DbContext CreateDbContext(string connectString)
        {
            return (DbContext) this.constructor.Invoke(new object[] { connectString });
        }

        internal protected virtual ConstructorInfo FindConstructor(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            var declaredConstructors = typeInfo.DeclaredConstructors;

            foreach (ConstructorInfo ci in declaredConstructors)
            {
                var parameters = ci.GetParameters();
                if (parameters.Length == 1 && 
                    parameters[0].ParameterType == typeof(string) &&
                    ci.IsPublic && !ci.IsStatic)
                {
                    return ci;
                }
            }

            return null;
        }
    }
    
}
#endif