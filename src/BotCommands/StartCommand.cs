using System.Threading.Tasks;

namespace WellModesBot.BotCommands
{
    public class StartCommand : Command
    {
        public const string Key = "start";
        public StartCommand(TelegramBotService botService)
            : base(botService)
        {
        }

        public override async Task Execute(CommandParameters parameters)
        {
            await _botService.SendMessage(parameters.ChatId, new MessageInfo()
            { 
                Text = $"\U0001F44B Здравствуйте {parameters.SenderLastName} {parameters.SenderFirstName}!\n\U0001F916 Меня зовут <u>{parameters.BotName}</u>, я телеграмм бот! ",
                PhotoUrl = "https://raw.githubusercontent.com/LEN4R/WellModesBot/main/pic/logo.jpg"
            });

            await _botService.SendMessage(parameters.ChatId, new MessageInfo()
            {
                Text = $"\U00002139 Для начала работы <b>отправьте мне номер скважины</b>."
            });
        }
    }
}
