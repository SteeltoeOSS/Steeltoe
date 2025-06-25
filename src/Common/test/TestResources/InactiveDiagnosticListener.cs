// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Steeltoe.Common.TestResources;

/// <summary>
/// A no-op replacement for the built-in <see cref="DiagnosticListener" />.
/// </summary>
/// <para>
/// This type is registered by host builders used in tests. It prevents failure in DiagnosticObserverHttpExchangeRecorder when many tests are running.
/// </para>
internal sealed class InactiveDiagnosticListener()
    : DiagnosticListener("Empty")
{
    public override bool IsEnabled(string name)
    {
        return false;
    }

    public override bool IsEnabled(string name, object? arg1, object? arg2 = null)
    {
        return false;
    }

    public override IDisposable Subscribe(IObserver<KeyValuePair<string, object?>> observer)
    {
        return EmptyDisposable.Instance;
    }

    public override IDisposable Subscribe(IObserver<KeyValuePair<string, object?>> observer, Func<string, object?, object?, bool>? isEnabled)
    {
        return EmptyDisposable.Instance;
    }

    public override IDisposable Subscribe(IObserver<KeyValuePair<string, object?>> observer, Predicate<string>? isEnabled)
    {
        return EmptyDisposable.Instance;
    }

    // ReSharper disable once MethodOverloadWithOptionalParameter
    public override IDisposable Subscribe(IObserver<KeyValuePair<string, object?>> observer, Func<string, object?, object?, bool>? isEnabled,
        Action<Activity, object?>? onActivityImport = null, Action<Activity, object?>? onActivityExport = null)
    {
        return EmptyDisposable.Instance;
    }

    public override void Write(string name, object? value)
    {
    }

    public override void OnActivityExport(Activity activity, object? payload)
    {
    }

    public override void OnActivityImport(Activity activity, object? payload)
    {
    }
}
