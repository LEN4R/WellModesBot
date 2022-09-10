using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace WellModesBot.BotCommands
{
    public class FindWellsCommand : Command
    {
        private readonly WellsDataService _wellsDataService;
        private readonly MessageBuilder _messageBuilder;

        public FindWellsCommand(TelegramBotService botService, WellsDataService dataService, MessageBuilder messageBuilder) 
            : base(botService)
        {
            _wellsDataService = dataService;
            _messageBuilder = messageBuilder;
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

            if (_wellsDataService.TryFindWellsByName(parameters.OriginalText, out WellData data))
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
                            Id = x.Id.ToString(),
                            Text = text
                        };
                    }).ToArray());
                }
                else
                {
                    await Send(_messageBuilder.BuildMessageByWellId(data.Wells[0].Id));
                }
            }
            else
            {
                // Отправка данных без выбора месторождении.
                if (_wellsDataService.TryFindWellIdByNamePrefix(parameters.OriginalText, out var id))
                {
                    await Send(_messageBuilder.BuildMessageByWellId(id));
                }
                else
                {
                    await Send("\U0000274C ОШИБКА! Такой скважины нет!");
                }
            }
        }
    }
}
