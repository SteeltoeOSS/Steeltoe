// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using System;

namespace Steeltoe.Stream.Binding
{
    public abstract class AbstractBindingTargetFactory<T> : IBindingTargetFactory
    {
        protected IApplicationContext _context;

        public Type BindingTargetType { get; }

        protected AbstractBindingTargetFactory(IApplicationContext context)
        {
            BindingTargetType = typeof(T);
            _context = context;
        }

        public virtual bool CanCreate(Type type)
        {
            return type.IsAssignableFrom(BindingTargetType);
        }

        public abstract T CreateInput(string name);

        public abstract T CreateOutput(string name);

        object IBindingTargetFactory.CreateInput(string name)
        {
            return CreateInput(name);
        }

        object IBindingTargetFactory.CreateOutput(string name)
        {
            return CreateOutput(name);
        }
    }
}
