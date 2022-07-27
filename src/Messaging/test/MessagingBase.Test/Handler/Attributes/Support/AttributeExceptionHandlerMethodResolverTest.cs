// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net.Sockets;
using Xunit;

namespace Steeltoe.Messaging.Handler.Attributes.Support.Test;

public class AttributeExceptionHandlerMethodResolverTest
{
    private AttributeExceptionHandlerMethodResolver resolver = new (typeof(ExceptionController));

    [Fact]
    public void ResolveMethodFromAttribute()
    {
        var exception = new IOException();
        Assert.Equal("HandleIOException", resolver.ResolveMethod(exception).Name);
    }

    [Fact]
    public void ResolveMethodFromArgument()
    {
        var exception = new ArgumentException();
        Assert.Equal("HandleArgumentException", resolver.ResolveMethod(exception).Name);
    }

    [Fact]
    public void ResolveMethodExceptionSubType()
    {
        IOException ioException = new FileNotFoundException();
        Assert.Equal("HandleIOException", resolver.ResolveMethod(ioException).Name);
        SocketException bindException = new BindException();
        Assert.Equal("HandleSocketException", resolver.ResolveMethod(bindException).Name);
    }

    [Fact]
    public void ResolveMethodBestMatch()
    {
        var exception = new SocketException();
        Assert.Equal("HandleSocketException", resolver.ResolveMethod(exception).Name);
    }

    [Fact]
    public void ResolveMethodNoMatch()
    {
        var exception = new Exception();
        Assert.Null(resolver.ResolveMethod(exception)); // 1st lookup
        Assert.Null(resolver.ResolveMethod(exception)); // 2nd lookup from cache
    }

    [Fact]
    public void ResolveMethodInherited()
    {
        resolver = new AttributeExceptionHandlerMethodResolver(typeof(InheritedController));
        var exception = new IOException();
        Assert.Equal("HandleIOException", resolver.ResolveMethod(exception).Name);
    }

    [Fact]
    public void ResolveMethodAgainstCause()
    {
        var exception = new AggregateException(new IOException());
        Assert.Equal("HandleIOException", resolver.ResolveMethod(exception).Name);
    }

    [Fact]
    public void AmbiguousExceptionMapping()
    {
        Assert.Throws<InvalidOperationException>(() => new AttributeExceptionHandlerMethodResolver(typeof(AmbiguousController)));
    }

    [Fact]
    public void NoExceptionMapping()
    {
        Assert.Throws<InvalidOperationException>(() => new AttributeExceptionHandlerMethodResolver(typeof(NoExceptionController)));
    }

    internal class BindException : SocketException
    {
    }

    internal class ExceptionController
    {
        public virtual void Handle()
        {
        }

        [MessageExceptionHandler(typeof(IOException))]
        public virtual void HandleIOException()
        {
        }

        [MessageExceptionHandler(typeof(SocketException))]
        public virtual void HandleSocketException()
        {
        }

        [MessageExceptionHandler]
        public virtual void HandleArgumentException(ArgumentException exception)
        {
        }

        [MessageExceptionHandler]
        public virtual void HandleInvalidOperationException(InvalidOperationException exception)
        {
        }
    }

    internal class InheritedController : ExceptionController
    {
        public override void HandleIOException()
        {
        }
    }

    internal class AmbiguousController
    {
        public void Handle()
        {
        }

        [MessageExceptionHandler(typeof(BindException), typeof(ArgumentException))]
        public string Handle1(Exception ex)
        {
            return ex.GetType().Name;  // ClassUtils.getShortName(ex.getClass());
        }

        [MessageExceptionHandler]
        public string Handle2(ArgumentException ex)
        {
            return ex.GetType().Name; // ClassUtils.getShortName(ex.getClass());
        }
    }

    internal class NoExceptionController
    {
        [MessageExceptionHandler]
        public void Handle()
        {
        }
    }
}