using ChatApp.BLL.Interfaces;
using ChatApp.DAL;
using ChatApp.DAL.Entities;
using ChatApp.DAL.Repositories;
using ChatApp.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Controllers;

public class GenerateController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccountService _accountService;

    public GenerateController(IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        IAccountService accountService)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _accountService = accountService;
    }
    
    [HttpPost]
    [Route("/generate/users")]
    public async Task<IActionResult> GenerateUsers()
    {
        const int count = 67;
        
        for (var i = 0; i < count; i++)
        {
            await _accountService.RegisterAsync(new RegisterViewModel
            {
                Login = $"login{i}",
                Email = $"email{i}@my.website",
                PhoneNumber = $"{i:D10}",
                Password = $"Password!{i}",
                PasswordConfirmation = $"Password!{i}"
            });
        }
        
        Console.WriteLine("Users registered");
        return Ok();
    }
    
    [HttpPost]
    [Route("/generate/personal-chats")]
    public async Task<IActionResult> GeneratePersonalChats()
    {
        var random = new Random();
        var users = _userManager.Users.ToList();
        var messageRepository = _unitOfWork.GetRepository<MessageRepository>();
        var memberChatRepository = _unitOfWork
            .GetRepository<MemberChatRepository>();
        var chatRepository = _unitOfWork.GetRepository<ChatRepository>();
        for (var i = 0; i < users.Count; i++)
        {
            for (var j = i + 1; j < users.Count; j++)
            {
                if (random.NextDouble() < 0.7)
                {
                    continue;
                }

                var username1 = users[i].UserName;
                var username2 = users[j].UserName;
                if (string.CompareOrdinal(username1, username2) > 0)
                {
                    (username1, username2) = (username2, username1);
                }
                var chat = new Chat
                {
                    Name = $"{username1} and {username2} Personal Chat"
                };
                await chatRepository.InsertAsync(chat);
                await _unitOfWork.SaveAsync();

                var userId1 = users[i].Id!;
                var userId2 = users[j].Id!;
                await memberChatRepository.InsertAsync(new MemberChat
                {
                    ChatId = chat.Id,
                    UserId = userId1
                });
                await memberChatRepository.InsertAsync(new MemberChat
                {
                    ChatId = chat.Id,
                    UserId = userId2
                });

                await GenerateMessages(messageRepository, 
                    chat, userId1, userId2);
                await _unitOfWork.SaveAsync();
            }
        }

        Console.WriteLine("Personal chats registered");
        return Ok();
    }

    private static async Task GenerateMessages(
        MessageRepository messageRepository,
        Chat chat, string userId1, string userId2)
    {
        for (var k = 0; k < 5; k++)
        {
            await messageRepository.InsertAsync(new Message
            {
                ChatId = chat.Id,
                DateTime = DateTime.UtcNow,
                ReplyTo = -1,
                UserId = k % 2 == 0 ? userId1 : userId2,
                Text = "message " + k
            });
        }
    }

    [HttpPost]
    [Route("/generate/group-chats")]
    public async Task<IActionResult> GenerateGroupChats()
    {
        const int chatsCount = 12;
        var random = new Random();
        var users = _userManager.Users.ToList();
        var messageRepository = _unitOfWork.GetRepository<MessageRepository>();
        var memberChatRepository = _unitOfWork
            .GetRepository<MemberChatRepository>();
        var chatRepository = _unitOfWork.GetRepository<ChatRepository>();
        for (var i = 0; i < chatsCount; i++)
        {
            var chat = new Chat
            {
                IsPersonal = false,
                Name = $"Group chat {i}"
            };
            await chatRepository.InsertAsync(chat);
            await _unitOfWork.SaveAsync();
            
            var usersCount = random.Next(users.Count);
            for (var j = 0; j < usersCount; j++)
            {
                var userIndex = random.Next(users.Count);
                await memberChatRepository.InsertAsync(new MemberChat
                {
                    ChatId = chat.Id,
                    UserId = users[userIndex].Id
                });
                for (var k = 0; k < random.Next(20); k++)
                {
                    await messageRepository.InsertAsync(new Message
                    {
                        ChatId = chat.Id,
                        UserId = users[userIndex].Id,
                        DateTime = DateTime.UtcNow - TimeSpan.FromMinutes(
                            random.Next(1000)),
                        ReplyTo = -1,
                        Text = $"{users[userIndex].UserName} message {k} " +
                               $"in chat {chat.Name}"
                    });
                }
            }
        }
        
        await _unitOfWork.SaveAsync();
        Console.WriteLine("Group chats registered");
        return Ok();
    }
}