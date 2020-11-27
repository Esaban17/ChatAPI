using System;
using System.Collections.Generic;
using System.Text;

namespace Compressor.Interface
{
    interface ICompressor
    {
        bool Compress(string filePath, string compressPath, string[] fileName, out string RealFileName);
        bool Decompress(string compressPath, string decompressPath, out string RealFileName);
    }
}
