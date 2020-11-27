using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ChatAPI.Models;
using ChatAPI.Services;
using ChatAPI.Singleton;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatAPI.Controllers
{
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ContactService _contactService;
        private readonly IWebHostEnvironment _env;

        public UserController(UserService userService,ContactService contactService,IWebHostEnvironment env)
        {
            _userService = userService;
            _contactService = contactService;
            _env = env;
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
        public async Task<ActionResult<List<User>>> GetUsers()
        {
            return await _userService.GetUsers();
        }


        // GET: api/User/pepito
        [Route("api/user/username/{username}", Name = "GetUserByUsername")]
        [HttpGet]
        public ActionResult<User> GetUserByUsername(string username)
        {
            var user = _userService.GetUserByUsername(username);

            if (user == null)
            {
                return NotFound();
            }

            return new JsonResult(new
            {
                user
            });
        }

        // GET: api/User/5
        [Route("api/user/{id:length(24)}", Name = "GetUserById")]
        [HttpGet]
        public ActionResult<User> GetUserById(string id)
        {
            var user = _userService.GetUserById(id);

            if (user == null)
            {
                return NotFound();
            }

            return new JsonResult(new
            {
                user
            });
        }

        // POST: api/User
        [Route("api/user")]
        [HttpPost]
        public async Task<ActionResult<User>> Register(User user)
        {
            var users = await _userService.GetUsers();

            if (!users.Exists(x => x.Username == user.Username))
            {
                user.Code = user.GetHashCode();
                _userService.CreateUser(user);

                return CreatedAtRoute("GetUserById", new { id = user.Id.ToString() }, user);
            }
            return null;
        }

        // POST: api/User
        [Route("api/user/login")]
        [HttpPost]
        public ActionResult<User> Login(User user)
        {
            var userLogged = _userService.Login(user);

            return new JsonResult(new
            {
                user = userLogged
            });
        }

        // PUT: api/User/5
        [Route("api/user/{id:length(24)}")]
        [HttpPut]
        public IActionResult UpdateUser(string id, User userIn)
        {
            var user = _userService.GetUserById(id);

            if (user == null)
            {
                return NotFound();
            }

            _userService.UpdateUser(id, userIn);

            return NoContent();
        }

        // DELETE: api/User/5
        [Route("api/user/{id:length(24)}")]
        [HttpDelete]
        public IActionResult DeleteUser(string id)
        {
            var user = _userService.GetUserById(id);

            if (user == null)
            {
                return NotFound();
            }

            _userService.RemoveUser(user.Id);

            return NoContent();
        }

        // GET: api/user/image/{photo}
        [Route("api/user/image/{photo}")]
        [HttpGet]
        public IActionResult GetImage([FromRoute] string photo)
        {
            try
            {
                if (photo != null)
                {
                    var extension = Path.GetExtension(photo).Split('.')[1];
                    var folderPath = Path.Combine(_env.ContentRootPath, @"Resources\Images", photo);
                    FileStream stream = System.IO.File.OpenRead(folderPath);
                    return File(stream, $"image/{extension}");
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        // PUT: api/user/upload/photo/5
        [Route("api/user/upload/{id}")]
        [HttpPost, DisableRequestSizeLimit]
        public async Task<IActionResult> Upload([FromRoute] string id)
        {
            try
            {
                var user = _userService.GetUserById(id);

                if (user == null)
                {
                    return NotFound();
                }

                var file = Request.Form.Files[0];
                var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                var extension = Path.GetExtension(fileName);
                var folderPath = Path.Combine(_env.ContentRootPath, "Resources/Images");

                var successUpload = await UploadImage(file, folderPath, $"{user.Username}-{fileName}");
                if (successUpload)
                {
                    var contacts = await _contactService.GetContactsWhereIdUser(user.Id);

                    foreach (var item in contacts)
                    {
                        item.Friend.Photo = $"{user.Username}-{fileName}";
                        _contactService.UpdateContact(item.Id, item);
                    }

                    user.Photo = $"{user.Username}-{fileName}";
                    _userService.UpdateUser(id, user);
                    return Ok();
                }
                else
                {
                    return NoContent();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        private async Task<bool> UploadImage(IFormFile file, string originalPath, string fileName)
        {
            bool isSaveSuccess = false;
            try
            {
                if (!Directory.Exists(originalPath))
                {
                    Directory.CreateDirectory(originalPath);
                }
                using (var stream = new FileStream(Path.Combine(originalPath, fileName), FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                isSaveSuccess = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return isSaveSuccess;
        }

    }
}
