using ChatAPI.Interface;
using ChatAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatAPI.Services
{
    public class ChatService
    {
        private readonly IMongoCollection<Chat> _chats;

        public ChatService(IChatDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            settings.ChatCollectionName = "Chats";
            _chats = database.GetCollection<Chat>(settings.ChatCollectionName);
        }

        public async Task<List<Chat>> GetMessages(string sender, string receiver)
        {
            var collection = await _chats.FindAsync(x => (x.Sender == sender && x.Receiver == receiver) || (x.Receiver == sender && x.Sender == receiver));
            return await collection.ToListAsync();
        }

        public Chat GetChatById(string id) => _chats.Find(x => x.Id == id).FirstOrDefault();

        public Chat CreateMessage(Chat newMessage)
        {          
            _chats.InsertOne(newMessage);
            return newMessage;
        }

    }
}
