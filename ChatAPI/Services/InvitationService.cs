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
    public class InvitationService
    {
        private readonly IMongoCollection<Invitation> _invitations;

        public InvitationService(IChatDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            settings.ChatCollectionName = "Invitations";
            _invitations = database.GetCollection<Invitation>(settings.ChatCollectionName);
        }

        public async Task<Invitation> GetInvitation(string id)
        {
            var result = await _invitations.FindAsync(x => x.Id == id);
            return await result.FirstOrDefaultAsync();
        }

        public async Task<List<Invitation>> GetInvitations(string username)
        {
            var collection = await _invitations.FindAsync(x => (x.Receiver == username || x.Sender == username));
            return await collection.ToListAsync();
        }

        public Invitation CreateInvitation(Invitation newInvitation)
        {
            _invitations.InsertOne(newInvitation);
            return newInvitation;
        }

        public void UpdateInvitation(string id, Invitation invitation) => _invitations.ReplaceOne(x => x.Id == id, invitation);

        public void RemoveInvitation(string id)
        {
            _invitations.DeleteOne(invitation => invitation.Id == id);
        }
    }
}
