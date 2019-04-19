using Steeltoe.Management.Census.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Steeltoe.Management.Census.Tags.Propagation
{
    [Obsolete("Use OpenCensus project packages")]
    internal static class SerializationUtils
    {
        internal const int VERSION_ID = 0;
        internal const int TAG_FIELD_ID = 0;
        // This size limit only applies to the bytes representing tag keys and values.
        internal const int TAGCONTEXT_SERIALIZED_SIZE_LIMIT = 8192;

        // Serializes a TagContext to the on-the-wire format.
        // Encoded tags are of the form: <version_id><encoded_tags>
        internal static byte[] SerializeBinary(ITagContext tags)
        {
            // Use a ByteArrayDataOutput to avoid needing to handle IOExceptions.
            //ByteArrayDataOutput byteArrayDataOutput = ByteStreams.newDataOutput();
            MemoryStream byteArrayDataOutput = new MemoryStream();

            byteArrayDataOutput.WriteByte(VERSION_ID);
            int totalChars = 0; // Here chars are equivalent to bytes, since we're using ascii chars.
            foreach(var tag in tags)
            {
                totalChars += tag.Key.Name.Length;
                totalChars += tag.Value.AsString.Length;
                EncodeTag(tag, byteArrayDataOutput);
            }
            //for (Iterator<Tag> i = InternalUtils.getTags(tags); i.hasNext();) {
            //    Tag tag = i.next();
            //    totalChars += tag.getKey().getName().length();
            //    totalChars += tag.getValue().asString().length();
            //    encodeTag(tag, byteArrayDataOutput);
            //}
            if (totalChars > TAGCONTEXT_SERIALIZED_SIZE_LIMIT) {
                throw new TagContextSerializationException(
                    "Size of TagContext exceeds the maximum serialized size "
                        + TAGCONTEXT_SERIALIZED_SIZE_LIMIT);
            }
            return byteArrayDataOutput.ToArray();
        }

        // Deserializes input to TagContext based on the binary format standard.
        // The encoded tags are of the form: <version_id><encoded_tags>
        internal static ITagContext DeserializeBinary(byte[] bytes)
        {
            try {
                if (bytes.Length == 0)
                {
                    // Does not allow empty byte array.
                    throw new TagContextDeserializationException("Input byte[] can not be empty.");
                }
                MemoryStream buffer = new MemoryStream(bytes);
                int versionId = buffer.ReadByte();
                if (versionId > VERSION_ID || versionId < 0)
                {
                    throw new TagContextDeserializationException(
                        "Wrong Version ID: " + versionId + ". Currently supports version up to: " + VERSION_ID);
                }
                return new TagContext(ParseTags(buffer));
            } catch (Exception exn) {
                throw new TagContextDeserializationException(exn.ToString()); // byte array format error.
            }
        }

        internal static IDictionary<ITagKey, ITagValue> ParseTags(MemoryStream buffer)

        {
            IDictionary<ITagKey, ITagValue> tags = new Dictionary<ITagKey, ITagValue>();
            long limit = buffer.Length;
            int totalChars = 0; // Here chars are equivalent to bytes, since we're using ascii chars.
            while (buffer.Position < limit) {
                int type = buffer.ReadByte();
                if (type == TAG_FIELD_ID) {
                    ITagKey key = CreateTagKey(DecodeString(buffer));
                    ITagValue val = CreateTagValue(key, DecodeString(buffer));
                    totalChars += key.Name.Length;
                    totalChars += val.AsString.Length;
                    tags[key] =  val;
                } else {
                    // Stop parsing at the first unknown field ID, since there is no way to know its length.
                    // TODO(sebright): Consider storing the rest of the byte array in the TagContext.
                    break;
                }
            }
            if (totalChars > TAGCONTEXT_SERIALIZED_SIZE_LIMIT) {
                throw new TagContextDeserializationException(
                    "Size of TagContext exceeds the maximum serialized size "
                        + TAGCONTEXT_SERIALIZED_SIZE_LIMIT);
            }
            return tags;
        }

        // TODO(sebright): Consider exposing a TagKey name validation method to avoid needing to catch an
        // IllegalArgumentException here.
        private static ITagKey CreateTagKey(String name)
        {
            try {
                return TagKey.Create(name);
            } catch (Exception e) {
                throw new TagContextDeserializationException("Invalid tag key: " + name, e);
            }
        }

        // TODO(sebright): Consider exposing a TagValue validation method to avoid needing to catch
        // an IllegalArgumentException here.
        private static ITagValue CreateTagValue(ITagKey key, String value)
        {
            try {
                return TagValue.Create(value);
            } catch (Exception e) {
                throw new TagContextDeserializationException(
                    "Invalid tag value for key " + key + ": " + value, e);
            }
        }

        private static void EncodeTag(ITag tag, MemoryStream byteArrayDataOutput)
        {
            byteArrayDataOutput.WriteByte(TAG_FIELD_ID);
            EncodeString(tag.Key.Name, byteArrayDataOutput);
            EncodeString(tag.Value.AsString, byteArrayDataOutput);
        }

        private static void EncodeString(String input, MemoryStream byteArrayDataOutput)
        {
            PutVarInt(input.Length, byteArrayDataOutput);
            var bytes = Encoding.UTF8.GetBytes(input);
            byteArrayDataOutput.Write(bytes, 0, bytes.Length);
        }

        private static void PutVarInt(int input, MemoryStream byteArrayDataOutput)
        {
            byte[] output = new byte[VarInt.VarIntSize(input)];
            VarInt.PutVarInt(input, output, 0);
            byteArrayDataOutput.Write(output, 0, output.Length);
        }

        private static String DecodeString(MemoryStream buffer)
        {
            int length = VarInt.GetVarInt(buffer);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                builder.Append((char)buffer.ReadByte());
            }
            return builder.ToString();
        }
    }
}
