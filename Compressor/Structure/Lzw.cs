using Compressor.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Compressor.Structure
{
    public class Lzw : ICompressor
    {
        private static Lzw instance = null;
        public static int buffer = 20000000;
        public Dictionary<string, int> metaData = new Dictionary<string, int>();
        public Dictionary<string, int> TableAux = new Dictionary<string, int>();
        public Dictionary<string, int> Tabla = new Dictionary<string, int>();
        public Dictionary<int, string> TablaIndices = new Dictionary<int, string>();
        public List<int> Indices = new List<int>();
        public static int maxID = 0;

        public static Lzw Instance
        {
            get
            {
                if (instance == null)
                    instance = new Lzw();
                return instance;
            }
        }
        public bool Compress(string filePath, string compressPath, string[] fileName, out string RealFileName)
        {
            try
            {
                using (FileStream Fs = new FileStream(filePath, FileMode.Open))
                {
                    using BinaryReader Br = new BinaryReader(Fs);
                    var bytes = new byte[buffer];
                    int index = 1;
                    while (Br.BaseStream.Position != Br.BaseStream.Length)
                    {
                        bytes = Br.ReadBytes(buffer);
                        foreach (var item in bytes)
                        {
                            var value = Convert.ToString(item, 2).PadLeft(8, '0');
                            if (!Tabla.ContainsKey(value))
                            {
                                Tabla.Add(value, index);
                                TableAux.TryAdd(value, index);
                                metaData.TryAdd(Convert.ToString(item, 2).PadLeft(8, '0'), index);
                                index++;
                            }
                        }
                    }
                    Fs.Seek(0, SeekOrigin.Begin);
                    bytes = new byte[buffer];
                    while (Br.BaseStream.Position != Br.BaseStream.Length)
                    {
                        bytes = Br.ReadBytes(buffer);
                        ReadMaxValue(bytes, index);
                    }
                }
                int cantidadDeBitsMayor = Convert.ToString(maxID, 2).Length;
                RealFileName = WriteCompress(filePath, compressPath, fileName, cantidadDeBitsMayor);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                RealFileName = "";
                return false;
            }
        }
        private void ReadMaxValue(byte[] arregloBytes, int index)
        {
            var previous = string.Empty;
            var current = string.Empty;
            var combined = string.Empty;
            foreach (var item in arregloBytes)
            {
                current = Convert.ToString(item, 2).PadLeft(8, '0');
                combined = previous + current;
                if (TableAux.ContainsKey(combined))
                {
                    previous = combined;
                }
                else
                {
                    if (TableAux[previous] > maxID)
                    {
                        maxID = TableAux[previous];
                    }
                    TableAux.Add(combined, index++);
                    previous = current;
                }
            }
            if (TableAux[previous] > maxID)
            {
                maxID = TableAux[previous];
            }
        }
        private string WriteCompress(string filePath, string compressPath, string[] fileName, int maxBits)
        {
            try
            {
                string RealFileName = CheckFileName(compressPath, fileName[0], ".lzw");
                using var Fs = new FileStream(filePath, FileMode.Open);
                using BinaryReader Br = new BinaryReader(Fs);
                using FileStream writeStream = new FileStream($"{compressPath}/{RealFileName}.lzw", FileMode.OpenOrCreate);
                using BinaryWriter Bw = new BinaryWriter(writeStream);


                Bw.Write(fileName[0]);
                Bw.Write(fileName[1]);
                Bw.Write("--");

                //IMPRIME EL TAMAÑO DE BYTES NECESARIOS PARA LEER LA LONGITUD LA METADATA
                Bw.Write(Convert.ToByte(4));

                //IMPRIME EL TAMAÑO DE BYTES NECESARIOS PARA LEER LA LONGITUD MÁXIMA DE BITS
                Bw.Write(Convert.ToByte(4));

                //IMPRIME LA LONGITUD DE LA METADATA
                Bw.Write(GetBytesFromBinaryString(Convert.ToString(metaData.Count, 2).PadLeft(8 * 4, '0')));

                //IMPRIME LA LONGITUD DE MÁXIMA DE BITS
                Bw.Write(GetBytesFromBinaryString(Convert.ToString(maxBits, 2).PadLeft(8 * 4, '0')));

                foreach (var item in metaData)
                {
                    var decimalNumber = Convert.ToInt32(item.Key, 2);
                    var character = Convert.ToByte(decimalNumber);
                    Bw.Write(character);
                }

                var index = Tabla.Count() + 1;
                var byteBuffer = new byte[buffer];

                var tempCode = string.Empty;
                var previous = string.Empty;
                var current = string.Empty;
                var combined = string.Empty;
                while (Br.BaseStream.Position != Br.BaseStream.Length)
                {
                    byteBuffer = Br.ReadBytes(buffer);
                    foreach (var item in byteBuffer)
                    {
                        current = Convert.ToString(item, 2).PadLeft(8, '0');
                        combined = previous + current;
                        if (Tabla.ContainsKey(combined))
                        {
                            previous = combined;
                        }
                        else
                        {
                            tempCode += Convert.ToString(Tabla[previous], 2).PadLeft(maxBits, '0');
                            while (tempCode.Length >= 8)
                            {
                                var decimalNumber = Convert.ToInt32(tempCode.Substring(0, 8), 2);
                                var character = Convert.ToByte(decimalNumber);
                                Bw.Write(character);
                                tempCode = tempCode.Remove(0, 8);
                            }
                            Tabla.Add(combined, index++);
                            previous = current;
                        }
                    }
                }

                var output = string.Empty;
                if (Tabla.ContainsKey(previous))
                {
                    output = tempCode + Convert.ToString(Tabla[previous], 2).PadLeft(maxBits, '0');
                }
                else
                {
                    output = tempCode;
                }

                while (output != "")
                {
                    if (output.Length >= 8)
                    {
                        var decimalNumber = Convert.ToInt32(output.Substring(0, 8), 2);
                        var character = Convert.ToByte(decimalNumber);
                        Bw.Write(character);
                        output = output.Remove(0, 8);
                    }
                    else
                    {
                        var decimalNumber = Convert.ToInt32(output.PadRight(8, '0'), 2);
                        var character = Convert.ToByte(decimalNumber);
                        Bw.Write(character);
                        output = "";
                    }
                }

                metaData.Clear();
                Tabla.Clear();
                Indices.Clear();
                TableAux.Clear();
                return RealFileName + ".lzw";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return "";
            }
        }
        public bool Decompress(string compressPath, string decompressPath, out string RealFileName)
        {
            try
            {
                string originalName = ReadOriginalName(compressPath);
                var originalNameArr = originalName.Split('.');
                RealFileName = CheckFileName(decompressPath, originalNameArr[0], "." + originalNameArr[1]);
                ReadMetaData(compressPath);
                WriteDecompress(compressPath, decompressPath, RealFileName + "." + originalNameArr[1]);
                RealFileName += "." + originalNameArr[1];
                metaData.Clear();
                TableAux.Clear();
                TablaIndices.Clear();
                Indices.Clear();
                maxID = 0;
                return true;
            }
            catch (Exception)
            {
                RealFileName = "";
                return false;
            }
        }
        private void WriteDecompress(string compressPath, string decompressPath, string originalName)
        {
            using (var streamWriter = new FileStream($"{decompressPath}/{originalName}", FileMode.OpenOrCreate))
            {
                using var Bw = new BinaryWriter(streamWriter);
                int index = metaData.Count + 1;
                string salida = "";

                string S = "";
                string C = "";
                int nuevo;
                int viejo = Indices[0];
                salida += TablaIndices[viejo];
                Indices.RemoveAt(0);
                while (Indices.Count > 0)
                {
                    if (salida.Length >= 10 * 8)
                    {
                        Bw.Write(GetBytesFromBinaryString(salida));
                        salida = "";
                    }
                    nuevo = Indices[0];
                    Indices.RemoveAt(0);
                    if (nuevo > TablaIndices.Count)
                    {
                        S = TablaIndices[viejo];
                        C = S.Substring(0, 8);
                        S += C;
                    }
                    else
                        S = TablaIndices[nuevo];

                    C = S.Substring(0, 8);
                    salida += S;
                    TablaIndices.Add(index, TablaIndices[viejo] + C);
                    index++;
                    viejo = nuevo;
                }

                Bw.Write(GetBytesFromBinaryString(salida));
                salida = "";
                TablaIndices.Clear();
            }
        }
        private string ReadOriginalName(string compressPath)
        {
            var originalName = "";
            var extension = "";
            using (var stream = new FileStream(compressPath, FileMode.Open))
            {
                using var Br = new BinaryReader(stream);
                originalName = Br.ReadString();
                extension = Br.ReadString();
            }
            return originalName + "." + extension;
        }
        private void ReadMetaData(string compressPath)
        {
            using (FileStream Fs = new FileStream(compressPath, FileMode.Open))
            {
                using BinaryReader Br = new BinaryReader(Fs);
                var bytes = new byte[buffer];
                var endName = false;
                var lengthIndicators = true;
                var endMetadata = false;
                var counter = 0;
                var metaDataSize = 0;
                var index = 1;
                int NumeroDeBits = 0;
                while ((Br.BaseStream.Position != Br.BaseStream.Length))
                {
                    if (!endName)
                    {
                        var currentByte = Br.ReadString();
                        if (currentByte == "--")
                        {
                            endName = true;
                        }
                    }
                    else
                    {
                        if (lengthIndicators)
                        {
                            var MetaDataBytes = Br.ReadByte();
                            var NumeroMasGrandeBytes = Br.ReadByte();

                            var bytesMetaData = "";
                            for (int i = 0; i < MetaDataBytes; i++)
                            {
                                bytesMetaData += Convert.ToString(Br.ReadByte(), 2).PadLeft(8, '0');
                            }
                            metaDataSize = Convert.ToInt32(bytesMetaData, 2);

                            var bytesNumeroMasGrande = "";
                            for (int i = 0; i < NumeroMasGrandeBytes; i++)
                            {
                                bytesNumeroMasGrande += Convert.ToString(Br.ReadByte(), 2).PadLeft(8, '0');
                            }
                            NumeroDeBits = Convert.ToInt32(bytesNumeroMasGrande, 2);
                            lengthIndicators = false;
                        }
                        else
                        {
                            if (!endMetadata)
                            {
                                bytes = Br.ReadBytes(metaDataSize);
                                foreach (var item in bytes)
                                {
                                    counter++;
                                    if (counter <= metaDataSize)
                                    {
                                        if (metaData.TryAdd(Convert.ToString(item, 2).PadLeft(8, '0'), index))
                                        {
                                            TablaIndices.Add(index, Convert.ToString(item, 2).PadLeft(8, '0'));
                                            index++;
                                        }
                                    }
                                }
                                endMetadata = true;
                            }
                            else
                            {
                                string tempVal = "";
                                bytes = Br.ReadBytes(buffer);
                                foreach (var item in bytes)
                                {
                                    tempVal += Convert.ToString(item, 2).PadLeft(8, '0');
                                    if (tempVal.Length >= NumeroDeBits)
                                    {
                                        do
                                        {
                                            var val = Convert.ToInt32(tempVal.Substring(0, NumeroDeBits), 2);
                                            Indices.Add(val);
                                            tempVal = tempVal.Remove(0, NumeroDeBits);
                                        } while (tempVal.Length >= NumeroDeBits);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        private Byte[] GetBytesFromBinaryString(String binary)
        {
            var list = new List<Byte>();
            for (int i = 0; i < binary.Length; i += 8)
            {
                String t = binary.Substring(i, 8);

                list.Add(Convert.ToByte(t, 2));
            }
            return list.ToArray();
        }
        public string CheckFileName(string path, string fileName, string fileExt)
        {
            string newName = fileName;
            for (int i = 1; ; ++i)
            {
                if (!File.Exists(path + "/" + newName + fileExt))
                    return newName;
                newName = fileName + "(" + i + ")";
            }
        }

    }
}
