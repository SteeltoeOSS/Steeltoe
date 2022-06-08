// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Steeltoe.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Steeltoe.Stream.Binder;

public static class EmbeddedHeaderUtils
{
    public static byte[] EmbedHeaders(MessageValues original, params string[] headers)
    {
        var headerValues = new byte[headers.Length][];
        var n = 0;
        var headerCount = 0;
        var headersLength = 0;
        foreach (var header in headers)
        {
            original.TryGetValue(header, out var value);
            if (value != null)
            {
                var json = JsonConvert.SerializeObject(value);
                headerValues[n] = Encoding.UTF8.GetBytes(json);
                headerCount++;
                headersLength += header.Length + headerValues[n++].Length;
            }
            else
            {
                headerValues[n++] = null;
            }
        }

        // 0xff, n(1), [ [lenHdr(1), hdr, lenValue(4), value] ... ]
        var byteBuffer = new MemoryStream();
        byteBuffer.WriteByte(0xff); // signal new format
        byteBuffer.WriteByte((byte)headerCount);
        for (var i = 0; i < headers.Length; i++)
        {
            if (headerValues[i] != null)
            {
                byteBuffer.WriteByte((byte)headers[i].Length);

                var buffer = Encoding.UTF8.GetBytes(headers[i]);
                byteBuffer.Write(buffer, 0, buffer.Length);

                buffer = GetBigEndianBytes(headerValues[i].Length);
                byteBuffer.Write(buffer, 0, buffer.Length);

                byteBuffer.Write(headerValues[i], 0, headerValues[i].Length);
            }
        }

        var payloadBuffer = (byte[])original.Payload;
        byteBuffer.Write(payloadBuffer, 0, payloadBuffer.Length);

        return byteBuffer.ToArray();
    }

    public static bool MayHaveEmbeddedHeaders(byte[] bytes)
    {
        return bytes.Length > 8 && (bytes[0] & 0xff) == 0xff;
    }

    public static MessageValues ExtractHeaders(byte[] payload)
    {
        return ExtractHeaders(payload, false, null);
    }

    public static MessageValues ExtractHeaders(IMessage<byte[]> message, bool copyRequestHeaders)
    {
        return ExtractHeaders(message.Payload, copyRequestHeaders, message.Headers);
    }

    public static string[] HeadersToEmbed(string[] configuredHeaders)
    {
        if (configuredHeaders == null || configuredHeaders.Length == 0)
        {
            return BinderHeaders.STANDARD_HEADERS;
        }
        else
        {
            var combinedHeadersToMap = new List<string>(BinderHeaders.STANDARD_HEADERS);
            combinedHeadersToMap.AddRange(configuredHeaders);
            return combinedHeadersToMap.ToArray();
        }
    }

    private static MessageValues ExtractHeaders(byte[] payload, bool copyRequestHeaders, IMessageHeaders requestHeaders)
    {
        var byteBuffer = new MemoryStream(payload);
        var headerCount = byteBuffer.ReadByte() & 0xff;
        if (headerCount == 0xff)
        {
            headerCount = byteBuffer.ReadByte() & 0xff;
            var headers = new Dictionary<string, object>();
            for (var i = 0; i < headerCount; i++)
            {
                var len = byteBuffer.ReadByte() & 0xff;
                var headerName = Encoding.UTF8.GetString(payload, (int)byteBuffer.Position, len);

                byteBuffer.Position += len;

                var intBytes = new byte[4];
                byteBuffer.Read(intBytes, 0, 4);
                len = GetIntFromBigEndianBytes(intBytes);
                var headerValue = Encoding.UTF8.GetString(payload, (int)byteBuffer.Position, len);
                var headerContent = JsonConvert.DeserializeObject(headerValue);

                headers.Add(headerName, headerContent);
                byteBuffer.Position += len;
            }

            var remaining = byteBuffer.Length - byteBuffer.Position;
            var newPayload = new byte[remaining];
            byteBuffer.Read(newPayload, 0, (int)remaining);
            return BuildMessageValues(newPayload, headers, copyRequestHeaders, requestHeaders);
        }
        else
        {
            return BuildMessageValues(payload, new Dictionary<string, object>(), copyRequestHeaders, requestHeaders);
        }
    }

    private static MessageValues BuildMessageValues(byte[] payload, Dictionary<string, object> headers, bool copyRequestHeaders, IMessageHeaders requestHeaders)
    {
        var messageValues = new MessageValues(payload, headers);
        if (copyRequestHeaders && requestHeaders != null)
        {
            messageValues.CopyHeadersIfAbsent(requestHeaders);
        }

        return messageValues;
    }

    private static byte[] GetBigEndianBytes(int value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return bytes;
    }

    private static int GetIntFromBigEndianBytes(byte[] bytes)
    {
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return BitConverter.ToInt32(bytes, 0);
    }
}
