using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Encryptors.Structures
{
    public class DiffieHellman
    {
        private int prime = 1021;
        private int generator = 21;

        public int GeneratePublicKey(int secret)
        {
            secret %= 100000;
            BigInteger power = BigInteger.Pow(generator, secret);
            return (int)(power % new BigInteger(prime));
        }

        public int GenerateK(int otherPublicKey, int secret)
        {
            secret %= 100000;
            BigInteger power = BigInteger.Pow(otherPublicKey, secret);
            return (int)(power % new BigInteger(prime));
        }

        public string GenerateKeySDES(int K)
        {
            string binaryKey = Convert.ToString(K, 2);
            if (binaryKey.Length < 10)
            {
                return binaryKey.PadLeft(10, '0');
            }
            else
            {
                return binaryKey.Substring(0, 10);
            }
        }
    }
}
