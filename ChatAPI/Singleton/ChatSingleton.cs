using ChatAPI.Models;
using Compressor.Structure;
using Encryptors.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatAPI.Singleton
{
    public class ChatSingleton
    {
        private static ChatSingleton instance = null;
        public CesarEncryption CesarEncryptor = new CesarEncryption();
        public SDES_Encryption SDESEncryptor = new SDES_Encryption();
        public DiffieHellman DiffiHell = new DiffieHellman();
        public Lzw lzw = new Lzw();
        public List<UserConnection> ConnectedUsers = new List<UserConnection>();

        public static ChatSingleton Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ChatSingleton();
                }
                return instance;
            }
        }
    }
}
