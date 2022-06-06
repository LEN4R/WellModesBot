
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
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
        private static string token = "5348869621:AAFeOl55384vMInbTORGsZwo9YVn-NoEv9w"; //WellModesBot
        //private static string token = "5333261863:AAEm0hBmW13UOuu2weGKZqlSHx3Nk7-4tlg"; // TestLEN4RBot

        private static readonly string InstructionText = $"\U00000031\U000020E3 Введите номер скважины, бот выводит режимные данные по скважине.\n" +
                                                         $"\U00000032\U000020E3 Если номер скважины совпадает в нескольких месторождении, бот предложит выбор выгрузки данных.\n\n" +
                                                         $"\U00002139 Для быстрого вывода данных возможен ввод: [номер скважины]+[начало названия месторождения]. \n\U000025B6 <b>Бот не привязан к регистру!</b>\U0001F4AA \n\n " +
                                                         $"\U000026A0 Бот не воспринимает '*', для получения информации скважин с индексом, неодходимо ввеcти <b>Индекс!</b>";

        private static readonly string VersionText = $"<b>[21.04.2022 Версия: 1.0.0 (Beta)]</b> \n \U000025AA Добавлена возможность вывода скважин с разными месторождениями. \n\n" +
                                                     $"<b>[23.04.2022 Версия: 1.0.1]</b> \n \U000025AA Добавлены единицы изменерия. \n \U000025AA Убран баг некорректного вывода скважин. \n\n " +
                                                     $"<b>[25.04.2022 Версия: 1.0.1.1]</b> \n \U000025AA Все текстовые команды, включая вывод скважин с разными месторождениями, переписаны в удобное кнопочное меню. \n\n " +
                                                     $"<b>[28.04.2022 Версия: 1.0.2]</b> \n \U000025AA Код переписан под API.TelegramBot v17 и адаптирован под хостинг. Теперь бот доступен 24/7.  \n\n " +
                                                     $"<b>[03.05.2022 Версия: 1.0.3]</b> \n \U000025AA Добавлена поддержка вывода режимных данных нагнетательных скважин.  \n\n " +
                                                     $"<b>[07.05.2022 Версия: 1.0.4]</b> \n \U000025AA Проработан вывод всех возможных вариантов по запросу. \n \U000025AA Расчет МРП производится на текущий день. \n \U000025AA Сокращение сиволов до двух знаков после запятой.";

        private static readonly string InfoText = $"\U0001F4C5 Дата создания бота: <b>20.04.2022</b>\n" +
                                                  $"\U0001F4BB Версия бота: <b>1.0.4 (Beta)</b>\n" +
                                                  $"\U0001F4BE Технологические режимы от <b>06.2022</b>";

        private static string Url = "https://v2.d-f.pw/app/application/6310/";
        private static TelegramBotClient client;

        static string logUsers = "logUsers.json";
        static List<BotUpdate> botUpdate = new List<BotUpdate>();
        private static List<WorksheetInfo> _worksheetsList;
        private static List<FieldInfo> _allFields;
        private static Dictionary<string, List<FieldInfo>> _allFieldsCombined;

        static void Main(string[] args)
        {
            GetData();
            client = new TelegramBotClient(token); // Токен бота
            using var cts = new CancellationTokenSource(); // Токен отмены
            var receiverOptions = new ReceiverOptions { AllowedUpdates = new[] { UpdateType.CallbackQuery, UpdateType.Message }};  // Настройка получении обновлени
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
                        var _botUpdate = new BotUpdate
                        {   
                            id = update.Message.Chat.Id,
                            data = update.Message.Date.Day + "." + update.Message.Date.Month + "." + update.Message.Date.Year + " " + update.Message.Date.Hour + ":" + update.Message.Date.Minute,
                            text = update.Message.Text,
                            username = update.Message.Chat.Username +" "+ update.Message.From.FirstName + " " + update.Message.From.LastName,
                        };
                        botUpdate.Add(_botUpdate);
                        var botUpdatesString = JsonConvert.SerializeObject(botUpdate);
                        System.IO.File.WriteAllText(logUsers, botUpdatesString);
                        return;
                    }
                    else
                    {
                        //await client.SendTextMessageAsync(update.Message.Chat.Id, text: $"Неавторизованный пользователь", parseMode: ParseMode.Html);
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
                if (msg.Text == null)
                    return;

                InlineKeyboardMarkup markup = null;

                if (msg.Text == "/start")
                {
                    await client.SendPhotoAsync(
                    msg.Chat.Id,
                    photo: "https://raw.githubusercontent.com/LEN4R/WellModesBot/main/pic/logo.jpg",
                    caption: $"\U0001F44B Здравствуйте {msg.From.LastName} {msg.From.FirstName}!\n\U0001F916 Меня зовут <u>{client.GetMeAsync().Result.FirstName}</u>, я телеграмм бот! ", parseMode: ParseMode.Html);
                    await client.SendTextMessageAsync(msg.Chat.Id, text: $"\U0001F310 Для вызова меню отправьте <b>/help</b>.", parseMode: ParseMode.Html);
                    await client.SendTextMessageAsync(msg.Chat.Id, text: $"\U00002139 Для начала работы <b>отправьте мне номер скважины</b>.", parseMode: ParseMode.Html);
                    return;
                }

                if (msg.Text == "log" || msg.Text == "Log")
                {
                    if (msg.Chat.Id == 947161854)
                    {
                        await using Stream fileUserLog = System.IO.File.OpenRead(logUsers);
                        Message message = await client.SendDocumentAsync(msg.Chat.Id, document: new InputOnlineFile(content: fileUserLog, fileName: "LogUsers.json"));
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
                                new []{InlineKeyboardButton.WithCallbackData("\U0000231B История изменении", "version")},
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
                    case "version":
                        await client.SendPhotoAsync(callbackQuery.Message.Chat.Id,
                        photo: "https://raw.githubusercontent.com/LEN4R/WellModesBot/main/pic/pic_version.jpg",
                        caption: VersionText,
                        parseMode: ParseMode.Html);
                        break;
                    case "contact":
                        await сlient.SendContactAsync(callbackQuery.Message.Chat.Id,
                        phoneNumber: "+79678888663",
                        firstName: "Галиев",
                        lastName: "Ленар");
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
                        if (queryIndex.key == "МРП")
                        {
                            string? queryIndexinput = queryIndex.value.ToString();
                            bool mrpBool = int.TryParse(queryIndexinput, out var number);
                            if (mrpBool == true)
                                message.AppendLine($"{queryIndex.key}: {Int32.Parse(queryIndex.value.ToString()) + DateTime.Now.Day - 1} {queryIndex.metrics} на {DateTime.Now.ToString("dd.MM.yyyy")}");
                            else
                                message.AppendLine($"{queryIndex.key}: {queryIndex.value} {queryIndex.metrics} на 01.06.2022");
                        }
                        else if (queryIndex.key == "верх")
                        {
                            queryIndex.key = "Вверх. интер. перф.";
                            message.AppendLine($"{queryIndex.key}: {queryIndex.value} {queryIndex.metrics}");
                        }
                        else if (queryIndex.key == "низ")
                        {
                            queryIndex.key = "Нижн. интер. перф.";
                            message.AppendLine($"{queryIndex.key}: {queryIndex.value} {queryIndex.metrics}");
                        }
                        else
                        {
                            message.AppendLine($"{queryIndex.key}: {queryIndex.value} {queryIndex.metrics}");
                        }
                        break;
                    case OutputType.Number:
                        bool numbertwo = double.TryParse(queryIndex.value.ToString(), out var result);
                        if (numbertwo)
                        {
                            message.AppendLine($"{queryIndex.key}: {double.Parse(queryIndex.value.ToString()).ToString("#.##")} {queryIndex.metrics}");
                        }
                        else
                        {
                            message.AppendLine($"{queryIndex.key}: {queryIndex.value} {queryIndex.metrics}");
                        }
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
                worksheetsList.Add(ReadWorksheet(xlPackage, 0, new[] { (5, OutputType.Default),   // Месторождение
                                                                       (7, OutputType.Default),   // № скв
                                                                       (8, OutputType.Default),   // Куст
                                                                       (3, OutputType.Default),   // Цех
                                                                       (17, OutputType.Default),  // Диам. экспл. колон.
                                                                       (11, OutputType.Default),  // Объект разработки/пласт
                                                                       (14, OutputType.Default),  // верх
                                                                       (15, OutputType.Default),  // низ
                                                                       (16, OutputType.Number),  // Удл. на в.д.
                                                                       (18, OutputType.Default),  // Тек. забой
                                                                       (22, OutputType.Default),  // Марка насоса
                                                                       (23, OutputType.Default),  // Глубина насоса
                                                                       (33, OutputType.Default),  // МРП
                                                                       (34, OutputType.Default),  // Доп. оборуд.
                                                                       (38, OutputType.Default),  // Ндин
                                                                       (39, OutputType.Default),  // Рзат. при Ндин.
                                                                       (41, OutputType.Number),   // Рдин. на ТМС
                                                                       (51, OutputType.Default),  // Рпл. внк
                                                                       (64, OutputType.Default),  // Сост. на конец мес/
                                                                       (54, OutputType.Number),   // Qж.ф.
                                                                       (55, OutputType.Number),   // % воды
                                                                       (56, OutputType.Number),   // Qн.ф.
                                                                       })); //ТРДС
                worksheetsList.Add(ReadWorksheet(xlPackage, 1, new[] { (5, OutputType.Default),   // Месторождение
                                                                       (7, OutputType.Default),   // № скв
                                                                       (8, OutputType.Default),   // Куст
                                                                       (3, OutputType.Default),   // Цех
                                                                       (11, OutputType.Default),  // Блок 
                                                                       (12, OutputType.Default),  // Объект разработки
                                                                       (19, OutputType.Default),  // верх
                                                                       (20, OutputType.Default),  // низ
                                                                       (21, OutputType.Number),  // Удл. на в.д.
                                                                       (23, OutputType.Default),  // Иск. забой
                                                                       (24, OutputType.Default),  // Тек. забой
                                                                       (25, OutputType.Default),  // СЭ/Характер лифта
                                                                       (26, OutputType.Default),  // Длина подвески НКТ
                                                                       (30, OutputType.Default),  // Глубина пакера
                                                                       (33, OutputType.Default),  // Доп.оборуд. (длина хвост.)
                                                                       (113, OutputType.Default), // МРП
                                                                       (41, OutputType.Default),  // Рзаб. ВНК.
                                                                       (48, OutputType.Default),  // Рпл. внк
                                                                       (45, OutputType.Default),  // Нст.
                                                                       (44, OutputType.Default),  // Руст. стат.
                                                                       (54, OutputType.Number),  // Q
                                                                       (38, OutputType.Default),  // Pл.
                                                                       (34, OutputType.Default),  // Dшт.
                                                                       (115, OutputType.Default), // Потребная закачка
                                                                       })); //ТРНС

                _worksheetsList = worksheetsList;
                _allFields = worksheetsList.SelectMany(x => x.Fields).ToList();
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
                var numberCell = myWorksheet.Cells[i, 9];
                var fieldNameCell = myWorksheet.Cells[i, 7];

                var number = numberCell.Value;
                var fieldName = fieldNameCell.Value;

                if (number == null || fieldName == null)
                    continue;

                var numberStr = number.ToString();
                var fieldNameStr = fieldName.ToString();

                if (string.IsNullOrWhiteSpace(numberStr) || string.IsNullOrWhiteSpace(fieldNameStr))
                    continue;

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
                worksheetFields.Add(fieldInfo);
            }

            return new WorksheetInfo(myWorksheet.Name, worksheetFields, worksheetFieldsCombined, requiredData, columnNames, columnMetrics);
        }
    }
}
