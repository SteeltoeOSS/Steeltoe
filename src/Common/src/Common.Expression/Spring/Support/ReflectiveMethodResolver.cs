// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

public class ReflectiveMethodResolver : IMethodResolver
{
    private readonly bool _useDistance;
    private Dictionary<Type, IMethodFilter> _filters;

    public ReflectiveMethodResolver()
    {
        _useDistance = true;
    }

    public ReflectiveMethodResolver(bool useDistance)
    {
        _useDistance = useDistance;
    }

    public virtual void RegisterMethodFilter(Type type, IMethodFilter filter)
    {
        _filters ??= new Dictionary<Type, IMethodFilter>();

        if (filter != null)
        {
            _filters[type] = filter;
        }
        else
        {
            _filters.Remove(type);
        }
    }

    public virtual IMethodExecutor Resolve(IEvaluationContext context, object targetObject, string name, IReadOnlyList<Type> argumentTypes)
    {
        try
        {
            ITypeConverter typeConverter = context.TypeConverter;
            Type type = targetObject as Type ?? targetObject.GetType();
            var methods = new List<MethodInfo>(GetMethods(type, targetObject));

            // If a filter is registered for this type, call it
            IMethodFilter filter = null;
            _filters?.TryGetValue(type, out filter);

            if (filter != null)
            {
                methods = filter.Filter(methods).ToList();
            }

            // Sort methods into a sensible order
            if (methods.Count > 1)
            {
                methods.Sort((m1, m2) =>
                {
                    int m1Pl = m1.GetParameters().Length;
                    int m2Pl = m2.GetParameters().Length;

                    // vararg methods go last
                    if (m1Pl == m2Pl)
                    {
                        if (!m1.IsVarArgs() && m2.IsVarArgs())
                        {
                            return -1;
                        }

                        if (m1.IsVarArgs() && !m2.IsVarArgs())
                        {
                            return 1;
                        }

                        return 0;
                    }

                    return m1Pl.CompareTo(m2Pl);
                });
            }

            // Remove duplicate methods (possible due to resolved bridge methods)
            var methodsToIterate = new HashSet<MethodInfo>(methods);

            MethodInfo closeMatch = null;
            int closeMatchDistance = int.MaxValue;
            MethodInfo matchRequiringConversion = null;
            bool multipleOptions = false;

            foreach (MethodInfo method in methodsToIterate)
            {
                if (method.Name.Equals(name))
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    int paramCount = parameters.Length;

                    var paramDescriptors = new List<Type>(paramCount);

                    for (int i = 0; i < paramCount; i++)
                    {
                        paramDescriptors.Add(parameters[i].ParameterType);
                    }

                    ArgumentsMatchInfo matchInfo = null;

                    if (method.IsVarArgs() && argumentTypes.Count >= paramCount - 1)
                    {
                        // *sigh* complicated
                        matchInfo = ReflectionHelper.CompareArgumentsVarargs(paramDescriptors, argumentTypes, typeConverter);
                    }
                    else if (paramCount == argumentTypes.Count)
                    {
                        // Name and parameter number match, check the arguments
                        matchInfo = ReflectionHelper.CompareArguments(paramDescriptors, argumentTypes, typeConverter);
                    }

                    if (matchInfo != null)
                    {
                        if (matchInfo.IsExactMatch)
                        {
                            return new ReflectiveMethodExecutor(method);
                        }

                        if (matchInfo.IsCloseMatch)
                        {
                            if (_useDistance)
                            {
                                int matchDistance = ReflectionHelper.GetTypeDifferenceWeight(paramDescriptors, argumentTypes);

                                if (closeMatch == null || matchDistance < closeMatchDistance)
                                {
                                    // This is a better match...
                                    closeMatch = method;
                                    closeMatchDistance = matchDistance;
                                }
                            }
                            else
                            {
                                // Take this as a close match if there isn't one already
                                if (closeMatch == null)
                                {
                                    closeMatch = method;
                                }
                            }
                        }
                        else if (matchInfo.IsMatchRequiringConversion)
                        {
                            if (matchRequiringConversion != null)
                            {
                                multipleOptions = true;
                            }

                            matchRequiringConversion = method;
                        }
                    }
                }
            }

            if (closeMatch != null)
            {
                return new ReflectiveMethodExecutor(closeMatch);
            }

            if (matchRequiringConversion != null)
            {
                if (multipleOptions)
                {
                    throw new SpelEvaluationException(SpelMessage.MultiplePossibleMethods, name);
                }

                return new ReflectiveMethodExecutor(matchRequiringConversion);
            }

            return null;
        }
        catch (EvaluationException ex)
        {
            throw new AccessException("Failed to resolve method", ex);
        }
    }

    protected virtual MethodInfo[] GetMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
    }

    protected virtual bool IsCandidateForInvocation(MethodInfo method, Type targetClass)
    {
        return true;
    }

    private ISet<MethodInfo> GetMethods(Type type, object targetObject)
    {
        if (targetObject is Type)
        {
            var result = new HashSet<MethodInfo>();

            // Add these so that static methods are invocable on the type: e.g. Float.valueOf(..)
            MethodInfo[] methods = GetMethods(type);

            foreach (MethodInfo method in methods)
            {
                if (method.IsStatic)
                {
                    result.Add(method);
                }
            }

            // Also expose methods from System.Type itself
            foreach (MethodInfo m in GetMethods(typeof(Type)))
            {
                result.Add(m);
            }

            return result;
        }
        else
        {
            var result = new HashSet<MethodInfo>();
            MethodInfo[] methods = GetMethods(type);

            foreach (MethodInfo method in methods)
            {
                if (IsCandidateForInvocation(method, type))
                {
                    result.Add(method);
                }
            }

            return result;
        }
    }
}
