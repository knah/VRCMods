using System.Linq;
using System.Text;

namespace LagFreeScreenshots
{
    public static class PngUtils
    {
        // https://stackoverflow.com/questions/24082305/how-is-png-crc-calculated-exactly
        private static readonly uint[] ourCRCTable = Enumerable.Range(0, 256).Select(n =>
        {
            uint c = (uint) n;
            for (var k = 0; k <= 7; k++)
            {
                if ((c & 1) == 1)
                    c = 0xEDB88320 ^ ((c >> 1) & 0x7FFFFFFF);
                else
                    c = ((c >> 1) & 0x7FFFFFFF);
            }

            return c;
        }).ToArray();

        private static uint PngCrc32(byte[] stream, int offset, int length, uint crc)
        {
            uint c = crc ^ 0xffffffff;
            var endOffset = offset + length;
            for (var i = offset; i < endOffset; i++)
            {
                c = ourCRCTable[(c ^ stream[i]) & 255] ^ ((c >> 8) & 0xFFFFFF);
            }

            return c ^ 0xffffffff;
        }

        internal static byte[] ProducePngDescriptionTextChunk(string text)
        {
            var keyword = "Description";
            var chunkDataSize = keyword.Length + 1 + 1 + 1 + 1 + 1 + Encoding.UTF8.GetByteCount(text);
            var chunkBytes = new byte[12 + chunkDataSize];
            chunkBytes[0] = (byte) (chunkDataSize >> 24);
            chunkBytes[1] = (byte) (chunkDataSize >> 16);
            chunkBytes[2] = (byte) (chunkDataSize >> 8);
            chunkBytes[3] = (byte) (chunkDataSize >> 0);

            chunkBytes[4] = (byte) 'i';
            chunkBytes[5] = (byte) 'T';
            chunkBytes[6] = (byte) 'X';
            chunkBytes[7] = (byte) 't';

            Encoding.UTF8.GetBytes(keyword, 0, keyword.Length, chunkBytes, 8);

            chunkBytes[8 + keyword.Length + 0] = 0; // null separator
            chunkBytes[8 + keyword.Length + 1] = 0; // compression flag
            chunkBytes[8 + keyword.Length + 2] = 0; // compression method
            chunkBytes[8 + keyword.Length + 3] = 0; // null separator
            chunkBytes[8 + keyword.Length + 4] = 0; // null separator

            Encoding.UTF8.GetBytes(text, 0, text.Length, chunkBytes, 8 + keyword.Length + 5);

            var crc = PngCrc32(chunkBytes, 4, chunkBytes.Length - 8, 0);

            chunkBytes[chunkBytes.Length - 4] = (byte) (crc >> 24);
            chunkBytes[chunkBytes.Length - 3] = (byte) (crc >> 16);
            chunkBytes[chunkBytes.Length - 2] = (byte) (crc >> 8);
            chunkBytes[chunkBytes.Length - 1] = (byte) (crc >> 0);

            return chunkBytes;
        }
    }
}