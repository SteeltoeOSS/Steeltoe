// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using LockPrimitive =
#if NET10_0_OR_GREATER
    System.Threading.Lock
#else
    object
#endif
    ;

namespace Steeltoe.Management.Endpoint;

/// <summary>
/// A <see cref="TextWriter" /> that can safely receive concurrent writes from multiple threads. Memory dumps require this, see
/// https://github.com/dotnet/diagnostics/issues/6048.
/// </summary>
/// <remarks>
/// More efficient than <see cref="TextWriter.Synchronized" /> because this implementation only locks on low-level write operations. Also overrides the
/// async members, because the base class implements those by dispatching to the thread pool via <see cref="TaskFactory.StartNew(Action)" />, which is
/// wasteful for an in-memory buffer that never actually does anything asynchronous.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "This is only basic plumbing to satisfy downstream APIs.")]
internal sealed class ConcurrentTextWriter : TextWriter
{
    private readonly LockPrimitive _gate = new();
    private readonly StringBuilder _buffer = new();

    public override Encoding Encoding => Encoding.Unicode;

    public override void Write(char value)
    {
        lock (_gate)
        {
            _buffer.Append(value);
        }
    }

    public override void Write(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            lock (_gate)
            {
                _buffer.Append(value);
            }
        }
    }

    public override void Write(char[] buffer, int index, int count)
    {
        lock (_gate)
        {
            _buffer.Append(buffer, index, count);
        }
    }

    public override void Write(ReadOnlySpan<char> buffer)
    {
        lock (_gate)
        {
            _buffer.Append(buffer);
        }
    }

    public override void WriteLine()
    {
        lock (_gate)
        {
            _buffer.Append(CoreNewLine);
        }
    }

    public override void WriteLine(char value)
    {
        lock (_gate)
        {
            _buffer.Append(value).Append(CoreNewLine);
        }
    }

    public override void WriteLine(string? value)
    {
        lock (_gate)
        {
            _buffer.Append(value).Append(CoreNewLine);
        }
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        lock (_gate)
        {
            _buffer.Append(buffer, index, count).Append(CoreNewLine);
        }
    }

    public override void WriteLine(ReadOnlySpan<char> buffer)
    {
        lock (_gate)
        {
            _buffer.Append(buffer).Append(CoreNewLine);
        }
    }

    public override Task WriteAsync(char value)
    {
        Write(value);
        return Task.CompletedTask;
    }

    public override Task WriteAsync(string? value)
    {
        Write(value);
        return Task.CompletedTask;
    }

    public override Task WriteAsync(char[] buffer, int index, int count)
    {
        Write(buffer, index, count);
        return Task.CompletedTask;
    }

    public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
    {
        Write(buffer.Span);
        return Task.CompletedTask;
    }

    public override Task WriteLineAsync()
    {
        WriteLine();
        return Task.CompletedTask;
    }

    public override Task WriteLineAsync(char value)
    {
        WriteLine(value);
        return Task.CompletedTask;
    }

    public override Task WriteLineAsync(string? value)
    {
        WriteLine(value);
        return Task.CompletedTask;
    }

    public override Task WriteLineAsync(char[] buffer, int index, int count)
    {
        WriteLine(buffer, index, count);
        return Task.CompletedTask;
    }

    public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
    {
        WriteLine(buffer.Span);
        return Task.CompletedTask;
    }

    public override void Flush()
    {
    }

    public override Task FlushAsync()
    {
        return Task.CompletedTask;
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override string ToString()
    {
        lock (_gate)
        {
            return _buffer.ToString();
        }
    }
}
