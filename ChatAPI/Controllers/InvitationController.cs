using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatAPI.Models;
using ChatAPI.Services;
using ChatAPI.Singleton;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatAPI.Controllers
{
    [ApiController]
    public class InvitationController : ControllerBase
    {
        private readonly InvitationService  _invitationService;
        private readonly ContactService _contactService;
        private readonly UserService _userService;
        private readonly ChatKeyService _chatKeyService;

        public InvitationController(InvitationService invitationService, ContactService contactService, UserService userService, ChatKeyService chatKeyService)
        {
            _invitationService = invitationService;
            _contactService = contactService;
            _userService = userService;
            _chatKeyService = chatKeyService;
        }

        // GET: api/invitation/5
        [Route("api/invitation/{id:length(24)}", Name = "GetInvitation")]
        [HttpGet]
        public async Task<ActionResult<Invitation>> GetInvitation(string id)
        {
            var invitation = await _invitationService.GetInvitation(id);

            if (invitation == null)
            {
                return NotFound();
            }

            return invitation;
        }

        // GET: api/invitation/{username}
        [Route("api/invitation/{username}")]
        [HttpGet]
        public async Task<ActionResult<List<Invitation>>> GetInvitations([FromRoute] string username)
        {
            return await _invitationService.GetInvitations(username);
        }

        // POST: api/invitation
        [Route("api/invitation")]
        [HttpPost]
        public async Task<ActionResult<Invitation>> CreateInvitation(Invitation newInvitation)
        {
            var invitations = await _invitationService.GetInvitations(newInvitation.Sender);

            if (!invitations.Exists(x => (x.Sender == newInvitation.Sender && x.Receiver == newInvitation.Receiver)))
            {
                _invitationService.CreateInvitation(newInvitation);
                return CreatedAtRoute("GetInvitation", new { id = newInvitation.Id.ToString() }, newInvitation);
            }
            return null;
        }

        // POST: api/invitation/action
        [Route("api/invitation/action")]
        [HttpPost]
        public async Task<ActionResult> AcceptOrDecline(Invitation newInvitation)
        {
            try
            {
                if (newInvitation.Status == "accepted")
                {
                    var Sender = _userService.GetUserByUsername(newInvitation.Sender);
                    var Receiver = _userService.GetUserByUsername(newInvitation.Receiver);

                    //GENERAMOS LA LLAVE PÚBLICA PARA CADA USUARIO
                    var senderPublicKey = ChatSingleton.Instance.DiffiHell.GeneratePublicKey(Sender.Code);
                    var receiverPublicKey = ChatSingleton.Instance.DiffiHell.GeneratePublicKey(Receiver.Code);

                    Sender.Password = null;
                    Sender.Code = 0;
                    Receiver.Password = null;
                    Receiver.Code = 0;

                    ChatKey chatKey = new ChatKey
                    {
                        Id = null,
                        Sender = newInvitation.Sender,
                        Receiver = newInvitation.Receiver,
                        KeySender = senderPublicKey,
                        KeyReceiver = receiverPublicKey
                    };

                    Contact userContact = new Contact
                    {
                        Id = null,
                        Owner = newInvitation.Receiver,
                        Friend = Sender
                    };

                    Contact senderContact = new Contact
                    {
                        Id = null,
                        Owner = newInvitation.Sender,
                        Friend = Receiver
                    };

                    await _contactService.CreateContact(userContact);
                    await _contactService.CreateContact(senderContact);
                    await _chatKeyService.CreateChatKeys(chatKey);
                }
                _invitationService.UpdateInvitation(newInvitation.Id, newInvitation);
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return BadRequest();
            }
        }

    }
}
