using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Encryptors.Structures
{
    public class CesarEncryption
    {
        private static CesarEncryption instance = null;
        public List<char> LowerAlphabet = new List<char>();
        public List<char> UpperAlphabet = new List<char>();
        public Dictionary<char, char> EncodingAlphabet = new Dictionary<char, char>();
        public Dictionary<char, char> DecodingAlphabet = new Dictionary<char, char>();

        public static CesarEncryption Instance
        {
            get
            {
                if (instance == null)
                    instance = new CesarEncryption();
                return instance;
            }
        }
        public CesarEncryption()
        {
            GenerateAlphabet();
        }
        private void GenerateAlphabet()
        {
            for (int i = 65; i <= 90; i++)
            {
                UpperAlphabet.Add((char)i);
            }
            for (int i = 97; i <= 122; i++)
            {
                LowerAlphabet.Add((char)i);
            }
        }
        public string Encrypting(string password, string key)
        {
            try
            {
                var encryptedPassword = string.Empty;
                GenerateDictionary(key, true);
                foreach (var item in password)
                {
                    if (EncodingAlphabet.ContainsKey(item))
                    {
                        encryptedPassword += EncodingAlphabet[item];
                    }
                    else
                    {
                        encryptedPassword += item;
                    }
                }
                EncodingAlphabet.Clear();
                return encryptedPassword;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        private void GenerateDictionary(string key, bool encoding)
        {
            var upperEncoding = new List<char>();
            var lowerEncoding = new List<char>();
            lowerEncoding.AddRange(LowerAlphabet);
            upperEncoding.AddRange(UpperAlphabet);

            foreach (var item in key)
            {
                upperEncoding.Remove(char.ToUpper(item));
                lowerEncoding.Remove(char.ToLower(item));
            }
            upperEncoding.InsertRange(0, key.ToUpper().ToCharArray());
            lowerEncoding.InsertRange(0, key.ToLower().ToCharArray());

            var completeAlphabet = new List<char>();
            completeAlphabet.AddRange(upperEncoding);
            completeAlphabet.AddRange(lowerEncoding);

            if (encoding)
            {
                for (int i = 0; i < UpperAlphabet.Count; i++)
                {
                    EncodingAlphabet.Add(UpperAlphabet[i], completeAlphabet[i]);
                }
                for (int i = 0; i < LowerAlphabet.Count; i++)
                {
                    EncodingAlphabet.Add(LowerAlphabet[i], completeAlphabet[LowerAlphabet.Count + i]);
                }
            }
            else
            {
                for (int i = 0; i < UpperAlphabet.Count; i++)
                {
                    DecodingAlphabet.Add(completeAlphabet[i], UpperAlphabet[i]);
                }
                for (int i = 0; i < LowerAlphabet.Count; i++)
                {
                    DecodingAlphabet.Add(completeAlphabet[LowerAlphabet.Count + i], LowerAlphabet[i]);
                }
            }
        }

        public string Decrypting(string encryptedPassword, string key)
        {
            try
            {
                var password = string.Empty;
                GenerateDictionary(key, false);
                foreach (var item in encryptedPassword)
                {
                    if (DecodingAlphabet.ContainsKey(item))
                    {
                        password += DecodingAlphabet[item];
                    }
                    else
                    {
                        password += item;
                    }
                }
                DecodingAlphabet.Clear();
                return password;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
    }
}
