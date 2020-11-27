using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Encryptors.Structures
{
    public class SDES_Encryption
    {
        public static int buffer = 100000;
        char[] K1 = new char[8];
        char[] K2 = new char[8];
        public string[,] S0 = { { "01", "00", "11", "10" },//fila, columna
                                { "11", "10", "01", "00" },
                                { "00", "10", "01", "11" },
                                { "11", "01", "11", "10" } };

        public string[,] S1 = { { "00", "01", "10", "11" },
                                { "10", "00", "01", "11" },
                                { "11", "00", "01", "00" },
                                { "10", "01", "00", "11" } };
        public void GenerateKeys(char[] entry)
        {
            char[] P10Res = P10(entry);
            char[] LS1Res1 = new char[5];
            char[] LS1Res2 = new char[5];
            Array.Copy(P10Res, 0, LS1Res1, 0, 5);
            LS1Res1 = LS1(LS1Res1);
            Array.Copy(P10Res, 5, LS1Res2, 0, 5);
            LS1Res2 = LS1(LS1Res2);
            char[] Combined = new char[10];
            LS1Res1.CopyTo(Combined, 0);
            LS1Res2.CopyTo(Combined, 5);
            K1 = P8(Combined);
            LS1Res1 = LS2(LS1Res1);
            LS1Res2 = LS2(LS1Res2);
            LS1Res1.CopyTo(Combined, 0);
            LS1Res2.CopyTo(Combined, 5);
            K2 = P8(Combined);
        }

        public string Cipher(string entry, string key)
        {
            GenerateKeys(key.ToCharArray());
            byte[] entryBytes = Encoding.UTF8.GetBytes(entry);
            byte[] outputBytes = new byte[entryBytes.Length];
            int i = 0;
            foreach (var item in entryBytes)
            {
                var value = Convert.ToString(item, 2).PadLeft(8, '0');
                // FIRST ROUND
                char[] FrRes = FirstRound(value.ToCharArray(), K1);
                // SECOND ROUND
                char[] output = SecondRound(FrRes, K2);
                outputBytes[i] = Convert.ToByte(new string(output), 2);
                i++;
            }
            K1 = null;
            K2 = null;
            return Convert.ToBase64String(outputBytes);
        }
        public bool Cipher(string filePath, string[] fileName, string pathEncryption, string key, out string RealFileName)
        {
            try
            {
                GenerateKeys(key.ToCharArray());
                RealFileName = CheckFileName(pathEncryption, fileName[0], ".sdes");

                using (var Fs = new FileStream(filePath, FileMode.Open))
                {
                    using (var Br = new BinaryReader(Fs))
                    {
                        using (var Fs2 = new FileStream($"{pathEncryption}/{RealFileName}.sdes", FileMode.OpenOrCreate))
                        {
                            using (var Bw = new BinaryWriter(Fs2))
                            {
                                Bw.Write(fileName[0]);
                                Bw.Write(fileName[1]);
                                Bw.Write("--");
                                byte[] entryBytes = new byte[buffer];
                                while (Br.BaseStream.Position != Br.BaseStream.Length)
                                {
                                    entryBytes = Br.ReadBytes(buffer);

                                    foreach (var item in entryBytes)
                                    {
                                        var value = Convert.ToString(item, 2).PadLeft(8, '0');
                                        // FIRST ROUND
                                        char[] FrRes = FirstRound(value.ToCharArray(), K1);
                                        // SECOND ROUND
                                        char[] output = SecondRound(FrRes, K2);
                                        Bw.Write(Convert.ToByte(new string(output), 2));
                                    }
                                }
                                Bw.Close();
                            };
                            Fs2.Close();
                        };
                        Br.Close();
                    };
                    Fs.Close();
                };
                RealFileName += ".sdes";
                K1 = null;
                K2 = null;
                return true;
            }
            catch (Exception)
            {
                K1 = null;
                K2 = null;
                RealFileName = "";
                return true;
            }
        }
        public string Decipher(string entry, string key)
        {
            GenerateKeys(key.ToCharArray());
            byte[] entryBytes = Convert.FromBase64String(entry);
            byte[] outputBytes = new byte[entryBytes.Length];
            int i = 0;
            foreach (var item in entryBytes)
            {
                var value = Convert.ToString(item, 2).PadLeft(8, '0');
                // FIRST ROUND
                char[] FrRes = FirstRound(value.ToCharArray(), K2);
                // SECOND ROUND
                char[] output = SecondRound(FrRes, K1);
                outputBytes[i] = Convert.ToByte(new string(output), 2);
                i++;
            }
            K1 = null;
            K2 = null;
            return Encoding.UTF8.GetString(outputBytes);
        }
        public bool Decipher(string pathEncryption, string[] fileName, string pathDecrypt, string key, out string RealFileName)
        {
            try
            {
                GenerateKeys(key.ToCharArray());
                string[] originalName = ReadOriginalName(pathEncryption).Split('.');
                RealFileName = CheckFileName(pathDecrypt, fileName[0], "." + originalName[1]);
                bool endName = false;
                using (var Fs = new FileStream(pathEncryption, FileMode.Open))
                {
                    using (var Br = new BinaryReader(Fs))
                    {
                        using (var Fs2 = new FileStream($"{pathDecrypt}/{RealFileName}.{originalName[1]}", FileMode.OpenOrCreate))
                        {
                            using (var Bw = new BinaryWriter(Fs2))
                            {
                                byte[] entryBytes = new byte[buffer];
                                while (Br.BaseStream.Position != Br.BaseStream.Length)
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
                                        entryBytes = Br.ReadBytes(buffer);

                                        foreach (var item in entryBytes)
                                        {
                                            var value = Convert.ToString(item, 2).PadLeft(8, '0');
                                            // FIRST ROUND
                                            char[] FrRes = FirstRound(value.ToCharArray(), K2);
                                            // SECOND ROUND
                                            char[] output = SecondRound(FrRes, K1);
                                            Bw.Write(Convert.ToByte(new string(output), 2));
                                        }
                                    }
                                }
                                Bw.Close();
                            };
                            Fs2.Close();
                        };
                        Br.Close();
                    };
                    Fs.Close();
                };
                RealFileName += "." + originalName[1];
                K1 = null;
                K2 = null;
                return true;
            }
            catch (Exception)
            {
                K1 = null;
                K2 = null;
                RealFileName = "";
                return true;
            }
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
        private string ReadOriginalName(string path)
        {
            var originalName = "";
            var extension = "";
            using (var stream = new FileStream(path, FileMode.Open))
            {
                using var Br = new BinaryReader(stream);
                originalName = Br.ReadString();
                extension = Br.ReadString();
            }
            return originalName + "." + extension;
        }
        private char[] FirstRound(char[] entry, char[] key)
        {
            char[] InitialP = PI(entry);
            char[] Div1 = new char[4];
            char[] Div2 = new char[4];
            Array.Copy(InitialP, 0, Div1, 0, 4);
            Array.Copy(InitialP, 4, Div2, 0, 4);
            string XORRes = new string(XOR(EP(Div2), key));

            string OutputSBoxes = "";
            string f1 = "";
            f1 += XORRes[0];
            f1 += XORRes[3];
            string c1 = "";
            c1 += XORRes[1];
            c1 += XORRes[2];
            string f2 = "";
            f2 += XORRes[4];
            f2 += XORRes[7];
            string c2 = "";
            c2 += XORRes[5];
            c2 += XORRes[6];
            OutputSBoxes += S0[Convert.ToInt32(f1, 2), Convert.ToInt32(c1, 2)];
            OutputSBoxes += S1[Convert.ToInt32(f2, 2), Convert.ToInt32(c2, 2)];
            char[] XorRes2 = XOR(P4(OutputSBoxes.ToCharArray()), Div1);
            string output = new string(Div2);
            output += new string(XorRes2);
            return output.ToCharArray();
        }
        private char[] SecondRound(char[] entry, char[] key)
        {
            char[] Div1 = new char[4];
            char[] Div2 = new char[4];
            Array.Copy(entry, 0, Div1, 0, 4);
            Array.Copy(entry, 4, Div2, 0, 4);
            string XORRes = new string(XOR(EP(Div2), key));

            string OutputSBoxes = "";
            string f1 = "";
            f1 += XORRes[0];
            f1 += XORRes[3];
            string c1 = "";
            c1 += XORRes[1];
            c1 += XORRes[2];
            string f2 = "";
            f2 += XORRes[4];
            f2 += XORRes[7];
            string c2 = "";
            c2 += XORRes[5];
            c2 += XORRes[6];
            OutputSBoxes += S0[Convert.ToInt32(f1, 2), Convert.ToInt32(c1, 2)];
            OutputSBoxes += S1[Convert.ToInt32(f2, 2), Convert.ToInt32(c2, 2)];
            char[] XorRes2 = XOR(P4(OutputSBoxes.ToCharArray()), Div1);
            string Combined = new string(XorRes2);
            Combined += new string(Div2);
            char[] output = PI_I(Combined.ToCharArray());
            return output;
        }
        private char[] P10(char[] entry)
        {
            string output = "";
            output += entry[9];
            output += entry[7];
            output += entry[1];
            output += entry[0];
            output += entry[4];
            output += entry[8];
            output += entry[6];
            output += entry[2];
            output += entry[5];
            output += entry[3];
            return output.ToCharArray();
        }
        private char[] P8(char[] entry)
        {
            string output = "";
            output += entry[5];
            output += entry[1];
            output += entry[7];
            output += entry[4];
            output += entry[2];
            output += entry[9];
            output += entry[3];
            output += entry[0];
            return output.ToCharArray();
        }
        private char[] P4(char[] entry)
        {
            string output = "";
            output += entry[2];
            output += entry[0];
            output += entry[1];
            output += entry[3];
            return output.ToCharArray();
        }
        private char[] EP(char[] entry)
        {
            string output = "";
            output += entry[1];
            output += entry[0];
            output += entry[3];
            output += entry[2];
            output += entry[3];
            output += entry[1];
            output += entry[0];
            output += entry[2];
            return output.ToCharArray();
        }
        private char[] PI(char[] entry)
        {
            string output = "";
            output += entry[3];
            output += entry[7];
            output += entry[6];
            output += entry[0];
            output += entry[1];
            output += entry[4];
            output += entry[2];
            output += entry[5];
            return output.ToCharArray();
        }
        private char[] PI_I(char[] entry)
        {
            string output = "";
            output += entry[3];
            output += entry[4];
            output += entry[6];
            output += entry[0];
            output += entry[5];
            output += entry[7];
            output += entry[2];
            output += entry[1];
            return output.ToCharArray();
        }
        private char[] LS1(char[] entry)
        {
            char[] output = new char[entry.Length];
            char primero = entry[0];
            Array.Copy(entry, 1, output, 0, entry.Length - 1);
            output[entry.Length - 1] = primero;
            return output;
        }
        private char[] LS2(char[] entry)
        {
            char[] output = LS1(LS1(entry));
            return output;
        }
        private char[] XOR(char[] entry, char[] comparison)
        {
            string output = "";
            for (int i = 0; i < entry.Length; i++)
            {
                if (entry[i] != comparison[i])
                {
                    output += "1";
                }
                else
                {
                    output += "0";
                }
            }
            return output.ToCharArray();
        }
    }
}
