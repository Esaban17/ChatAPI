using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChatAPI.HubConfig;
using ChatAPI.Interface;
using ChatAPI.Models;
using ChatAPI.Services;
using ChatAPI.Singleton;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace ChatAPI.Controllers
{
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly ChatService _chatService;
        private readonly UserService _userService;
        private readonly ChatKeyService _chatKeyService;

        public ChatController(IHubContext<ChatHub> hubContext, ChatService chatService, UserService userService, ChatKeyService chatKeyService, IWebHostEnvironment env)
        {
            _chatHub = hubContext;
            _chatService = chatService;
            _userService = userService;
            _chatKeyService = chatKeyService;
            _env = env;
        }

        // GET: api/chat
        [Route("api/chat/{sender}/{receiver}")]
        [HttpGet]
        public async Task<ActionResult<List<Chat>>> GetMessages([FromRoute] string sender, [FromRoute] string receiver, [FromQuery] string senderCode)
        {
            //SE OBTIENEN LOS MENSAJES CIFRADOS DE MONGODB
            var encryptedMessages = await _chatService.GetMessages(sender, receiver);

            //OBTENEMOS LAS LLAVES PÚBLICAS DEL CHAT
            var chatKeys = _chatKeyService.GetChatKeys(sender, receiver);

            //GENERAMOS LLAVE EN COMÚN
            int senderKey;
            if (chatKeys.Sender == receiver)
            {
                senderKey = ChatSingleton.Instance.DiffiHell.GenerateK(chatKeys.KeySender, int.Parse(senderCode));
            }
            else
            {
                senderKey = ChatSingleton.Instance.DiffiHell.GenerateK(chatKeys.KeyReceiver, int.Parse(senderCode));
            }

            var senderKeySDES = ChatSingleton.Instance.DiffiHell.GenerateKeySDES(senderKey);

            List<Chat> decryptedMessages = new List<Chat>();
            foreach (var chat in encryptedMessages)
            {
                if (!chat.IsFile)
                {
                    //DESCIFRAR MENSAJES
                    chat.Message = ChatSingleton.Instance.SDESEncryptor.Decipher(chat.Message, senderKeySDES);
                }
                decryptedMessages.Add(chat);
            }
            return decryptedMessages;
        }

        [Route("api/chat/send/private/{senderCode}")]
        [HttpPost]
        public IActionResult SendPrivate([FromBody] Chat chat, [FromRoute] int senderCode)
        {
            try
            {
                var idSender = (ChatSingleton.Instance.ConnectedUsers.Where(u => u.Username == chat.Sender).Select(u => u.ConnectionId).FirstOrDefault());
                var idReceiver = (ChatSingleton.Instance.ConnectedUsers.Where(u => u.Username == chat.Receiver).Select(u => u.ConnectionId).FirstOrDefault());

                //OBTENEMOS LAS LLAVES PÚBLICAS DEL CHAT
                var chatKeys = _chatKeyService.GetChatKeys(chat.Sender, chat.Receiver);

                //OBTENEMOS EL USUARIO RECEPTOR
                var receiver = _userService.GetUserByUsername(chat.Receiver);

                //GENERAMOS LLAVE EN COMÚN
                int senderKey;
                int receiverKey;
                if (chatKeys.Sender == chat.Receiver)
                {
                    senderKey = ChatSingleton.Instance.DiffiHell.GenerateK(chatKeys.KeySender, senderCode);
                    receiverKey = ChatSingleton.Instance.DiffiHell.GenerateK(chatKeys.KeyReceiver, receiver.Code);
                }
                else
                {
                    senderKey = ChatSingleton.Instance.DiffiHell.GenerateK(chatKeys.KeyReceiver, senderCode);
                    receiverKey = ChatSingleton.Instance.DiffiHell.GenerateK(chatKeys.KeySender, receiver.Code);
                }
                var senderKeySDES = ChatSingleton.Instance.DiffiHell.GenerateKeySDES(senderKey);
                var receiverKeySDES = ChatSingleton.Instance.DiffiHell.GenerateKeySDES(receiverKey);

                //ENVIARLO POR EL SOCKET AL USUARIO QUE LO ENVÍO
                _chatHub.Clients.Client(idSender).SendAsync("sendPrivate", chat.Id, chat.Sender, chat.Receiver, chat.Message, chat.Date, chat.IsFile, chat.FileName);

                //CIFRAR MENSAJE ENVIADO
                chat.Message = ChatSingleton.Instance.SDESEncryptor.Cipher(chat.Message, senderKeySDES);

                //ENVIARLO A MONGODB YA CIFRADO
                _chatService.CreateMessage(chat);

                if (idReceiver != null)
                {
                    //DESCIFRAR EL MENSAJE CON LA LLAVE DEL CONTACTO SDES
                    chat.Message = ChatSingleton.Instance.SDESEncryptor.Decipher(chat.Message, receiverKeySDES);

                    //ENVIARLO POR EL SOCKET AL RECEPTOR
                    _chatHub.Clients.Client(idReceiver).SendAsync("sendPrivate", chat.Id, chat.Sender, chat.Receiver, chat.Message, chat.Date, chat.IsFile, chat.FileName);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return BadRequest();
            }
        }

        [Route("api/chat/send/private/file")]
        [HttpPost]
        public async Task<ActionResult> SendFile()
        {   
            try
            {
                var filePath = Path.Combine(_env.ContentRootPath, "Resources/Files");
                var pathEncryption = Path.Combine(_env.ContentRootPath, "Resources/Encrypted");
                var compressPath = Path.Combine(_env.ContentRootPath, "Resources/Compressed");

                var chat = JsonConvert.DeserializeObject<Chat>(Request.Form["chat"]);
                var file = Request.Form.Files[0];
                var senderCode = int.Parse(Request.Form["senderCode"]);

                var name = file.FileName;
                var fileName = name.Split('.');

                //SE OBTIENEN LOS IDS DE CONEXIÓN
                string idSender = (ChatSingleton.Instance.ConnectedUsers.Where(u => u.Username == chat.Sender).Select(u => u.ConnectionId).FirstOrDefault());
                string idReceiver = (ChatSingleton.Instance.ConnectedUsers.Where(u => u.Username == chat.Receiver).Select(u => u.ConnectionId).FirstOrDefault());

                //OBTENEMOS LAS LLAVES PÚBLICAS DEL CHAT
                var chatKeys = _chatKeyService.GetChatKeys(chat.Sender, chat.Receiver);

                //OBTENEMOS EL USUARIO RECEPTOR
                var receiver = _userService.GetUserByUsername(chat.Receiver);

                //GENERAMOS LLAVE EN COMÚN
                int senderKey;
                int receiverKey;
                if (chatKeys.Sender == chat.Receiver)
                {
                    senderKey = ChatSingleton.Instance.DiffiHell.GenerateK(chatKeys.KeySender, senderCode);
                    receiverKey = ChatSingleton.Instance.DiffiHell.GenerateK(chatKeys.KeyReceiver, receiver.Code);
                }
                else
                {
                    senderKey = ChatSingleton.Instance.DiffiHell.GenerateK(chatKeys.KeyReceiver, senderCode);
                    receiverKey = ChatSingleton.Instance.DiffiHell.GenerateK(chatKeys.KeySender, receiver.Code);
                }
                var senderKeySDES = ChatSingleton.Instance.DiffiHell.GenerateKeySDES(senderKey);
                var receiverKeySDES = ChatSingleton.Instance.DiffiHell.GenerateKeySDES(receiverKey);

                ////SUBIMOS EL ARCHIVO A LA CARPETA FILES
                var uploaded = await UploadFile(file, filePath, name);
                if (uploaded)
                {
                    //CIFRAMOS EL ARCHIVO
                    if (ChatSingleton.Instance.SDESEncryptor.Cipher(Path.Combine(filePath, name), fileName, pathEncryption,senderKeySDES, out string RealFileNameCipher))
                    {
                        pathEncryption = Path.Combine(_env.ContentRootPath, "Resources/Encrypted", RealFileNameCipher);
                    }
                    var cipherFileName = RealFileNameCipher.Split('.');
                    //COMPRIMIMOS ARCHIVO
                    if (ChatSingleton.Instance.lzw.Compress(pathEncryption, compressPath, cipherFileName, out string RealFileNameCompress))
                    {
                        compressPath = Path.Combine(_env.ContentRootPath, "Resources/Compressed", RealFileNameCompress);
                    }
                    chat.FileName = name;
                    chat.CompressName = RealFileNameCompress;
                }

                //SOLO ENVIARLO A MONGODB
                _chatService.CreateMessage(chat);

                if (idReceiver != null)
                {
                    //ENVIARLO POR EL SOCKET
                    await _chatHub.Clients.Client(idReceiver).SendAsync("sendFile", chat.Id, chat.Sender, chat.Receiver, chat.Message, chat.Date, chat.IsFile, chat.FileName);
                }
                await _chatHub.Clients.Client(idSender).SendAsync("sendFile", chat.Id, chat.Sender, chat.Receiver, chat.Message, chat.Date, chat.IsFile, chat.FileName);
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return BadRequest();
            }
        }

        // GET: api/chat/{id}/download/
        [Route("api/chat/{id}/download")]
        [HttpGet]
        public IActionResult DownloadFile([FromRoute] string id, [FromQuery] string sender, [FromQuery] string senderCode)
        {
            try
            {
                var chat = _chatService.GetChatById(id);
                if (chat.CompressName != null)
                {
                    var extension = Path.GetExtension(chat.FileName).Split('.')[1];
                    var compressPath = Path.Combine(_env.ContentRootPath, "Resources/Compressed", chat.CompressName);
                    var decompressPath = Path.Combine(_env.ContentRootPath, "Resources/Decompressed");
                    var pathEncryption = Path.Combine(_env.ContentRootPath, "Resources/Encrypted");
                    var pathDecryption = Path.Combine(_env.ContentRootPath, "Resources/Decrypted");

                    //OBTENEMOS LAS LLAVES PÚBLICAS DEL CHAT
                    var chatKeys = _chatKeyService.GetChatKeys(chat.Sender, chat.Receiver);

                    //GENERAMOS LLAVE EN COMÚN
                    int senderKey;
                    if (chatKeys.Sender == sender)
                    {
                        senderKey = ChatSingleton.Instance.DiffiHell.GenerateK(chatKeys.KeyReceiver, int.Parse(senderCode));
                    }
                    else
                    {
                        senderKey = ChatSingleton.Instance.DiffiHell.GenerateK(chatKeys.KeySender, int.Parse(senderCode));
                    }
                    var senderKeySDES = ChatSingleton.Instance.DiffiHell.GenerateKeySDES(senderKey);

                    //DESCOMPRIMIMOS EL ARCHIVO
                    if (ChatSingleton.Instance.lzw.Decompress(compressPath, decompressPath, out string RealFileNameDecompress))
                    {
                        decompressPath = Path.Combine(_env.ContentRootPath, "Resources/Decompressed", RealFileNameDecompress);
                    }
                    var fileName = RealFileNameDecompress.Split('.');
                    //DESCIFRAMOS EL ARCHIVO
                    if (ChatSingleton.Instance.SDESEncryptor.Decipher(decompressPath, fileName, pathDecryption, senderKeySDES, out string RealFileNameCipher))
                    {
                        pathDecryption = Path.Combine(_env.ContentRootPath, "Resources/Decrypted", RealFileNameCipher);
                    }
 
                    FileStream stream = System.IO.File.OpenRead(pathDecryption);
                    return new FileStreamResult(stream, $"application/{extension}")
                    {
                        FileDownloadName = chat.FileName
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        private async Task<bool> UploadFile(IFormFile file, string originalPath, string fileName)
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
