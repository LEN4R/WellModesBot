using System;
using System.Threading.Tasks;

namespace WellModesBot.BotCommands
{
    public abstract class Command
    {
        protected readonly TelegramBotService _botService;

        public Command(TelegramBotService botService)
        {
            _botService = botService;
        }

        public abstract Task Execute(CommandParameters parameters);
    }
}
