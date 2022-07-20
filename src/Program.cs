
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Newtonsoft.Json;
using Telegram.Bot.Types.InputFiles;

namespace WellModesBot
{
    struct BotUpdate
    {
        public string data;
        public string text;
        public long id;
        public string? username;
    }
    class Program
    {
        //private static string token = "5348869621:AAFeOl55384vMInbTORGsZwo9YVn-NoEv9w"; //WellModesBot
        private static string token = System.IO.File.ReadAllText("wmbot.txt"); //WMBot

        private static readonly string InstructionText = $"\U00000031\U000020E3 Введите номер скважины, бот выводит режимные данные.\n" +
                                                         $"\U00000032\U000020E3 Если номер скважины совпадает в нескольких месторождении, бот предложит выбор скважин.\n\n" +
                                                         $"\U00002139 Для быстрого вывода данных возможен ввод: [номер скважины]+[начало названия месторождения]. \n\U000025B6 <b>Бот не привязан к регистру!</b>\U0001F4AA \n\n " +
                                                         $"\U000026A0 Бот не воспринимает '*', для получения информации скважин с индексом, неодходимо ввеcти <b>Индекс!</b>";

        private static readonly string InfoText = $"\U0001F4C5 Дата создания бота: <b>20.04.2022</b>\n" +
                                                  $"\U0001F4BB Версия бота: <b>1.1.2</b>\n" +
                                                  $"\U0001F4BE Технологические режимы от <b>07.2022</b>";

        private static TelegramBotClient client;

        static string logUsers = "logUsers.json";
        static string userList = @"users.txt";
        static string rootList = @"root.txt";
        static List<BotUpdate> botUpdate = new List<BotUpdate>();
        private static List<WorksheetInfo> _worksheetsList;
        private static List<FieldInfo> _allFields;
        private static Dictionary<string, List<FieldInfo>> _allFieldsCombined;


        static void Main(string[] args)
        {
            GetData();
            client = new TelegramBotClient(token); // Токен бота
            using var cts = new CancellationTokenSource(); // Токен отмены
            var receiverOptions = new ReceiverOptions { AllowedUpdates = new[] { UpdateType.CallbackQuery, UpdateType.Message } };  // Настройка получении обновлени
            client.StartReceiving(HandleUpdatesAsync, HandleErrorAsync, receiverOptions, cancellationToken: cts.Token); // Функция получении обновлении от Telegram

            // Проверка на запуск
            var me = client.GetMeAsync().Result;
            Console.WriteLine($"Bot ID: {me.Id} \nBot Name: {me.FirstName}");
            Console.ReadLine();
            cts.Cancel();

            //Запись всех обновлении бота
            try
            {
                var botUpdatesString = System.IO.File.ReadAllText(logUsers);
                botUpdate = JsonConvert.DeserializeObject<List<BotUpdate>>(botUpdatesString) ?? botUpdate;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка десериализации обновлении бота {ex}");
            }

            // Метод обработки обновление бота
            async Task HandleUpdatesAsync(ITelegramBotClient сlient, Update update, CancellationToken cancellationToken)
            {
                if (update.Type == UpdateType.Message && update?.Message?.Text != null)
                {
                    await HandleMessage(сlient, update.Message);
                    if (update.Message.Chat.Id != 947161854)
                    {
                        var timeZoneHourEkb = (update.Message.Date.Hour + 5) % 24;
                        var _botUpdate = new BotUpdate
                        {
                            id = update.Message.Chat.Id,
                            data = update.Message.Date.Day + "." + update.Message.Date.Month + "." + update.Message.Date.Year + " " + timeZoneHourEkb + ":" + update.Message.Date.Minute,
                            text = update.Message.Text,
                            username = update.Message.Chat.Username + " " + update.Message.From.FirstName + " " + update.Message.From.LastName,
                        };
                        botUpdate.Add(_botUpdate);
                        var botUpdatesString = JsonConvert.SerializeObject(botUpdate);
                        System.IO.File.WriteAllText(logUsers, botUpdatesString);
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

            // Метод обработки сообщении бота
            async Task HandleMessage(ITelegramBotClient сlient, Message msg)
            {
                // Список пользователей
                var fileUserList = System.IO.File.ReadAllLines(userList);
                HashSet<string> listOfUsers = new HashSet<string>();
                for (var i = 0; i < fileUserList.Length; i++)
                {
                    listOfUsers.Add(fileUserList[i]);
                }

                // Список пользователей c правами админа
                var fileRootList = System.IO.File.ReadAllLines(rootList);
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
                            await using Stream fileUserLog = System.IO.File.OpenRead(logUsers);
                            Message message = await client.SendDocumentAsync(msg.Chat.Id, document: new InputOnlineFile(content: fileUserLog, fileName: "LogUsers.json"));
                            return;
                        }

                        if (msg.Text.IndexOf("reg")> -1)
                        {
                            string[] separatingStrings = {" "};
                            var regUser = msg.Text.Split(separatingStrings, StringSplitOptions.RemoveEmptyEntries);
                            if (long.TryParse(regUser[1], out var null3))
                            {
                                var appendText = Environment.NewLine + regUser[1];
                                System.IO.File.AppendAllText(userList, appendText);
                            }
                            if (msg.Chat.Id != 947161854)
                                await client.SendTextMessageAsync(msg.Chat.Id, text: $"\U00002795 Добавлен новый пользователь: {regUser[1]}", parseMode: ParseMode.Html);
                            await client.SendTextMessageAsync(msg.Chat.Id = 947161854, text: $"\U00002795 Добавлен новый пользователь: {regUser[1]}", parseMode: ParseMode.Html);
                            return;
                        }

                        if (msg.Text == "users" || msg.Text == "Users")
                        {
                            await using Stream fileUserLog = System.IO.File.OpenRead(userList);
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
                }
            }

            async Task HandleCallbackQuery(ITelegramBotClient сlient, CallbackQuery callbackQuery)
            {
                var data = callbackQuery.Data;
                switch (data)
                {
                    case "instruction":
                        await client.SendPhotoAsync(callbackQuery.Message.Chat.Id,
                        photo: "https://raw.githubusercontent.com/LEN4R/WellModesBot/main/pic/pic_instruction.jpg",
                        caption: InstructionText,
                        parseMode: ParseMode.Html);
                        break;
                    case "info":
                        await client.SendPhotoAsync(callbackQuery.Message.Chat.Id,
                        photo: "https://raw.githubusercontent.com/LEN4R/WellModesBot/main/pic/pic_info.jpg",
                        caption: InfoText,
                        parseMode: ParseMode.Html);
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
                }
            }

            async Task ProcessMessage(Message msg, InlineKeyboardMarkup markup)
            {
                var message = new StringBuilder();
                if (_allFieldsCombined.TryGetValue(msg.Text, out var wellsList))
                {
                    // Отправка данных с выбором месторождении.
                    if (wellsList.Count > 1)
                    {
                        message.Append("\U0001F50E Пожалуйста, выберите скважину:");
                        markup = new InlineKeyboardMarkup(wellsList.Select(x => new[] {InlineKeyboardButton.WithCallbackData(_worksheetsList[x.WorksheetNumber].Name + " " + x.FullName, _allFields.IndexOf(x).ToString())}).ToArray());
                    }
                    else
                    {
                        PrintFieldDataByColumnIndexes(wellsList[0], message, _worksheetsList[wellsList[0].WorksheetNumber]);
                    }
                    await SendMessage(msg.Chat.Id, message.ToString(), markup: markup);
                }
                else
                {
                    // Отправка данных без выбора месторождении.
                    var firstField = _allFields.FirstOrDefault(x => x.FullName.StartsWith(msg.Text, StringComparison.OrdinalIgnoreCase));
                    if (firstField != null)
                    {
                        PrintFieldDataByColumnIndexes(firstField, message, _worksheetsList[firstField.WorksheetNumber]);
                        await SendMessage(msg.Chat.Id, message.ToString());
                    }
                    else
                    {
                        await SendMessage(msg.Chat.Id, "\U0000274C ОШИБКА! Такой скважины нет!");
                    }
                }
            }

            // Обработка ошибок бота
            Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
            {
                var errorMessage = exception switch
                {
                    ApiRequestException apiRequestException => $"Ошибка Telegram Api: {apiRequestException.ErrorCode}",_ => exception.ToString()
                };
                Console.WriteLine(errorMessage);
                return Task.CompletedTask;
            }
        }

        // Сборка данных с массивов данных.
        private static async Task SendFieldInfoByIndex(int index, long chatId)
        {
            var firstField = _allFields[index];
            var worksheet = _worksheetsList[firstField.WorksheetNumber];
            var message = new StringBuilder();
            PrintFieldDataByColumnIndexes(firstField, message, worksheet);
            await SendMessage(chatId, message.ToString());
        }

        private static async Task SendMessage(long chatId, string message, int replyMessageId = 0, InlineKeyboardMarkup markup = null)
        {
            await client.SendTextMessageAsync(chatId: chatId, text: message, replyToMessageId: replyMessageId, replyMarkup: markup);
        }

        private static void PrintFieldDataByColumnIndexes(FieldInfo field, StringBuilder message, WorksheetInfo info)
        {
            var query = info.ColumnNames
                .Select((x, i) => (key: x, value: field.Data[i], metrics: info.ColumnMetrics[i]))
                .Where(x => x.key != null).ToArray();

            foreach ((int, OutputType) index in info.RequiredData)
            {
                var queryIndex = query[index.Item1];

                switch (index.Item2)
                {
                    case OutputType.Default:
                        if (queryIndex.key == "№ скв")
                            queryIndex.key = "Скважина";
                        else if (queryIndex.metrics == "ат")
                            queryIndex.metrics = "атм";
                        message.AppendLine($"{queryIndex.key}: {queryIndex.value} {queryIndex.metrics}");
                        break;
                    case OutputType.PVR:
                        if (queryIndex.key == "верх")
                            queryIndex.key = "Вверх. интер. перф.";
                        else if (queryIndex.key == "низ")
                            queryIndex.key = "Нижн. интер. перф.";
                        message.AppendLine($"{queryIndex.key}: {queryIndex.value} {queryIndex.metrics}");
                        break;
                    case OutputType.Number:
                        bool numbertwo = double.TryParse(queryIndex.value.ToString(), out var result);
                        if (numbertwo)
                            message.AppendLine($"{queryIndex.key}: {double.Parse(queryIndex.value.ToString()).ToString("0.00")} {queryIndex.metrics}");
                        else
                            message.AppendLine($"{queryIndex.key}: {queryIndex.value} {queryIndex.metrics}");
                        break;
                    case OutputType.MRP:
                        string? queryIndexinput = queryIndex.value.ToString();
                        bool mrpBool = int.TryParse(queryIndexinput, out var number);
                        if (mrpBool == true)
                            message.AppendLine($"{queryIndex.key}: {Int32.Parse(queryIndex.value.ToString()) + DateTime.Now.Day - 1} {queryIndex.metrics} на {DateTime.Now.ToString("dd.MM.yyyy")}");
                        else
                            message.AppendLine($"{queryIndex.key}: {queryIndex.value} {queryIndex.metrics}");
                        break;
                    case OutputType.KNS:
                        if (queryIndex.key == "БКНС, КНС")
                            message.AppendLine($"{"БКНС"}: КНС-{queryIndex.value} {queryIndex.metrics}");
                        break;
                    default:
                        break;
                }
            }
        }

        public static void GetData()
        {
            //var path = Directory.EnumerateFiles(Environment.CurrentDirectory).FirstOrDefault(x => x.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase));
            var path = @"Info.xlsx";
            Console.WriteLine($"Файл загружен:{path}");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var xlPackage = new ExcelPackage(new FileInfo(path)))
            {
                var worksheetsList = new List<WorksheetInfo>();
                worksheetsList.Add(ReadWorksheet(xlPackage, 0, new[] { (4, OutputType.Default),   // Месторождение
                                                                       (6, OutputType.Default),   // № скв
                                                                       (7, OutputType.Default),   // Куст
                                                                       (2, OutputType.Default),   // Цех
                                                                       (16, OutputType.Default),  // Диам. экспл. колон.
                                                                       (10, OutputType.Default),  // Объект разработки/пласт
                                                                       (13, OutputType.PVR),      // верх
                                                                       (14, OutputType.PVR),      // низ
                                                                       (15, OutputType.Number),   // Удл. на в.д.
                                                                       (17, OutputType.Default),  // Тек. забой
                                                                       (21, OutputType.Default),  // Марка насоса
                                                                       (22, OutputType.Default),  // Глубина насоса
                                                                       (34, OutputType.Default),  // Доп. оборуд.
                                                                       (32, OutputType.MRP),      // МРП
                                                                       (28, OutputType.Default),  // N
                                                                       (35, OutputType.Default),  // D шт.
                                                                       (38, OutputType.Default),  // Ндин
                                                                       (39, OutputType.Default),  // Рзат. при Ндин.
                                                                       (41, OutputType.Number),   // Рдин. на ТМС
                                                                       (51, OutputType.Default),  // Рпл. внк
                                                                       (64, OutputType.Default),  // Сост. на конец мес/
                                                                       (54, OutputType.Number),   // Qж.ф.
                                                                       (55, OutputType.Number),   // % воды
                                                                       (56, OutputType.Number),   // Qн.ф.
                                                                       })); //ТРДС
                worksheetsList.Add(ReadWorksheet(xlPackage, 1, new[] { (4, OutputType.Default),   // Месторождение
                                                                       (6, OutputType.Default),   // № скв
                                                                       (7, OutputType.Default),   // Куст
                                                                       (2, OutputType.Default),   // Цех
                                                                       (3, OutputType.KNS),       // БКНС, КНС
                                                                       (10, OutputType.Default),  // Блок 
                                                                       (11, OutputType.Default),  // Объект разработки
                                                                       (18, OutputType.PVR),      // верх
                                                                       (19, OutputType.PVR),      // низ
                                                                       (20, OutputType.Number),   // Удл. на в.д.
                                                                       (22, OutputType.Default),  // Иск. забой
                                                                       (23, OutputType.Default),  // Тек. забой
                                                                       (24, OutputType.Default),  // СЭ/Характер лифта
                                                                       (25, OutputType.Default),  // Длина подвески НКТ
                                                                       (29, OutputType.Default),  // Глубина пакера
                                                                       (32, OutputType.Default),  // Доп.оборуд. (длина хвост.)
                                                                       (114, OutputType.MRP),     // МРП
                                                                       (47, OutputType.Default),  // Рпл. внк
                                                                       (44, OutputType.Default),  // Нст.
                                                                       (43, OutputType.Default),  // Руст. стат.
                                                                       (53, OutputType.Number),   // Q
                                                                       (37, OutputType.Number),  // Pл.
                                                                       (33, OutputType.Default),  // Dшт.
                                                                       (116, OutputType.Default), // Потребная закачка
                                                                       })); //ТРНС

                _worksheetsList = worksheetsList;                _allFields = worksheetsList.SelectMany(x => x.Fields).ToList();
                _allFieldsCombined = worksheetsList.SelectMany(x => x.FieldsCombined)
                    .GroupBy(x => x.Key)
                    .Select(x => (x.Key, x.SelectMany(y => y.Value)))
                    .ToDictionary(x => x.Key, x => x.Item2.ToList());
            }
        }

        private static WorksheetInfo ReadWorksheet(ExcelPackage xlPackage, int worksheetIndex, (int, OutputType)[] requiredData)
        {
            var worksheetFields = new List<FieldInfo>();
            var worksheetFieldsCombined = new Dictionary<string, List<FieldInfo>>();
            var columnNames = new List<string>();
            var columnMetrics = new List<string>();

            var myWorksheet = xlPackage.Workbook.Worksheets[worksheetIndex];
            var totalRows = myWorksheet.Dimension.End.Row;
            var totalColumns = myWorksheet.Dimension.End.Column;

            for (int k = 2; k <= totalColumns; k++)
            {
                columnNames.Add(myWorksheet.Cells[14, k].Value?.ToString() ?? myWorksheet.Cells[13, k].Value?.ToString());
                columnMetrics.Add(myWorksheet.Cells[15, k].Value?.ToString());
            }

            for (int i = 22; i <= totalRows; i++)
            {
                var numberCell = myWorksheet.Cells[i, 8];
                var fieldNameCell = myWorksheet.Cells[i, 6];

                var number = numberCell.Value;
                var fieldName = fieldNameCell.Value;

                if (number == null || fieldName == null)
                    continue;

                var numberStr = number.ToString();
                var fieldNameStr = fieldName.ToString();

                if (string.IsNullOrWhiteSpace(numberStr) || string.IsNullOrWhiteSpace(fieldNameStr))
                    continue;

                //if (!worksheetFieldsCombined.TryGetValue(numberStr.ToLowerInvariant(), out List<FieldInfo> list)) // Укороченный поиск
                //    list = worksheetFieldsCombined[numberStr] = new List<FieldInfo>();

                var data = new List<object>();

                for (int k = 2; k <= totalColumns; k++)
                {
                    var dataCell = myWorksheet.Cells[i, k];
                    data.Add(dataCell.Value);
                }

                var fieldInfo = new FieldInfo()
                {
                    Number = numberStr,
                    FieldName = fieldNameStr,
                    RowIndex = i,
                    Data = data,
                    WorksheetNumber = worksheetIndex
                };

                var numberBuilder = new StringBuilder(numberStr);
                while (numberBuilder.Length > 0)
                {
                    var key = numberBuilder.ToString().ToLowerInvariant();
                    if (!worksheetFieldsCombined.TryGetValue(key, out List<FieldInfo> list))
                        list = worksheetFieldsCombined[key] = new List<FieldInfo>();

                    list.Add(fieldInfo);
                    numberBuilder.Remove(numberBuilder.Length - 1, 1);
                }

                //list.Add(fieldInfo);
                worksheetFields.Add(fieldInfo);
            }

            return new WorksheetInfo(myWorksheet.Name, worksheetFields, worksheetFieldsCombined, requiredData, columnNames, columnMetrics);
        }
    }
}
