// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common;
using static Steeltoe.Messaging.Handler.Attributes.Test.MessagingPredicates;

namespace Steeltoe.Messaging.Handler.Invocation.Test;

internal sealed class ResolvableMethod
{
    public MethodInfo Method { get; }

    public ParameterInfo ReturnType => Method.ReturnParameter;

    public ResolvableMethod(MethodInfo method)
    {
        ArgumentGuard.NotNull(method);

        Method = method;
    }

    public static Builder<T> On<T>()
    {
        return new Builder<T>();
    }

    public ParameterInfo Arg(Type type)
    {
        return new ArgResolver(this).Arg(type);
    }

    public ArgResolver Annotation(params IPredicate<ParameterInfo>[] filters)
    {
        return new ArgResolver(this, filters);
    }

    public ArgResolver AnnotationPresent(params Type[] annotationTypes)
    {
        return new ArgResolver(this).AnnotationPresent(annotationTypes);
    }

    public ArgResolver AnnotationNotPresent(params Type[] annotationTypes)
    {
        return new ArgResolver(this).AnnotationNotPresent(annotationTypes);
    }

    public override string ToString()
    {
        return $"ResolvableMethod={Method}";
    }

    internal sealed class Builder<T>
    {
        private readonly Type _objectClass;

        private readonly List<IPredicate<MethodInfo>> _filters = new();

        public Builder()
        {
            _objectClass = typeof(T);
        }

        public Builder<T> Named(string methodName)
        {
            AddFilter($"methodName={methodName}", method => method.Name.Equals(methodName));
            return this;
        }

        public Builder<T> ArgTypes(params Type[] argTypes)
        {
            AddFilter($"argTypes={string.Join<Type>(",", argTypes)}", method =>
            {
                Type[] paramTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();

                if (paramTypes.Length != argTypes.Length)
                {
                    return false;
                }

                for (int i = 0; i < argTypes.Length; i++)
                {
                    if (argTypes[i] != paramTypes[i])
                    {
                        return false;
                    }
                }

                return true;
            });

            return this;
        }

        public Builder<T> Annotation(params IPredicate<MethodInfo>[] filters)
        {
            _filters.AddRange(filters);
            return this;
        }

        public Builder<T> AnnotationPresent(params Type[] annotationTypes)
        {
            string message = $"annotationPresent={string.Join<Type>(",", annotationTypes)}";

            AddFilter(message, method =>
            {
                foreach (Type type in annotationTypes)
                {
                    if (method.GetCustomAttribute(type) == null)
                    {
                        return false;
                    }
                }

                return true;
            });

            return this;
        }

        public Builder<T> AnnotationNotPresent(params Type[] annotationTypes)
        {
            string message = $"annotationNotPresent={string.Join<Type>(",", annotationTypes)}";

            AddFilter(message, method =>
            {
                if (annotationTypes.Length != 0)
                {
                    foreach (Type type in annotationTypes)
                    {
                        if (method.GetCustomAttribute(type) != null)
                        {
                            return false;
                        }
                    }

                    return true;
                }

                return method.GetCustomAttributes().ToList().Count == 0;
            });

            return this;
        }

        public Builder<T> Returning(Type returnType)
        {
            string message = $"returnType={returnType}";
            AddFilter(message, method => method.ReturnType == returnType);
            return this;
        }

        // public Builder<T> returning(Class<?> returnType, ResolvableType generic, ResolvableType... generics)
        // {
        //    return returning(toResolvableType(returnType, generic, generics));
        // }

        // public Builder<T> returning(ResolvableType returnType)
        // {
        //    String expected = returnType.toString();
        //    String message = "returnType=" + expected;
        //    addFilter(message, m->expected.equals(ResolvableType.forMethodReturnType(m).toString()));
        //    return this;
        // }
        public ResolvableMethod Build()
        {
            List<MethodInfo> methods = _objectClass.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(IsMatch).ToList();

            if (methods.Count == 0)
            {
                throw new InvalidOperationException($"No matching method: {this}");
            }

            if (methods.Count > 1)
            {
                throw new InvalidOperationException($"Multiple matching methods: {this}");
            }

            return new ResolvableMethod(methods[0]);
        }

        // private String formatMethods(Set<Method> methods)
        // {
        //    return "\nMatched:\n" + methods.stream()
        //            .map(Method::toGenericString).collect(joining(",\n\t", "[\n\t", "\n]"));
        // }

        // public ResolvableMethod mockCall(Consumer<T> invoker)
        // {
        //    MethodInvocationInterceptor interceptor = new MethodInvocationInterceptor();
        //    T proxy = initProxy(this.objectClass, interceptor);
        //    invoker.accept(proxy);
        //    Method method = interceptor.getInvokedMethod();
        //    return new ResolvableMethod(method);
        // }

        // Build & resolve shortcuts...
        public MethodInfo ResolveMethod()
        {
            return Build().Method;
        }

        public MethodInfo ResolveMethod(string methodName)
        {
            return Named(methodName).Build().Method;
        }

        public ParameterInfo ResolveReturnType()
        {
            return Build().ReturnType;
        }

        public ParameterInfo ResolveReturnType(Type returnType)
        {
            return Returning(returnType).Build().ReturnType;
        }

        // public MethodParameter ResolveReturnType(Type returnType, ResolvableType generic,
        //        ResolvableType... generics)
        // {

        // return returning(returnType, generic, generics).build().returnType();
        // }

        // public MethodParameter resolveReturnType(ResolvableType returnType)
        // {
        //    return returning(returnType).build().returnType();
        // }

        // public String toString()
        //        {
        //            return "ResolvableMethod.Builder[\n" +
        //                    "\tobjectClass = " + this.objectClass.getName() + ",\n" +
        //                    "\tfilters = " + formatFilters() + "\n]";
        //        }

        // private String formatFilters()
        //        {
        //            return this.filters.stream().map(Object::toString)
        //                    .collect(joining(",\n\t\t", "[\n\t\t", "\n\t]"));
        //        }
        private void AddFilter(string message, Func<MethodInfo, bool> func)
        {
            _filters.Add(new LabeledPredicate<MethodInfo>(message, func));
        }

        private bool IsMatch(MethodInfo method)
        {
            foreach (IPredicate<MethodInfo> predicate in _filters)
            {
                if (!predicate.Test(method))
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal sealed class ArgResolver
    {
        private readonly List<IPredicate<ParameterInfo>> _filters = new();

        private readonly ResolvableMethod _resolvable;

        public ArgResolver(ResolvableMethod resolvable, params IPredicate<ParameterInfo>[] filters)
        {
            _resolvable = resolvable ?? throw new ArgumentNullException(nameof(resolvable));
            _filters.AddRange(filters);
        }

        public ArgResolver Annotation(params IPredicate<ParameterInfo>[] filters)
        {
            _filters.AddRange(filters);
            return this;
        }

        public ArgResolver AnnotationPresent(params Type[] annotationTypes)
        {
            _filters.Add(new FuncPredicate(param =>
            {
                foreach (Type attr in annotationTypes)
                {
                    if (param.GetCustomAttribute(attr) == null)
                    {
                        return false;
                    }
                }

                return true;
            }));

            return this;
        }

        public ArgResolver AnnotationNotPresent(params Type[] annotationTypes)
        {
            _filters.Add(new FuncPredicate(param =>
            {
                if (annotationTypes.Length > 0)
                {
                    foreach (Type attr in annotationTypes)
                    {
                        if (param.GetCustomAttribute(attr) != null)
                        {
                            return false;
                        }
                    }

                    return true;
                }

                return param.GetCustomAttributes().ToList().Count == 0;
            }));

            return this;
        }

        public ParameterInfo Arg(Type type)
        {
            _filters.Add(new FuncPredicate(param => param.ParameterType == type));

            return Arg();
        }

        // public ParameterInfo Arg(Type type, ResolvableType generic, ResolvableType... generics)
        // {
        //    return arg(toResolvableType(type, generic, generics));
        // }

        // public MethodParameter Arg(ResolvableType type)
        // {
        //    this.filters.add(p->type.toString().equals(ResolvableType.forMethodParameter(p).toString()));
        //    return arg();
        // }
        public ParameterInfo Arg()
        {
            List<ParameterInfo> matches = ApplyFilters();

            if (matches.Count == 0)
            {
                throw new InvalidOperationException($"No matching arg in method: {_resolvable.Method}");
            }

            if (matches.Count > 1)
            {
                throw new InvalidOperationException($"Multiple matching args in method: {_resolvable.Method} {string.Join(",", matches)}");
            }

            return matches[0];
        }

        private List<ParameterInfo> ApplyFilters()
        {
            var matches = new List<ParameterInfo>();

            for (int i = 0; i < _resolvable.Method.GetParameters().Length; i++)
            {
                ParameterInfo param = _resolvable.Method.GetParameters()[i];

                // param.initParameterNameDiscovery(nameDiscoverer);
                bool allFiltersMatch = true;

                foreach (IPredicate<ParameterInfo> p in _filters)
                {
                    if (!p.Test(param))
                    {
                        allFiltersMatch = false;
                    }
                }

                if (allFiltersMatch)
                {
                    matches.Add(param);
                }
            }

            return matches;
        }

        internal sealed class FuncPredicate : IPredicate<ParameterInfo>
        {
            private readonly Func<ParameterInfo, bool> _func;

            public FuncPredicate(Func<ParameterInfo, bool> func)
            {
                _func = func ?? throw new ArgumentNullException(nameof(func));
            }

            public bool Test(ParameterInfo t)
            {
                return _func(t);
            }
        }
    }

    internal sealed class LabeledPredicate<T> : IPredicate<T>
    {
        private readonly string _label;

        private readonly Func<T, bool> _del;

        public LabeledPredicate(string label, Func<T, bool> del)
        {
            _label = label;
            _del = del;
        }

        public bool Test(T t)
        {
            return _del(t);
        }
    }
}
