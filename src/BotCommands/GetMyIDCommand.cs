using System.Threading.Tasks;

namespace WellModesBot.BotCommands
{
    public class GetMyIDCommand : Command
    {
        public const string Key = "telegramid";

        public GetMyIDCommand(TelegramBotService botService)
            : base(botService)
        {
        }

        public override Task Execute(CommandParameters parameters)
        {
            return _botService.SendMessage(parameters.ChatId, new MessageInfo()
            { 
                Text = parameters.ChatId.ToString()
            });
        }
    }
}
