using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatAPI.Models
{
    public class Invitation
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Status { get; set; }

        public Invitation()
        {

        }
    }
}
