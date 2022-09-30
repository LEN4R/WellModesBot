using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WellModesBot.BotCommands
{
    public class FindWellsCommand : Command
    {
        public const string Key = "findwells";

        private readonly WellsDataService _wellsDataService;

        public FindWellsCommand(TelegramBotService botService, WellsDataService dataService) 
            : base(botService)
        {
            _wellsDataService = dataService;
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

            var query = parameters.OriginalText;

            if (query.StartsWith(Key, System.StringComparison.OrdinalIgnoreCase))
                query = query.Substring(Key.Length).TrimStart();

            if (_wellsDataService.TryFindWellsByName(query, out WellData data))
            {
                // Отправка данных с выбором месторождении.
                if (data.WellsCount > 1)
                {
                    await _botService.SendButtons(parameters.ChatId, "\U0001F50E Пожалуйста, выберите скважину:", data.Wells.Select(x =>
                    {
                        var name = _wellsDataService.GetWorkSheetNameByNumber(x.WorksheetNumber);
                        var text = name + " " + x.FullName;

                        return new ButtonInfo()
                        {
                            Id = $"{GetWellByIdCommand.Key} {x.Id}",
                            Text = text
                        };
                    }).ToArray());
                }
                else
                {
                    await Send(_wellsDataService.PrintFieldDataByIndex(data.Wells[0].Id));
                }
            }
            else
            {
                // Отправка данных без выбора месторождении.
                if (_wellsDataService.TryFindWellIdByNamePrefix(parameters.OriginalText, out var id))
                {
                    await Send(_wellsDataService.PrintFieldDataByIndex(id));
                }
                else
                {
                    await Send("\U0000274C ОШИБКА! Такой скважины нет!");
                }
            }
        }
    }
}
