// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Security.Authentication.Mtls;

internal static class LoggingExtensions
{
    private static readonly Action<ILogger, Exception> OnNoCertificate = LoggerMessage.Define(eventId: new EventId(0, "NoCertificate"),
        logLevel: LogLevel.Debug, formatString: "No client certificate found.");

    private static readonly Action<ILogger, string, string, Exception> OnCertificateRejected = LoggerMessage.Define<string, string>(
        eventId: new EventId(1, "CertificateRejected"), logLevel: LogLevel.Warning,
        formatString: "{CertificateType} certificate rejected, subject was {Subject}.");

    private static readonly Action<ILogger, string, string, Exception> OnCertificateFailedValidation = LoggerMessage.Define<string, string>(
        eventId: new EventId(2, "CertificateFailedValidation"), logLevel: LogLevel.Warning,
        formatString: $"Certificate validation failed, subject was {{Subject}}.{Environment.NewLine}{{ChainErrors}}");

    public static void NoCertificate(this ILogger logger)
    {
        OnNoCertificate(logger, null);
    }

    public static void CertificateRejected(this ILogger logger, string certificateType, string subject)
    {
        OnCertificateRejected(logger, certificateType, subject, null);
    }

    public static void CertificateFailedValidation(this ILogger logger, string subject, IEnumerable<string> chainedErrors)
    {
        OnCertificateFailedValidation(logger, subject, string.Join(Environment.NewLine, chainedErrors), null);
    }
}
