// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint
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
