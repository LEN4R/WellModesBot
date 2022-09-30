using System;
using System.Linq;
using System.Threading.Tasks;

namespace WellModesBot.BotCommands
{
    public class GetClusterCommand : Command
    {
        public const string Key = "cluster";
        private readonly WellsDataService _dataService;

        public GetClusterCommand(TelegramBotService botService, WellsDataService dataService) 
            : base(botService)
        {
            _dataService = dataService;
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

            var messageParts = parameters.MessageParts;

            if (messageParts.Length < 2 || messageParts.Length > 3)
            {
                await Send($"Неверный формат команды");
                return;
            }

            var clusterNumber = messageParts[1];
            var clusters = _dataService.FindClustersByNumber(clusterNumber);

            if (messageParts.Length == 2)
            {
                if (clusters.Count == 0)
                {
                    await Send($"Такого куста не существует");
                    return;
                }

                if (clusters.Count > 1)
                {
                    await _botService.SendButtons(parameters.ChatId, "Какой куст вас интересует?", clusters.Select(x => new ButtonInfo
                    {
                        Id = $"{GetClusterCommand.Key} {clusterNumber} {x.Key}",
                        Text = x.Key
                    }).ToArray());
                }
                else
                {
                    await SendClusterInfo(clusters.First().Value, parameters);
                }
            }
            else
            {
                if (clusters.TryGetValue(messageParts[2], out var cluster))
                {
                    await SendClusterInfo(cluster, parameters);
                }
                else
                {
                    await Send($"Такого куста не существует");
                }
            }
        }

        private Task SendClusterInfo(WellsClusterInfo cluster, CommandParameters parameters)
        {
            return _botService.SendMessage(parameters.ChatId, new MessageInfo()
            {
                Text = string.Join('\n', cluster.WellsOrderList)
            });
        }
    }
}
