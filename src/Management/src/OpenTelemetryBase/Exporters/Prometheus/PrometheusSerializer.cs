// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Steeltoe.Management.OpenTelemetry.Exporters.Prometheus;

/// <summary>
/// Basic PrometheusSerializer which has no OpenTelemetry dependency. Copied from OpenTelemetry.Net project.
/// </summary>
internal static class PrometheusSerializer
{
    private const byte AsciiQuotationMark = 0x22; // '"'
    private const byte AsciiFullStop = 0x2E; // '.'
    private const byte AsciiHyphenMinus = 0x2D; // '-'
    private const byte AsciiReverseSolidus = 0x5C; // '\\'
    public const byte AsciiLinefeed = 0x0A; // `\n`

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteDouble(byte[] buffer, int cursor, double value)
    {
        if (!double.IsInfinity(value) && !double.IsNaN(value))
        {
            cursor = WriteAsciiStringNoEscape(buffer, cursor, value.ToString(CultureInfo.InvariantCulture));
        }
        else if (double.IsPositiveInfinity(value))
        {
            cursor = WriteAsciiStringNoEscape(buffer, cursor, "+Inf");
        }
        else if (double.IsNegativeInfinity(value))
        {
            cursor = WriteAsciiStringNoEscape(buffer, cursor, "-Inf");
        }
        else
        {
            Debug.Assert(double.IsNaN(value), $"{nameof(value)} should be NaN.");
            cursor = WriteAsciiStringNoEscape(buffer, cursor, "Nan");
        }

        return cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteLong(byte[] buffer, int cursor, long value)
    {
        cursor = WriteAsciiStringNoEscape(buffer, cursor, value.ToString(CultureInfo.InvariantCulture));
        return cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteAsciiStringNoEscape(byte[] buffer, int cursor, string value)
    {
        foreach (char ch in value)
        {
            buffer[cursor++] = unchecked((byte)ch);
        }

        return cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteUnicodeNoEscape(byte[] buffer, int cursor, ushort ordinal)
    {
        if (ordinal <= 0x7F)
        {
            buffer[cursor++] = unchecked((byte)ordinal);
        }
        else if (ordinal <= 0x07FF)
        {
            buffer[cursor++] = unchecked((byte)(0b_1100_0000 | (ordinal >> 6)));
            buffer[cursor++] = unchecked((byte)(0b_1000_0000 | (ordinal & 0b_0011_1111)));
        }
        else if (ordinal <= 0xFFFF)
        {
            buffer[cursor++] = unchecked((byte)(0b_1110_0000 | (ordinal >> 12)));
            buffer[cursor++] = unchecked((byte)(0b_1000_0000 | ((ordinal >> 6) & 0b_0011_1111)));
            buffer[cursor++] = unchecked((byte)(0b_1000_0000 | (ordinal & 0b_0011_1111)));
        }
        else
        {
            Debug.Assert(ordinal <= 0xFFFF, ".NET string should not go beyond Unicode BMP.");
        }

        return cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteUnicodeString(byte[] buffer, int cursor, string value)
    {
        foreach (char ch in value)
        {
            ushort ordinal = (ushort)ch;

            switch (ordinal)
            {
                case AsciiReverseSolidus:
                    buffer[cursor++] = AsciiReverseSolidus;
                    buffer[cursor++] = AsciiReverseSolidus;
                    break;
                case AsciiLinefeed:
                    buffer[cursor++] = AsciiReverseSolidus;
                    buffer[cursor++] = unchecked((byte)'n');
                    break;
                default:
                    cursor = WriteUnicodeNoEscape(buffer, cursor, ordinal);
                    break;
            }
        }

        return cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteLabelKey(byte[] buffer, int cursor, string value)
    {
        Debug.Assert(!string.IsNullOrEmpty(value), $"{nameof(value)} should not be null or empty.");

        ushort ordinal = (ushort)value[0];

        if (ordinal >= '0' && ordinal <= '9')
        {
            buffer[cursor++] = unchecked((byte)'_');
        }

        foreach (char ch in value)
        {
            ordinal = ch;

            if ((ordinal >= 'A' && ordinal <= 'Z') || (ordinal >= 'a' && ordinal <= 'z') || (ordinal >= '0' && ordinal <= '9'))
            {
                buffer[cursor++] = unchecked((byte)ordinal);
            }
            else
            {
                buffer[cursor++] = unchecked((byte)'_');
            }
        }

        return cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteLabelValue(byte[] buffer, int cursor, string value)
    {
        Debug.Assert(value != null, $"{nameof(value)} should not be null.");

        foreach (char ch in value)
        {
            ushort ordinal = (ushort)ch;

            switch (ordinal)
            {
                case AsciiQuotationMark:
                    buffer[cursor++] = AsciiReverseSolidus;
                    buffer[cursor++] = AsciiQuotationMark;
                    break;
                case AsciiReverseSolidus:
                    buffer[cursor++] = AsciiReverseSolidus;
                    buffer[cursor++] = AsciiReverseSolidus;
                    break;
                case AsciiLinefeed:
                    buffer[cursor++] = AsciiReverseSolidus;
                    buffer[cursor++] = unchecked((byte)'n');
                    break;
                default:
                    cursor = WriteUnicodeNoEscape(buffer, cursor, ordinal);
                    break;
            }
        }

        return cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteLabel(byte[] buffer, int cursor, string labelKey, object labelValue)
    {
        cursor = WriteLabelKey(buffer, cursor, labelKey);
        buffer[cursor++] = unchecked((byte)'=');
        buffer[cursor++] = unchecked((byte)'"');

        // In Prometheus, a label with an empty label value is considered equivalent to a label that does not exist.
        cursor = WriteLabelValue(buffer, cursor, labelValue?.ToString() ?? string.Empty);
        buffer[cursor++] = unchecked((byte)'"');

        return cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteMetricName(byte[] buffer, int cursor, string metricName, string metricUnit = null)
    {
        Debug.Assert(!string.IsNullOrEmpty(metricName), $"{nameof(metricName)} should not be null or empty.");

        foreach (char ch in metricName)
        {
            ushort ordinal = (ushort)ch;

            switch (ordinal)
            {
                case AsciiFullStop:
                case AsciiHyphenMinus:
                    buffer[cursor++] = unchecked((byte)'_');
                    break;
                default:
                    buffer[cursor++] = unchecked((byte)ordinal);
                    break;
            }
        }

        if (!string.IsNullOrEmpty(metricUnit))
        {
            buffer[cursor++] = unchecked((byte)'_');

            foreach (char ch in metricUnit)
            {
                ushort ordinal = (ushort)ch;

                if ((ordinal >= 'A' && ordinal <= 'Z') || (ordinal >= 'a' && ordinal <= 'z') || (ordinal >= '0' && ordinal <= '9'))
                {
                    buffer[cursor++] = unchecked((byte)ordinal);
                }
                else
                {
                    buffer[cursor++] = unchecked((byte)'_');
                }
            }
        }

        return cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteHelpText(byte[] buffer, int cursor, string metricName, string metricUnit = null, string metricDescription = null)
    {
        cursor = WriteAsciiStringNoEscape(buffer, cursor, "# HELP ");
        cursor = WriteMetricName(buffer, cursor, metricName, metricUnit);

        if (!string.IsNullOrEmpty(metricDescription))
        {
            buffer[cursor++] = unchecked((byte)' ');
            cursor = WriteUnicodeString(buffer, cursor, metricDescription);
        }

        buffer[cursor++] = AsciiLinefeed;

        return cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteTypeInfo(byte[] buffer, int cursor, string metricName, string metricUnit, string metricType)
    {
        Debug.Assert(!string.IsNullOrEmpty(metricType), $"{nameof(metricType)} should not be null or empty.");

        cursor = WriteAsciiStringNoEscape(buffer, cursor, "# TYPE ");
        cursor = WriteMetricName(buffer, cursor, metricName, metricUnit);
        buffer[cursor++] = unchecked((byte)' ');
        cursor = WriteAsciiStringNoEscape(buffer, cursor, metricType);

        buffer[cursor++] = AsciiLinefeed;

        return cursor;
    }
}
