using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using WellModesBot.BotCommands;

namespace WellModesBot
{
    public class TelegramBotService
    {
        private TelegramBotClient client;

        private readonly LogService _logService;
        private readonly SettingsService _settings;
        private readonly UsersService _usersService;
        private readonly WellsDataService _data;

        readonly Command _defaultCommand;
        readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>();

        public TelegramBotService(LogService logService,
            SettingsService settingsService, 
            UsersService usersService,
            WellsDataService dataService)
        {
            _logService = logService;
            _settings = settingsService;
            _usersService = usersService;
            _data = dataService;

            _commands = new Dictionary<string, Command>() 
            {
                { StartCommand.Key, new StartCommand(this) },
                { GetInstructionCommand.Key, new GetInstructionCommand(this, settingsService) },
                { RegisterNewUserCommand.Key, new RegisterNewUserCommand(this, usersService) },
                { GetMyIDCommand.Key, new GetMyIDCommand(this) },
                { GetContactsCommand.Key, new GetContactsCommand(this, settingsService) },
                { GetWellByIdCommand.Key, new GetWellByIdCommand(this, dataService) },
                { GetClusterCommand.Key, new GetClusterCommand(this, dataService) }
            };

            _defaultCommand = new FindWellsCommand(this, dataService);
        }

        public async Task SendButtons(long chatId, string text, ButtonInfo[] buttons)
        {
            var markup = new InlineKeyboardMarkup(buttons.Select(x => new[] { InlineKeyboardButton.WithCallbackData(x.Text, x.Id) }));
            await SendTelegramMessage(chatId, text, markup: markup);
        }

        public void Start()
        {
            client = new TelegramBotClient(_settings.BotTokenFilePath); // Токен бота
            using var cts = new CancellationTokenSource(); // Токен отмены
            var receiverOptions = new ReceiverOptions { AllowedUpdates = new[] { UpdateType.CallbackQuery, UpdateType.Message } };  // Настройка получении обновлени
            client.StartReceiving(HandleUpdatesAsync, HandleErrorAsync, receiverOptions, cancellationToken: cts.Token); // Функция получении обновлении от Telegram

            // Проверка на запуск
            var me = client.GetMeAsync().Result;
            Console.WriteLine($"Bot ID: {me.Id} \nBot Name: {me.FirstName}");
            Console.ReadLine();
            cts.Cancel();

            //Запись всех обновлении бота
            /*try
            {
                var botUpdatesString = System.IO.File.ReadAllText(_settings.LogUsersFilePath);
                botUpdate = JsonConvert.DeserializeObject<List<BotUpdate>>(botUpdatesString) ?? botUpdate;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка десериализации обновлении бота {ex}");
            }*/
        }


        // Метод обработки обновление бота
        async Task HandleUpdatesAsync(ITelegramBotClient сlient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                await HandleMessage(сlient, update.Message);
                if (update.Message.Chat.Id != _settings.AdministratorId)
                {
                    var timeZoneHourEkb = (update.Message.Date.Hour + 5) % 24;
                    var botUpdate = new BotUpdate
                    {
                        id = update.Message.Chat.Id,
                        data = update.Message.Date.Day + "." + update.Message.Date.Month + "." + update.Message.Date.Year + " " + timeZoneHourEkb + ":" + update.Message.Date.Minute,
                        text = update.Message.Text,
                        username = update.Message.Chat.Username + " " + update.Message.From.FirstName + " " + update.Message.From.LastName,
                    };

                    _logService.LogMessage(botUpdate);
                    return;
                }
            }

            if (update.Type == UpdateType.CallbackQuery)
            {
                await HandleCallbackQuery(сlient, update.CallbackQuery);
                return;
            }

            if (update.Message!.Type != MessageType.Text)
                return;
        }

        async Task HandleCallbackQuery(ITelegramBotClient сlient, CallbackQuery callbackQuery)
        {
            await HandleMessage(client, callbackQuery.Message, callbackQuery.Data);
        }

        private async Task SendTelegramMessage(long chatId, string message, int replyMessageId = 0, InlineKeyboardMarkup markup = null)
        {
            await client.SendTextMessageAsync(chatId: chatId, text: message, replyToMessageId: replyMessageId, replyMarkup: markup);
        }

        // Обработка ошибок бота
        Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Ошибка Telegram Api: {apiRequestException.ErrorCode}",
                _ => exception.ToString()
            };
            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }

        // Метод обработки сообщении бота
        async Task HandleMessage(ITelegramBotClient сlient, Message msg, string commandOverride = null)
        {
            var commandText = commandOverride ?? msg.Text;

            if (commandText == null)
                return;

            var originalText = commandText;

            if (commandText.StartsWith('/'))
            {
                commandText = commandText.Substring(1);
            }

            commandText = commandText.ToLowerInvariant();
               
            var currentUser = await client.GetMeAsync();

            var parameters = new CommandParameters()
            {
                OriginalText = originalText,
                SenderLastName = msg.From.LastName,
                SenderFirstName = msg.From.FirstName,
                BotName = currentUser.FirstName,
                ChatId = msg.Chat.Id,
            };

            if (_commands.TryGetValue(commandText, out Command command))
                await command.Execute(parameters);
            else
                await _defaultCommand.Execute(parameters);
        }

        internal async Task SendMessage(long chatId, MessageInfo message)
        {
            if (message.PhotoUrl != null)
            {
                await client.SendPhotoAsync(
                 chatId,
                 photo: message.PhotoUrl,
                 caption: message.Text, parseMode: ParseMode.Html);
            }
            else
            {
                await client.SendTextMessageAsync(chatId, text: message.Text, parseMode: ParseMode.Html);
            }
        }

        public Task SendContact(long chatId, string phoneNumber, string firstName, string lastName)
        {
            return client.SendContactAsync(chatId, phoneNumber, firstName, lastName);
        }
    }

    struct BotUpdate
    {
        public string data;
        public string text;
        public long id;
        public string? username;
    }
}