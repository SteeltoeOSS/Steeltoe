// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Messaging.Handler
{
    public class HandlerMethod
    {
        // Keep these readonly for perf reasons
        protected readonly Invoker _invoker;
        protected readonly int _argCount;
        protected readonly object _handler;

        public delegate object Invoker(object target, object[] args);

        public object Handler
        {
            get { return _handler; }
        }

        public MethodInfo Method { get; }

        public Type HandlerType { get; }

        public HandlerMethod ResolvedFromHandlerMethod { get; }

        protected internal Invoker HandlerInvoker
        {
            get { return _invoker; }
        }

        protected internal int ArgCount
        {
            get { return _argCount;  }
        }

        public HandlerMethod(object handler, MethodInfo handlerMethod)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (handlerMethod == null)
            {
                throw new ArgumentNullException(nameof(handlerMethod));
            }

            _handler = handler;
            HandlerType = handler.GetType();
            Method = handlerMethod;
            _argCount = Method.GetParameters().Length;
            _invoker = CreateInvoker();
        }

        public HandlerMethod(object handler, string handlerMethodName, params Type[] parameterTypes)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (string.IsNullOrEmpty(nameof(handlerMethodName)))
            {
                throw new ArgumentNullException(nameof(handlerMethodName));
            }

            _handler = handler;
            HandlerType = handler.GetType();
            Method = HandlerType.GetMethod(handlerMethodName, parameterTypes);
            _argCount = Method.GetParameters().Length;
            _invoker = CreateInvoker();
        }

        private HandlerMethod(HandlerMethod handlerMethod, object handler)
        {
            if (handlerMethod == null)
            {
                throw new ArgumentNullException(nameof(handlerMethod));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _handler = handler;
            HandlerType = handlerMethod.HandlerType;
            Method = handlerMethod.Method;
            _invoker = handlerMethod.HandlerInvoker;
            _argCount = handlerMethod.ArgCount;
            ResolvedFromHandlerMethod = handlerMethod;
        }

        public virtual bool IsVoid
        {
            get
            {
                return Method.ReturnType.Equals(typeof(void));
            }
        }

        public virtual string ShortLogMessage
        {
            get
            {
                var args = Method.GetParameters().Length;
                return HandlerType.Name + "#" + Method.Name + "[" + args + " args]";
            }
        }

        public virtual ParameterInfo[] MethodParameters
        {
            get { return Method.GetParameters(); }
        }

        public virtual ParameterInfo ReturnType
        {
            get { return Method.ReturnParameter; }
        }

        public virtual HandlerMethod CreateWithResolvedBean()
        {
            return new HandlerMethod(this, Handler);
        }

        protected static object FindProvidedArgument(ParameterInfo parameter, params object[] providedArgs)
        {
            if (!ObjectUtils.IsEmpty(providedArgs))
            {
                foreach (var providedArg in providedArgs)
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
            return "Could not resolve parameter [" + param.Position + "] in " +
                    param.Member.ToString() + (!string.IsNullOrEmpty(message) ? ": " + message : string.Empty);
        }

        protected HandlerMethod(HandlerMethod handlerMethod)
        {
            if (handlerMethod == null)
            {
                throw new ArgumentNullException(nameof(handlerMethod));
            }

            _handler = handlerMethod.Handler;
            HandlerType = handlerMethod.HandlerType;
            Method = handlerMethod.Method;
            _invoker = handlerMethod.HandlerInvoker;
            _argCount = handlerMethod.ArgCount;
        }

        protected virtual void AssertTargetBean(MethodInfo method, object targetBean, object[] args)
        {
            var methodDeclaringClass = method.DeclaringType;
            var targetBeanClass = targetBean.GetType();
            if (!methodDeclaringClass.IsAssignableFrom(targetBeanClass))
            {
                var text = "The mapped handler method class '" + methodDeclaringClass.Name +
                        "' is not an instance of the actual endpoint bean class '" +
                        targetBeanClass.Name;
                throw new InvalidOperationException(FormatInvokeError(text, args));
            }
        }

        protected virtual string FormatInvokeError(string text, object[] args)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] != null)
                {
                    sb.Append("[" + i + "] [type=" + args[i].GetType().FullName + "] [value=" + args[i] + "]");
                }
                else
                {
                    sb.Append("[" + i + "] [null]");
                }

                sb.Append("\n");
            }

            return text + "\n" +
                     "Endpoint [" + HandlerType.Name + "]\n" +
                     "Method [" + Method.ToString() + "] " +
                     "with argument values:\n" + sb.ToString();
        }

        private Invoker CreateInvoker()
        {
            var methodArgTypes = GetMethodParameterTypes();

            var dynamicMethod = new DynamicMethod(
                Method.Name + "Invoker",
                typeof(object),
                new Type[] { typeof(object), typeof(object[]) },
                Method.DeclaringType.Module);

            var generator = dynamicMethod.GetILGenerator(128);

            // Define some locals to store args into
            var argLocals = new LocalBuilder[methodArgTypes.Length];
            for (var i = 0; i < methodArgTypes.Length; i++)
            {
                argLocals[i] = generator.DeclareLocal(methodArgTypes[i]);
            }

            // Cast incoming arg0 to the target type
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Castclass, argLocals[0].LocalType);
            generator.Emit(OpCodes.Stloc, argLocals[0]);

            var arrayIndex = 0;
            for (var i = 1; i < methodArgTypes.Length; i++)
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
                var methodArgType = methodArgTypes[i];
                if (IsValueType(methodArgType))
                {
                    generator.Emit(OpCodes.Unbox_Any, methodArgType);
                }
                else
                {
                    generator.Emit(OpCodes.Castclass, methodArgType);
                }

                // Save to local
                generator.Emit(OpCodes.Stloc, argLocals[i]);
            }

            // Load all arg values from locals, including this
            for (var i = 0; i < argLocals.Length; i++)
            {
                generator.Emit(OpCodes.Ldloc, argLocals[i]);
            }

            // Call target method
            generator.EmitCall(OpCodes.Callvirt, Method, null);

            // Handle return if any
            if (Method.ReturnType != typeof(void))
            {
                // Box any value types
                var returnLocal = generator.DeclareLocal(typeof(object));
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
                _ => throw new InvalidOperationException(),
            };
        }

        private Type[] GetMethodParameterTypes()
        {
            var methodParameters = Method.GetParameters();
            var paramTypes = new Type[methodParameters.Length + 1];
            paramTypes[0] = Handler.GetType();
            for (var i = 0; i < methodParameters.Length; i++)
            {
                paramTypes[i + 1] = methodParameters[i].ParameterType;
            }

            return paramTypes;
        }
    }
}
