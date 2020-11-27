using ChatAPI.Interface;
using ChatAPI.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatAPI.Services
{
    public class ContactService
    {
        private readonly IMongoCollection<Contact> _contacts;

        public ContactService(IChatDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            settings.ChatCollectionName = "Contacts";
            _contacts = database.GetCollection<Contact>(settings.ChatCollectionName);
        }

        public async Task<Contact> GetContact(string id)
        {
            var result = await _contacts.FindAsync(x => x.Id == id);
            return await result.FirstOrDefaultAsync();
        }

        public async Task<List<Contact>> GetContacts(string username)
        {
            var collection = await _contacts.FindAsync(x => x.Owner == username);
            return await collection.ToListAsync();
        }

        public async Task<List<Contact>> GetContactsWhereIdUser(string idUser)
        {
            var collection = await _contacts.FindAsync(x => x.Friend.Id == idUser);
            return await collection.ToListAsync();
        }

        public async Task<Contact> CreateContact(Contact newContact)
        {
            await _contacts.InsertOneAsync(newContact);
            return newContact;
        }

        public void UpdateContact(string id, Contact contactIn) => _contacts.ReplaceOne(x => x.Id == id, contactIn);

        public void RemoveContact(string id)
        {
            _contacts.DeleteOne(contact => contact.Id == id);
        }
    }
}
