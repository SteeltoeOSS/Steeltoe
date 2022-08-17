// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Castle.DynamicProxy;
using Steeltoe.Common;

namespace Steeltoe.Stream.Binding;

public class BindingProxyGenerator
{
    public static object CreateProxy(IBindableProxyFactory factory)
    {
        return GenerateProxy(factory);
    }

    internal static object GenerateProxy(IBindableProxyFactory factory)
    {
        ArgumentGuard.NotNull(factory);

        var generator = new ProxyGenerator();
        Func<MethodInfo, object> del = factory.Invoke;
        object proxy = generator.CreateInterfaceProxyWithoutTarget(factory.BindingType, new BindingInterceptor(del));
        return proxy;
    }

    private sealed class BindingInterceptor : IInterceptor
    {
        private readonly Delegate _impl;

        public BindingInterceptor(Delegate impl)
        {
            _impl = impl;
        }

        public void Intercept(IInvocation invocation)
        {
            object result = _impl.DynamicInvoke(invocation.Method);
            invocation.ReturnValue = result;
        }
    }
}
