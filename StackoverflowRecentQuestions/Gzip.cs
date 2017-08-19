using System.IO;
using System.IO.Compression;

namespace StackoverflowRecentQuestions
{
    public static class Gzip
    {
        public static byte[] Decompress(byte[] gzip)
        {
            using (var compressedMs = new MemoryStream(gzip))
            {
                using (var decompressedMs = new MemoryStream())
                {
                    using (var gzs = new BufferedStream(new GZipStream(compressedMs, CompressionMode.Decompress), 4096))
                    {
                        gzs.CopyTo(decompressedMs);
                    }
                    return decompressedMs.ToArray();
                }
            }
        }

        public static byte[] Compress(byte[] bytes)
        {
            using (var compressIntoMs = new MemoryStream())
            {
                using (var gzs = new BufferedStream(new GZipStream(compressIntoMs, CompressionMode.Compress), 4096))
                {
                    gzs.Write(bytes, 0, bytes.Length);
                }
                return compressIntoMs.ToArray();
            }
        }
    }
}
