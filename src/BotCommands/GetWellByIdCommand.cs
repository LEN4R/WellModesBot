using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WellModesBot.BotCommands
{
    public class GetWellByIdCommand : Command
    {
        public const string Key = "getwellbyid";

        private readonly WellsDataService _dataService;

        public GetWellByIdCommand(TelegramBotService botService, WellsDataService dataService) 
            : base(botService)
        {
            _dataService = dataService;
        }

        public override async Task Execute(CommandParameters parameters)
        {
            var messageParts = parameters.OriginalText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            Task Send(string text)
            {
                return _botService.SendMessage(parameters.ChatId, new MessageInfo()
                {
                    Text = text
                });
            }

            if (messageParts.Length != 2)
            {
                await Send($"Неверный формат команды");
                return;
            }

            var indexString = messageParts[1];

            if (!int.TryParse(indexString, out var index))
            {
                await Send($"Неверный формат индекса скважины");
                return;
            }

            await _botService.SendMessage(parameters.ChatId, new MessageInfo()
            {
                Text = _dataService.PrintFieldDataByIndex(index)
            });
        }
    }
}
