// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Logging.DynamicLogger.Test;

internal sealed class ConsoleOutputBorrower : IDisposable
{
    private readonly TextWriter _consoleWriter;
    private readonly StringBuilder _currentBuilder;

    public ConsoleOutputBorrower()
    {
        _consoleWriter = Console.Out;
        _currentBuilder = new StringBuilder();

        Console.SetOut(new StringWriter(_currentBuilder));
    }

    public void Clear()
    {
        _currentBuilder.Clear();
    }

    public override string ToString()
    {
        return _currentBuilder.ToString();
    }

    public void Dispose()
    {
        Console.SetOut(_consoleWriter);
    }
}
