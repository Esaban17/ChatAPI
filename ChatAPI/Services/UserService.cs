using ChatAPI.Interface;
using ChatAPI.Models;
using ChatAPI.Singleton;
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
        private readonly string encryptionKey = "VIRUS";

        public UserService(IChatDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            settings.ChatCollectionName = "Users";
            _users = database.GetCollection<User>(settings.ChatCollectionName);
        }
        public User GetUserByUsername(string username) => _users.Find(x => x.Username == username).FirstOrDefault();
        public User GetUserById(string id) => _users.Find(x => x.Id == id).FirstOrDefault();
        public async Task<List<User>> GetUsers()
        {
            var collection = await _users.FindAsync(user => true);
            return await collection.ToListAsync();
        }
        public User CreateUser(User user)
        {
            //SE CIFRA LA PASSWORD CON CESAR
            var encryptedPassword = ChatSingleton.Instance.CesarEncryptor.Encrypting(user.Password, encryptionKey);
            user.Password = encryptedPassword;

            _users.InsertOne(user);
            return user;
        }
        public User Login(User user)
        {
            var userLogged = GetUserByUsername(user.Username);
            var encryptedPassword = ChatSingleton.Instance.CesarEncryptor.Encrypting(user.Password, encryptionKey);

            if (userLogged != null)
            {
                if (userLogged.Password == encryptedPassword)
                {
                    return userLogged;
                }
            }
            return null;
        }
        public void UpdateUser(string id, User userIn) => _users.ReplaceOne(x => x.Id == id, userIn);
        public void RemoveUser(string id) => _users.DeleteOne(user => user.Id == id);

    }
}
