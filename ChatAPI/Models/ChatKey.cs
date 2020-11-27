using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatAPI.Models
{
    public class ChatKey
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public int KeySender { get; set; }
        public int KeyReceiver { get; set; }

        public ChatKey()
        {
        }
    }
}
