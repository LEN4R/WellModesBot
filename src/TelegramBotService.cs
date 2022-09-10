using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
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
        private readonly MessageBuilder _messageBuilder;

        readonly Command _defaultCommand;
        readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>();

        public TelegramBotService(LogService logService,
            SettingsService settingsService, 
            UsersService usersService,
            WellsDataService dataService,
            MessageBuilder messageBuilder)
        {
            _logService = logService;
            _settings = settingsService;
            _usersService = usersService;
            _data = dataService;
            _messageBuilder = messageBuilder;

            _commands = new Dictionary<string, Command>() 
            {
                { "start", new StartCommand(this) },
                { "info", new InstructionCommand(this, settingsService) },
                { "reg", new RegisterNewUserCommand(this, usersService) }
            };

            _defaultCommand = new FindWellsCommand(this, dataService, messageBuilder);
        }

        public async Task SendButtons(long chatId, string text, ButtonInfo[] buttons)
        {
            var markup = new InlineKeyboardMarkup(buttons.Select(x => new[] { InlineKeyboardButton.WithCallbackData(x.Text, x.Id) }));
            await SendTelegramMessage(chatId, text, markup: markup);
        }

        public void Start()
        {
            client = new TelegramBotClient(_settings.BotToken); // Токен бота
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
            await HandleMessage(сlient, callbackQuery.Message, callbackQuery.Data);

            /*var data = callbackQuery.Data;
            switch (data)
            {
                case "instruction":
                    //await HandleMessage(сlient, callbackQuery.Message, data);
                    // await client.SendPhotoAsync(callbackQuery.Message.Chat.Id,
                    // photo: "https://raw.githubusercontent.com/LEN4R/WellModesBot/main/pic/pic_instruction.jpg",
                    // caption: _settings.InstructionText,
                    // parseMode: ParseMode.Html);
                    break;
                case "contact":
                    await сlient.SendContactAsync(callbackQuery.Message.Chat.Id,
                    phoneNumber: "+79678888663",
                    firstName: "Галиев",
                    lastName: "Ленар");
                    break;
                case "telegramID":
                    await сlient.SendTextMessageAsync(callbackQuery.Message.Chat.Id,
                    text: $"{callbackQuery.Message.Chat.Id}",
                    parseMode: ParseMode.Html);
                    break;
                default:
                    await SendFieldInfoByIndex(int.Parse(data), callbackQuery.Message.Chat.Id);
                    break;
            }*/
        }

        // Сборка данных с массивов данных.
        private async Task SendFieldInfoByIndex(int index, long chatId)
        {
            var message = _messageBuilder.BuildMessage(index);
            await SendTelegramMessage(chatId, message.ToString());
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

        //async Task ProcessMessage(Message msg, InlineKeyboardMarkup markup)
        //{
            
        //}

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


            /*
            // Список пользователей
            var fileUserList = System.IO.File.ReadAllLines(_settings.UserList);
            HashSet<string> listOfUsers = new HashSet<string>();
            for (var i = 0; i < fileUserList.Length; i++)
            {
                listOfUsers.Add(fileUserList[i]);
            }

            // Список пользователей c правами админа
            var fileRootList = System.IO.File.ReadAllLines(_settings.RootList);
            HashSet<string> listOfRoot = new HashSet<string>();
            for (var k = 0; k < fileRootList.Length; k++)
            {
                listOfRoot.Add(fileRootList[k]);
            }

            if (msg.Text == null)
                return;

            InlineKeyboardMarkup markup = null;

            if (msg.Text == "/start")
            {
                await client.SendPhotoAsync(
                msg.Chat.Id,
                photo: "https://raw.githubusercontent.com/LEN4R/WellModesBot/main/pic/logo.jpg",
                caption: $"\U0001F44B Здравствуйте {msg.From.LastName} {msg.From.FirstName}!\n\U0001F916 Меня зовут <u>{client.GetMeAsync().Result.FirstName}</u>, я телеграмм бот! ", parseMode: ParseMode.Html);
                await client.SendTextMessageAsync(msg.Chat.Id, text: $"\U00002139 Для начала работы <b>отправьте мне номер скважины</b>.", parseMode: ParseMode.Html);
                return;

            }

            if (listOfUsers.TryGetValue(msg.Chat.Id.ToString(), out var null1))
            {
                if (listOfRoot.TryGetValue(msg.Chat.Id.ToString(), out var null2))
                {
                    if (msg.Text == "log" || msg.Text == "Log")
                    {
                        await using Stream fileUserLog = System.IO.File.OpenRead(_settings.LogUsersFilePath);
                        Message message = await client.SendDocumentAsync(msg.Chat.Id, document: new InputOnlineFile(content: fileUserLog, fileName: "LogUsers.json"));
                        return;
                    }

                    if (msg.Text.IndexOf("reg") > -1)
                    {
                        string[] separatingStrings = { " " };
                        var regUser = msg.Text.Split(separatingStrings, StringSplitOptions.RemoveEmptyEntries);
                        if (long.TryParse(regUser[1], out var null3))
                        {
                            var appendText = Environment.NewLine + regUser[1];
                            System.IO.File.AppendAllText(_settings.UserList, appendText);
                        }
                        if (msg.Chat.Id != 947161854)
                            await client.SendTextMessageAsync(msg.Chat.Id, text: $"\U00002795 Добавлен новый пользователь: {regUser[1]}", parseMode: ParseMode.Html);
                        await client.SendTextMessageAsync(msg.Chat.Id = 947161854, text: $"\U00002795 Добавлен новый пользователь: {regUser[1]}", parseMode: ParseMode.Html);
                        return;
                    }

                    if (msg.Text == "users" || msg.Text == "Users")
                    {
                        await using Stream fileUserLog = System.IO.File.OpenRead(_settings.UserList);
                        Message message = await client.SendDocumentAsync(msg.Chat.Id, document: new InputOnlineFile(content: fileUserLog, fileName: "UserList.txt"));
                        return;
                    }
                }

                if (msg.Text == "menu" || msg.Text == "Menu" || msg.Text == "Меню" || msg.Text == "меню" || msg.Text == "/help" || msg.Text == "help" || msg.Text == "Help")
                {
                    markup = new InlineKeyboardMarkup(
                        new[]
                        {
                                new []{InlineKeyboardButton.WithCallbackData("\U00002755 Инструкция по боту", "instruction")},
                                new []{InlineKeyboardButton.WithCallbackData("\U00002139 Информация по боту", "info")},
                                new []
                                {
                                    InlineKeyboardButton.WithUrl("\U0000270D Обратная связь","https://t.me/len4r"),
                                    InlineKeyboardButton.WithCallbackData("\U00002709 Контакты","contact")
                                }
                        });
                    await SendMessage(msg.Chat.Id, "\U00002705 Пожалуйста, выберите опцию:", markup: markup);
                    return;
                }
                await ProcessMessage(msg, markup);
            }
            else
            {
                await client.SendTextMessageAsync(msg.Chat.Id, text: $"\U0000274C <b>ОШИБКА!</b> У вас нет доступа!", parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("\U0001F194 Узнать Telegram ID", "telegramID") } }));
            }*/
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
    }

    struct BotUpdate
    {
        public string data;
        public string text;
        public long id;
        public string? username;
    }
}