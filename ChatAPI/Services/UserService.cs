using ChatAPI.Interface;
using ChatAPI.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatAPI.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;

        public UserService(IChatDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _users = database.GetCollection<User>(settings.ChatCollectionName);
        }

        public User Get(string name) => _users.Find(user => user.Name == name).FirstOrDefault();
        public List<User> Get() => _users.Find(user => true).ToList();
        public User Create(User user)
        {
            _users.InsertOne(user);
            return user;
        }
        public void Update(string id, User userIn) => _users.ReplaceOne(user => user.Id == id, userIn);
        public void Remove(string id) => _users.DeleteOne(user => user.Id == id);

    }
}
