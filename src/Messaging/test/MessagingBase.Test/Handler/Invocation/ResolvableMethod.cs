// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Steeltoe.Messaging.Handler.Attributes.Test.MessagingPredicates;

namespace Steeltoe.Messaging.Handler.Invocation.Test
{
    internal class ResolvableMethod
    {
        public ResolvableMethod(MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            this.Method = method;
        }

        public static Builder<T> On<T>()
        {
            return new Builder<T>();
        }

        public MethodInfo Method { get; }

        public ParameterInfo ReturnType
        {
            get { return Method.ReturnParameter; }
        }

        public ParameterInfo Arg(Type type)
        {
            return new ArgResolver(this).Arg(type);
        }

        public ArgResolver Annot(params IPredicate<ParameterInfo>[] filters)
        {
            return new ArgResolver(this, filters);
        }

        public ArgResolver AnnotPresent(params Type[] annotationTypes)
        {
            return new ArgResolver(this).AnnotPresent(annotationTypes);
        }

        public ArgResolver AnnotNotPresent(params Type[] annotationTypes)
        {
            return new ArgResolver(this).AnnotNotPresent(annotationTypes);
        }

        public override string ToString()
        {
            return $"ResolvableMethod={Method}";
        }

        internal class Builder<T>
        {
            private readonly Type objectClass;

            private readonly List<IPredicate<MethodInfo>> filters = new ();

            public Builder()
            {
                objectClass = typeof(T);
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
                    var paramTypes = method.GetParameters().Select((p) => p.ParameterType).ToArray();
                    if (paramTypes.Length != argTypes.Length)
                    {
                        return false;
                    }

                    for (var i = 0; i < argTypes.Length; i++)
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

            public Builder<T> Annot(params IPredicate<MethodInfo>[] filters)
            {
                this.filters.AddRange(filters);
                return this;
            }

            public Builder<T> AnnotPresent(params Type[] annotationTypes)
            {
                var message = $"annotationPresent={string.Join<Type>(",", annotationTypes)}";
                AddFilter(message, method =>
                {
                    foreach (var anno in annotationTypes)
                    {
                        if (method.GetCustomAttribute(anno) == null)
                        {
                            return false;
                        }
                    }

                    return true;
                });
                return this;
            }

            public Builder<T> AnnotNotPresent(params Type[] annotationTypes)
            {
                var message = $"annotationNotPresent={string.Join<Type>(",", annotationTypes)}";
                AddFilter(message, method =>
                {
                    if (annotationTypes.Length != 0)
                    {
                        foreach (var anno in annotationTypes)
                        {
                            if (method.GetCustomAttribute(anno) != null)
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                    else
                    {
                        return method.GetCustomAttributes().ToList().Count == 0;
                    }
                });
                return this;
            }

            public Builder<T> Returning(Type returnType)
            {
                var message = $"returnType={returnType}";
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
                var methods = objectClass.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static).Where(IsMatch).ToList();
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
            private void AddFilter(string message, IPredicate<MethodInfo> filter)
            {
                Func<MethodInfo, bool> func = filter.Test;
                filters.Add(new LabeledPredicate<MethodInfo>(message, func));
            }

            private void AddFilter(string message, Func<MethodInfo, bool> func)
            {
                filters.Add(new LabeledPredicate<MethodInfo>(message, func));
            }

            private bool IsMatch(MethodInfo method)
            {
                foreach (var predicate in filters)
                {
                    if (!predicate.Test(method))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        internal class ArgResolver
        {
            private readonly List<IPredicate<ParameterInfo>> filters = new ();

            private readonly ResolvableMethod resolvable;

            public ArgResolver(ResolvableMethod resolvable, params IPredicate<ParameterInfo>[] filters)
            {
                this.resolvable = resolvable ?? throw new ArgumentNullException(nameof(resolvable));
                this.filters.AddRange(filters);
            }

            public ArgResolver Annot(params IPredicate<ParameterInfo>[] filters)
            {
                this.filters.AddRange(filters);
                return this;
            }

            public ArgResolver AnnotPresent(params Type[] annotationTypes)
            {
                filters.Add(new FuncPredicate(param =>
                {
                    foreach (var attr in annotationTypes)
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

            public ArgResolver AnnotNotPresent(params Type[] annotationTypes)
            {
                filters.Add(new FuncPredicate(param =>
               {
                   if (annotationTypes.Length > 0)
                   {
                       foreach (var attr in annotationTypes)
                       {
                           if (param.GetCustomAttribute(attr) != null)
                           {
                               return false;
                           }
                       }

                       return true;
                   }
                   else
                   {
                       return param.GetCustomAttributes().ToList().Count == 0;
                   }
               }));

                return this;
            }

            public ParameterInfo Arg(Type type)
            {
                filters.Add(new FuncPredicate(param => param.ParameterType == type));

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
                var matches = ApplyFilters();
                if (matches.Count == 0)
                {
                    throw new InvalidOperationException($"No matching arg in method: {resolvable.Method}");
                }

                if (matches.Count > 1)
                {
                    throw new InvalidOperationException(
                        $"Multiple matching args in method: {resolvable.Method} {string.Join(",", matches)}");
                }

                return matches[0];
            }

            private List<ParameterInfo> ApplyFilters()
            {
                var matches = new List<ParameterInfo>();
                for (var i = 0; i < resolvable.Method.GetParameters().Length; i++)
                {
                    var param = resolvable.Method.GetParameters()[i];

                    // param.initParameterNameDiscovery(nameDiscoverer);
                    var allFiltersMatch = true;

                    foreach (var p in filters)
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

            internal class FuncPredicate : IPredicate<ParameterInfo>
            {
                private readonly Func<ParameterInfo, bool> func;

                public FuncPredicate(Func<ParameterInfo, bool> func)
                {
                    this.func = func ?? throw new ArgumentNullException(nameof(func));
                }

                public bool Test(ParameterInfo t)
                {
                    return func(t);
                }
            }
        }

        internal class LabeledPredicate<T> : IPredicate<T>
        {
            private readonly string label;

            private readonly Func<T, bool> del;

            public LabeledPredicate(string label, Func<T, bool> del)
            {
                this.label = label;
                this.del = del;
            }

            public bool Test(T t)
            {
                return del(t);
            }
        }
    }
}
