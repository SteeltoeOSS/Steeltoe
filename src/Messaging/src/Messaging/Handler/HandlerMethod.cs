// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Steeltoe.Common;
using Steeltoe.Common.Util;

namespace Steeltoe.Messaging.Handler;

public class HandlerMethod
{
    // Keep these readonly for perf reasons
    protected readonly Invoker InnerInvoker;
    protected readonly int InnerArgCount;
    protected readonly object InnerHandler;

    protected internal Invoker HandlerInvoker => InnerInvoker;

    protected internal int ArgCount => InnerArgCount;

    public object Handler => InnerHandler;

    public MethodInfo Method { get; }

    public Type HandlerType { get; }

    public HandlerMethod ResolvedFromHandlerMethod { get; }

    public virtual bool IsVoid => Method.ReturnType.Equals(typeof(void));

    public virtual string ShortLogMessage
    {
        get
        {
            int args = Method.GetParameters().Length;
            return $"{HandlerType.Name}#{Method.Name}[{args} args]";
        }
    }

    public virtual ParameterInfo[] MethodParameters => Method.GetParameters();

    public virtual ParameterInfo ReturnType => Method.ReturnParameter;

    public HandlerMethod(object handler, MethodInfo handlerMethod)
    {
        ArgumentGuard.NotNull(handlerMethod);

        ArgumentGuard.NotNull(handler);

        InnerHandler = handler;
        HandlerType = handler.GetType();
        Method = handlerMethod;
        InnerArgCount = Method.GetParameters().Length;
        InnerInvoker = CreateInvoker();
    }

    public HandlerMethod(object handler, string handlerMethodName, params Type[] parameterTypes)
    {
        ArgumentGuard.NotNullOrEmpty(handlerMethodName);
        ArgumentGuard.NotNull(handler);

        InnerHandler = handler;
        HandlerType = handler.GetType();
        Method = HandlerType.GetMethod(handlerMethodName, parameterTypes);
        InnerArgCount = Method.GetParameters().Length;
        InnerInvoker = CreateInvoker();
    }

    protected HandlerMethod(HandlerMethod handlerMethod)
    {
        ArgumentGuard.NotNull(handlerMethod);

        InnerHandler = handlerMethod.Handler;
        HandlerType = handlerMethod.HandlerType;
        Method = handlerMethod.Method;
        InnerInvoker = handlerMethod.HandlerInvoker;
        InnerArgCount = handlerMethod.ArgCount;
    }

    private HandlerMethod(HandlerMethod handlerMethod, object handler)
    {
        ArgumentGuard.NotNull(handlerMethod);
        ArgumentGuard.NotNull(handler);

        InnerHandler = handler;
        HandlerType = handlerMethod.HandlerType;
        Method = handlerMethod.Method;
        InnerInvoker = handlerMethod.HandlerInvoker;
        InnerArgCount = handlerMethod.ArgCount;
        ResolvedFromHandlerMethod = handlerMethod;
    }

    public virtual HandlerMethod CreateWithResolvedBean()
    {
        return new HandlerMethod(this, Handler);
    }

    protected static object FindProvidedArgument(ParameterInfo parameter, params object[] providedArgs)
    {
        if (!ObjectUtils.IsNullOrEmpty(providedArgs))
        {
            foreach (object providedArg in providedArgs)
            {
                if (parameter.ParameterType.IsInstanceOfType(providedArg))
                {
                    return providedArg;
                }
            }
        }

        return null;
    }

    protected static string FormatArgumentError(ParameterInfo param, string message)
    {
        return $"Could not resolve parameter [{param.Position}] in {param.Member}{(!string.IsNullOrEmpty(message) ? $": {message}" : string.Empty)}";
    }

    protected virtual void AssertTargetBean(MethodInfo method, object targetBean, object[] args)
    {
        Type methodDeclaringClass = method.DeclaringType;
        Type targetBeanClass = targetBean.GetType();

        if (!methodDeclaringClass.IsAssignableFrom(targetBeanClass))
        {
            string text =
                $"The mapped handler method class '{methodDeclaringClass.Name}' is not an instance of the actual endpoint bean class '{targetBeanClass.Name}";

            throw new InvalidOperationException(FormatInvokeError(text, args));
        }
    }

    protected virtual string FormatInvokeError(string text, object[] args)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < args.Length; i++)
        {
            sb.Append(args[i] != null ? $"[{i}] [type={args[i].GetType().FullName}] [value={args[i]}]" : $"[{i}] [null]");

            sb.Append('\n');
        }

        return $"{text}\nEndpoint [{HandlerType.Name}]\nMethod [{Method}] with argument values:\n{sb}";
    }

    private Invoker CreateInvoker()
    {
        Type[] methodArgTypes = GetMethodParameterTypes();

        var dynamicMethod = new DynamicMethod($"{Method.Name}Invoker", typeof(object), new[]
        {
            typeof(object),
            typeof(object[])
        }, Method.DeclaringType.Module);

        ILGenerator generator = dynamicMethod.GetILGenerator(128);

        // Define some locals to store args into
        var argLocals = new LocalBuilder[methodArgTypes.Length];

        for (int i = 0; i < methodArgTypes.Length; i++)
        {
            argLocals[i] = generator.DeclareLocal(methodArgTypes[i]);
        }

        // Cast incoming arg0 to the target type
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Castclass, argLocals[0].LocalType);
        generator.Emit(OpCodes.Stloc, argLocals[0]);

        int arrayIndex = 0;

        for (int i = 1; i < methodArgTypes.Length; i++)
        {
            // Load argument from incoming array
            generator.Emit(OpCodes.Ldarg_1);

            if (arrayIndex <= 8)
            {
                generator.Emit(GetLoadIntConst(arrayIndex++));
            }
            else
            {
                generator.Emit(OpCodes.Ldc_I4, arrayIndex++);
            }

            generator.Emit(OpCodes.Ldelem_Ref);

            // Cast/Unbox if needed
            Type methodArgType = methodArgTypes[i];
            generator.Emit(IsValueType(methodArgType) ? OpCodes.Unbox_Any : OpCodes.Castclass, methodArgType);

            // Save to local
            generator.Emit(OpCodes.Stloc, argLocals[i]);
        }

        // Load all arg values from locals, including this
        foreach (LocalBuilder builder in argLocals)
        {
            generator.Emit(OpCodes.Ldloc, builder);
        }

        // Call target method
        generator.EmitCall(OpCodes.Callvirt, Method, null);

        // Handle return if any
        if (Method.ReturnType != typeof(void))
        {
            // Box any value types
            LocalBuilder returnLocal = generator.DeclareLocal(typeof(object));

            if (IsValueType(Method.ReturnType))
            {
                generator.Emit(OpCodes.Box, Method.ReturnType);
            }

            generator.Emit(OpCodes.Stloc, returnLocal);
            generator.Emit(OpCodes.Ldloc, returnLocal);
        }
        else
        {
            // Target method returns void, so return null
            generator.Emit(OpCodes.Ldnull);
        }

        // Return from invoker
        generator.Emit(OpCodes.Ret);

        // Create invoker delegate
        return (Invoker)dynamicMethod.CreateDelegate(typeof(Invoker));
    }

    private bool IsValueType(Type type)
    {
        return type.IsValueType;
    }

    private OpCode GetLoadIntConst(int constant)
    {
        return constant switch
        {
            0 => OpCodes.Ldc_I4_0,
            1 => OpCodes.Ldc_I4_1,
            2 => OpCodes.Ldc_I4_2,
            3 => OpCodes.Ldc_I4_3,
            4 => OpCodes.Ldc_I4_4,
            5 => OpCodes.Ldc_I4_5,
            6 => OpCodes.Ldc_I4_6,
            7 => OpCodes.Ldc_I4_7,
            8 => OpCodes.Ldc_I4_8,
            _ => throw new InvalidOperationException()
        };
    }

    private Type[] GetMethodParameterTypes()
    {
        ParameterInfo[] methodParameters = Method.GetParameters();
        var paramTypes = new Type[methodParameters.Length + 1];
        paramTypes[0] = Method.DeclaringType;

        for (int i = 0; i < methodParameters.Length; i++)
        {
            paramTypes[i + 1] = methodParameters[i].ParameterType;
        }

        return paramTypes;
    }

    public delegate object Invoker(object target, object[] args);
}
