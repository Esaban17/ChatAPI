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
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [Route("api/home")]
        // GET: api/encrypted
        [HttpGet]
        public IActionResult Home()
        {
            return new JsonResult(new
            {
                university = "Universidad Rafael Landívar",
                title = "PROYECTO FINAL - Estructura de Datos II",
                memberOne = "Erick Estuardo Sabán - 1195619",
                memberTwo = "Iván Alexander Canel - 1301019",
                memberThree = "Juan Sebastian Sánchez Rojas - 1023819",
                date = "27 de noviembre del 2020"
            });
        }

        // GET: api/User
        [Route("api/user")]
        [HttpGet]
        public ActionResult<List<User>> Get() => _userService.Get();

        // GET: api/User/5
        [Route("api/user/{id:length(24)}", Name = "GetUser")]
        [HttpGet]
        public ActionResult<User> Get(string id)
        {
            var user = _userService.Get(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // POST: api/User
        [Route("api/user")]
        [HttpPost]
        public ActionResult<User> Create(User user)
        {
            _userService.Create(user);

            return CreatedAtRoute("GetUser", new { id = user.Id.ToString() }, user);
        }


        // PUT: api/User/5
        [Route("api/user/{id:length(24)}")]
        [HttpPut]
        public IActionResult Update(string id, User userIn)
        {
            var book = _userService.Get(id);

            if (book == null)
            {
                return NotFound();
            }

            _userService.Update(id, userIn);

            return NoContent();
        }

        // DELETE: api/User/5
        [Route("api/user/{id:length(24)}")]
        [HttpDelete]
        public IActionResult Delete(string id)
        {
            var user = _userService.Get(id);

            if (user == null)
            {
                return NotFound();
            }

            _userService.Remove(user.Id);

            return NoContent();
        }
    }
}
