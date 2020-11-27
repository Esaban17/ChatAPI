using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatAPI.Models;
using ChatAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatAPI.Controllers
{
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly ContactService _contactService;

        public ContactController(ContactService contactService)
        {
            _contactService = contactService;
        }

        // GET: api/contact/5
        [Route("api/contact/{id:length(24)}", Name = "GetContact")]
        [HttpGet]
        public async Task<ActionResult<Contact>> GetContact(string id)
        {
            var contact = await _contactService.GetContact(id);

            if (contact == null)
            {
                return NotFound();
            }

            return contact;
        }

        // GET: api/contact/{username}
        [Route("api/contact/{username}")]
        [HttpGet]
        public async Task<ActionResult<List<Contact>>> GetContacts([FromRoute] string username)
        {
            return await _contactService.GetContacts(username);
        }

        // POST: api/contact
        [Route("api/contact")]
        [HttpPost]
        public async Task<ActionResult<Contact>> CreateContact(Contact newContact)
        {
            var contacts = await _contactService.GetContacts(newContact.Owner);

            if (!contacts.Exists(x => x.Friend == newContact.Friend))
            {
                await _contactService.CreateContact(newContact);
                return CreatedAtRoute("GetContact", new { id = newContact.Id.ToString() }, newContact);
            }
            return null;
        }
    }
}
