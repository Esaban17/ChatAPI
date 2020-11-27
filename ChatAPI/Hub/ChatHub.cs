using ChatAPI.Models;
using ChatAPI.Services;
using ChatAPI.Singleton;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatAPI.HubConfig
{
    public class ChatHub : Hub
    {
        public string Connect(string username)
        {
            string connectionId = Context.ConnectionId;

            if (ChatSingleton.Instance.ConnectedUsers.Count(x => x.ConnectionId == connectionId) == 0)
            {
                ChatSingleton.Instance.ConnectedUsers.Add(new UserConnection { ConnectionId = connectionId, Username = username});
            }
            return connectionId;
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var item = ChatSingleton.Instance.ConnectedUsers.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (item != null)
            {
                ChatSingleton.Instance.ConnectedUsers.Remove(item);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
