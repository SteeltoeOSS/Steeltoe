// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointBase
{
    public static class Utils
    {
        /// <summary>
        /// Applies GZip compression to a file on disk, returns it as a stream and deletes the original file
        /// </summary>
        /// <param name="filename">Path of file to load</param>
        /// <param name="gzFilename">Name to use for compressed output</param>
        /// <param name="logger"><see cref="ILogger"/> for recording exceptions</param>
        /// <returns>A filestream with the file's compressed contents</returns>
        public static Stream CompressFile(string filename, string gzFilename, ILogger logger = null)
        {
            try
            {
                using (var input = new FileStream(filename, FileMode.Open))
                {
                    using var output = new FileStream(gzFilename, FileMode.CreateNew);
                    using var gzipStream = new GZipStream(output, CompressionLevel.Fastest);
                    input.CopyTo(gzipStream);
                }

                return new FileStream(gzFilename, FileMode.Open);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Unable to compress dump");
            }
            finally
            {
                File.Delete(filename);
            }

            return null;
        }

        /// <summary>
        /// Applies GZip compression to a file on disk, returns it as a stream and deletes the original file
        /// </summary>
        /// <param name="filename">Path of file to load</param>
        /// <param name="gzFilename">Name to use for compressed output</param>
        /// <param name="logger"><see cref="ILogger"/> for recording exceptions</param>
        /// <returns>A filestream with the file's compressed contents</returns>
        public static async Task<Stream> CompressFileAsync(string filename, string gzFilename, ILogger logger = null)
        {
            try
            {
                using (var input = new FileStream(filename, FileMode.Open))
                {
                    using var output = new FileStream(gzFilename, FileMode.CreateNew);
                    using var gzipStream = new GZipStream(output, CompressionLevel.Fastest);
                    await input.CopyToAsync(gzipStream).ConfigureAwait(false);
                }

                return new FileStream(gzFilename, FileMode.Open);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Unable to compress dump");
            }
            finally
            {
                File.Delete(filename);
            }

            return null;
        }
    }
}
