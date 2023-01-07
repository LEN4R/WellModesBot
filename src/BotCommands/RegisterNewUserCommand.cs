using System;
using System.Threading.Tasks;

namespace WellModesBot.BotCommands
{
    public class RegisterNewUserCommand : Command
    {
        public const string Key = "reg";

        private readonly UsersService _usersService;

        public RegisterNewUserCommand(TelegramBotService botService, UsersService usersService)
            : base(botService)
        {
            _usersService = usersService;
        }

        public override async Task Execute(CommandParameters parameters)
        {
            Task Send(string text)
            {
                return _botService.SendMessage(parameters.ChatId, new MessageInfo()
                {
                    Text = text
                });
            }

            if (parameters.MessageParts.Length != 2)
            {
                await Send($"Неверный формат команды");
                return;
            }

            var newUserIdString = parameters.MessageParts[1];

            if (!long.TryParse(newUserIdString, out var newUserId))
            {
                await Send($"Неверный формат Id пользователя");
                return;
            }

            if (!_usersService.RegisterNewUser(newUserId))
            {
                await Send($"Пользователь с указанным Id уже зарегистрирован");
                return;
            }

            await Send($"\U00002795 Добавлен новый пользователь: {newUserId}");
        }
    }
}
