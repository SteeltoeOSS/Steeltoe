// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Integration.Util;

public class ErrorHandlingMessageHandlingRunnable : IMessageHandlingRunnable
{
    private readonly IMessageHandlingRunnable _runnable;
    private readonly IErrorHandler _errorHandler;

    public IMessage Message => _runnable.Message;

    public IMessageHandler MessageHandler => _runnable.MessageHandler;

    public ErrorHandlingMessageHandlingRunnable(IMessageHandlingRunnable runnable, IErrorHandler errorHandler)
    {
        _runnable = runnable ?? throw new ArgumentNullException(nameof(runnable));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
    }

    public bool Run()
    {
        try
        {
            return _runnable.Run();
        }
        catch (Exception e)
        {
            _errorHandler.HandleError(e);
            return false;
        }
    }
}

// IMessageHandlingRunnable
