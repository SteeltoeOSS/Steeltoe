// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Standard
{
    public class SpelCompiler
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<SpelCompiler> _logger;

        // private static readonly int _CLASSES_DEFINED_LIMIT = 100;

        // A compiler is created for each classloader, it manages a child class loader of that
        // classloader and the child is used to load the compiled expressions.
        // private static readonly Map<ClassLoader, SpelCompiler> compilers = new ConcurrentReferenceHashMap<>();

        // The child ClassLoader used to load the compiled expression classes
        // private ChildClassLoader ccl;

        // Counter suffix for generated classes within this SpelCompiler instance
        // private readonly AtomicInteger _suffixId = new AtomicInteger(1);
        public static SpelCompiler GetCompiler(ILoggerFactory loggerFactory = null)
        {
            return new SpelCompiler(loggerFactory);

            // ClassLoader clToUse = (classLoader != null ? classLoader : ClassUtils.getDefaultClassLoader());
            // synchronized(compilers) {
            //    SpelCompiler compiler = compilers.get(clToUse);
            //    if (compiler == null)
            //    {
            //        compiler = new SpelCompiler(clToUse);
            //        compilers.put(clToUse, compiler);
            //    }
            //    return compiler;
            // }
        }

        public static void RevertToInterpreted(IExpression expression)
        {
            if (expression is SpelExpression)
            {
                ((SpelExpression)expression).RevertToInterpreted();
            }
        }

        public static bool Compile(IExpression expression)
        {
            return expression is SpelExpression && ((SpelExpression)expression).CompileExpression();
        }

        public CompiledExpression Compile(ISpelNode expression)
        {
            if (expression.IsCompilable())
            {
                _logger?.LogDebug("SpEL: compiling {expression}", expression.ToStringAST());
                return CreateExpressionClass(expression);
            }

            _logger?.LogDebug("SpEL: unable to compile {expression} ", expression.ToStringAST());
            return null;
        }

        private SpelCompiler(ILoggerFactory loggerFactory = null)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory?.CreateLogger<SpelCompiler>();

            // this.ccl = new ChildClassLoader(classloader);
        }

        private CompiledExpression CreateExpressionClass(ISpelNode expressionToCompile)
        {
            throw new NotImplementedException();

            // // Create class outline 'spel/ExNNN extends org.springframework.expression.spel.CompiledExpression'
            //    String className = "spel/Ex" + getNextSuffix();
            //    ClassWriter cw = new ExpressionClassWriter();
            //    cw.visit(V1_5, ACC_PUBLIC, className, null, "org/springframework/expression/spel/CompiledExpression", null);

            // // Create default constructor
            //    MethodVisitor mv = cw.visitMethod(ACC_PUBLIC, "<init>", "()V", null, null);
            //    mv.visitCode();
            //    mv.visitVarInsn(ALOAD, 0);
            //    mv.visitMethodInsn(INVOKESPECIAL, "org/springframework/expression/spel/CompiledExpression",
            //            "<init>", "()V", false);
            //    mv.visitInsn(RETURN);
            //    mv.visitMaxs(1, 1);
            //    mv.visitEnd();

            // // Create getValue() method
            //    mv = cw.visitMethod(ACC_PUBLIC, "getValue",
            //            "(Ljava/lang/Object;Lorg/springframework/expression/EvaluationContext;)Ljava/lang/Object;", null,
            //            new String[] { "org/springframework/expression/EvaluationException" });
            //    mv.visitCode();

            // CodeFlow cf = new CodeFlow(className, cw);

            // // Ask the expression AST to generate the body of the method
            //    try
            //    {
            //        expressionToCompile.generateCode(mv, cf);
            //    }
            //    catch (IllegalStateException ex)
            //    {
            //        if (logger.isDebugEnabled())
            //        {
            //            logger.debug(expressionToCompile.getClass().getSimpleName() +
            //                    ".generateCode opted out of compilation: " + ex.getMessage());
            //        }
            //        return null;
            //    }

            // CodeFlow.insertBoxIfNecessary(mv, cf.lastDescriptor());
            //    if ("V".equals(cf.lastDescriptor()))
            //    {
            //        mv.visitInsn(ACONST_NULL);
            //    }
            //    mv.visitInsn(ARETURN);

            // mv.visitMaxs(0, 0);  // not supplied due to COMPUTE_MAXS
            //    mv.visitEnd();
            //    cw.visitEnd();

            // cf.finish();

            // byte[] data = cw.toByteArray();
            //    // TODO need to make this conditionally occur based on a debug flag
            //    // dump(expressionToCompile.toStringAST(), clazzName, data);
            //    return loadClass(StringUtils.replace(className, "/", "."), data);
        }

        // private Class<? extends CompiledExpression> LoadClass(String name, byte[] bytes)
        // {
        //    if (this.ccl.getClassesDefinedCount() > CLASSES_DEFINED_LIMIT)
        //    {
        //        this.ccl = new ChildClassLoader(this.ccl.getParent());
        //    }
        //    return (Class <? extends CompiledExpression >) this.ccl.defineClass(name, bytes);
        // }
    }
}
