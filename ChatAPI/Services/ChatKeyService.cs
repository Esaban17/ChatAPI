using ChatAPI.Interface;
using ChatAPI.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatAPI.Services
{
    public class ChatKeyService
    {
        private readonly IMongoCollection<ChatKey> _chatKeys;

        public ChatKeyService(IChatDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            settings.ChatCollectionName = "ChatKeys";
            _chatKeys = database.GetCollection<ChatKey>(settings.ChatCollectionName);
        }

        public ChatKey GetChatKeys(string sender, string receiver)
        {
            return _chatKeys.Find(x => (x.Sender == sender && x.Receiver == receiver) || (x.Receiver == sender && x.Sender == receiver)).FirstOrDefault();
        }

        public async Task<ChatKey> CreateChatKeys(ChatKey newChatKeys)
        {
            await _chatKeys.InsertOneAsync(newChatKeys);
            return newChatKeys;
        }
    }
}
