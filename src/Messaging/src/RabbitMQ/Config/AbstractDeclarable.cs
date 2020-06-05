// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
