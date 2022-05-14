using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace WellModesBot
{
    struct BotUpdate
    {
        public string messageText;
        public long chatId;
        public string? username;
        public string firstname;
        public string lastname;
    }
    class Program
    {
        private static string token = "5348869621:AAFeOl55384vMInbTORGsZwo9YVn-NoEv9w"; //WellModesBot
        //private static string token = "5333261863:AAEm0hBmW13UOuu2weGKZqlSHx3Nk7-4tlg"; // TestLEN4RBot

        private static readonly string InstructionText = $"\U00000031\U000020E3 Введите номер скважины, бот выведит данные по скважине.\n" +
                                                         $"\U00000032\U000020E3 Если номер скважины совпадает в нескольких месторождениях, то бот предложит выбор выгрузки данных.\n\n" +
                                                         $"\U00002139 Для бювстрого вывода даннвх возможен ввод: [номер скважины]+[начало месторождения]. \n\U000025B6 <b>Бот не привязан к регистру!</b>\U0001F4AA \n\n " +
                                                         $"\U000026A0 Бот не воспринимает '*', для получения информации скважин с индексом, неодходимо ввеcти <b>Индекс!</b>";

        private static readonly string VersionText = $"<b>[21.04.2022 Версия: 1.0.0 (Beta)]</b> \n \U000025AA Добавлена возможность вывода скважин с разными месторождениями. \n\n" +
                                                     $"<b>[23.04.2022 Версия: 1.0.1]</b> \n \U000025AA При выводе данных добавлены единицы изменерия. \n \U000025AA Убран баг некорректного вывода скважин. \n\n " +
                                                     $"<b>[25.04.2022 Версия: 1.0.1.1]</b> \n \U000025AA Все текстовые команды переписаны в меню. \n \U000025AA Вывод скважин с разными местородениями в качестве кнопок. \n\n " +
                                                     $"<b>[28.04.2022 Версия: 1.0.2]</b> \n \U000025AA Код полностью переписан под API.TelegramBot v17. WellModesBot адаптирован под хостинг, теперь доступен 24/7.  \n\n " +
                                                     $"<b>[03.05.2022 Версия: 1.0.3]</b> \n \U000025AA Добавлена поддержка нагнетательных скважин.";

        private static readonly string InfoText = $"\U0001F4C5 Дата создания бота: <b>20.04.2022</b>\n" +
                                                  $"\U0001F4BB Текущая версия бота: <b>1.0.3</b>\n" +
                                                  $"\U0001F4BE База данных от <b>05.2022</b>\n\n \U0000270F Разработка:\n<b><i>\U0001F518LEN4R\n\U0001F518elemaunt\n\U0001F518Favelin</i></b>";

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
                    var _botUpdate = new BotUpdate
                    {
                        chatId = update.Message.Chat.Id,
                        messageText = update.Message.Text,
                        username = update.Message.Chat.Username,
                        firstname = update.Message.From.FirstName,
                        lastname = update.Message.From.LastName,
                    };
                    botUpdate.Add(_botUpdate);
                    var botUpdatesString = JsonConvert.SerializeObject(botUpdate);
                    System.IO.File.WriteAllText(logUsers, botUpdatesString);
                    return;
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
                    /*caption: "text";*/ parseMode: ParseMode.Html);
                    await client.SendTextMessageAsync(msg.Chat.Id, text: $"\U0001F44B Здравствуйте {msg.From.LastName} {msg.From.FirstName}!\n\U0001F916 Меня зовут <u>WellModesBot</u>, я телеграмм бот!", parseMode: ParseMode.Html);
                    await client.SendTextMessageAsync(msg.Chat.Id, text: $"\U00002139 Для начала работы <b>отправьте мне номер скважины</b>.", parseMode: ParseMode.Html);
                    await client.SendTextMessageAsync(msg.Chat.Id, text: $"\U0001F310 Чтобы вызвать меню, отправьте <b>'/help'</b>.", parseMode: ParseMode.Html);
                    return;
                }
                else
                {
                    if (msg.Text == "menu" || msg.Text == "Menu" || msg.Text == "Меню" || msg.Text == "меню" || msg.Text == "/help" || msg.Text == "help" || msg.Text == "Help")
                    {
                        markup = new InlineKeyboardMarkup(
                            new[]
                            {
                                new []{InlineKeyboardButton.WithCallbackData("\U00002755 Инструкция по боту", "instruction")},
                                new []{InlineKeyboardButton.WithCallbackData("\U00002139 Информация о боте", "info")},
                                new []{InlineKeyboardButton.WithCallbackData("\U0000231B История изменении", "version")},
                                new []
                                {
                                    InlineKeyboardButton.WithUrl("\U0000270D Обратная связь","https://t.me/len4r"),
                                    InlineKeyboardButton.WithUrl("\U00002709 VK.com","https://vk.com/len4r")
                                }
                            });
                        await SendMessage(msg.Chat.Id, "\U00002705 Выберите опцию:", markup: markup);
                        return;
                    }
                    await ProcessMessage(msg, markup);
                }
            }

            async Task ProcessMessage(Message msg, InlineKeyboardMarkup markup)
            {
                if (_allFieldsCombined.TryGetValue(msg.Text, out var wellsList))
                {
                    var message = new StringBuilder();
                    if (wellsList.Count > 1)
                    {
                        message.Append("\U0001F50E Пожалуйста, выберите скважину:");
                        markup = new InlineKeyboardMarkup(wellsList.Select(x => new[] { InlineKeyboardButton.WithCallbackData(_worksheetsList[x.WorksheetNumber].Name + " " + x.FullName, _allFields.IndexOf(x).ToString()) }).ToArray());
                    }
                    else
                    {
                        PrintFieldDataByColumnIndexes(wellsList[0], message, _worksheetsList[wellsList[0].WorksheetNumber]);
                    }
                    await SendMessage(msg.Chat.Id, message.ToString(), markup: markup);
                }
                else
                {
                    var message = new StringBuilder();
                    var firstField = _allFields.FirstOrDefault(x => x.FullName.StartsWith(msg.Text, StringComparison.OrdinalIgnoreCase));

                    if (firstField != null)
                    {
                        PrintFieldDataByColumnIndexes(firstField, message, _worksheetsList[firstField.WorksheetNumber]);
                        await SendMessage(msg.Chat.Id, message.ToString());
                    }
                    else
                    {
                        await SendMessage(msg.Chat.Id, "\U000026A0 ОШИБКА! Такой скважины нет!");
                    }
                }
            }

            async Task HandleCallbackQuery(ITelegramBotClient сlient, CallbackQuery callbackQuery)
            {
                var data = callbackQuery.Data;

                switch (data)
                {
                    case "instruction":
                        //await client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, text: InstructionText);
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
                    default:
                        await SendFieldInfoByIndex(int.Parse(data), callbackQuery.Message.Chat.Id);
                        break;
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
                        message.AppendLine($"{queryIndex.key}: {queryIndex.value} {queryIndex.metrics}");
                        break;
                    case OutputType.Number:
                        message.AppendLine($"{queryIndex.key}: {double.Parse(queryIndex.value.ToString()).ToString("#.##")} {queryIndex.metrics}");
                        break;
                    default:
                        break;
                }
            }
        }

        public static void GetData()
        {
            //var path = Directory.EnumerateFiles(Environment.CurrentDirectory).FirstOrDefault(x => x.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase));
            var path = @"Info_05_2022.xlsx";
            var locationWellsDoc = @"locationOfWells.xlsx";
            Console.WriteLine($"Файл загружен:{path} + {locationWellsDoc}");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var xlPackage = new ExcelPackage(new FileInfo(path)))
            {
                var worksheetsList = new List<WorksheetInfo>();

                worksheetsList.Add(ReadWorksheet(xlPackage, 0, new[] { (5, OutputType.Default), 
                                                                       (7, OutputType.Default),
                                                                       (8, OutputType.Default),
                                                                       (3, OutputType.Default),
                                                                       (17, OutputType.Default),
                                                                       (11, OutputType.Default),
                                                                       (14, OutputType.Default),
                                                                       (15, OutputType.Default),
                                                                       (18, OutputType.Default),
                                                                       (22, OutputType.Default),
                                                                       (23, OutputType.Default),
                                                                       (33, OutputType.Default),
                                                                       (34, OutputType.Default),
                                                                       (38, OutputType.Default),
                                                                       (39, OutputType.Default),
                                                                       (41, OutputType.Default),
                                                                       (51, OutputType.Default),
                                                                       (64, OutputType.Default),
                                                                       (54, OutputType.Number),
                                                                       (55, OutputType.Number),
                                                                       (56, OutputType.Number)
                                                                      })); //ТРДС
                worksheetsList.Add(ReadWorksheet(xlPackage, 1, new[] { (5, OutputType.Default),
                                                                       (7, OutputType.Default),
                                                                       (8, OutputType.Default),
                                                                       (3, OutputType.Default),
                                                                       (11, OutputType.Default),
                                                                       (12, OutputType.Default),
                                                                       (19, OutputType.Default),
                                                                       (20, OutputType.Default),
                                                                       (23, OutputType.Default),
                                                                       (24, OutputType.Default),
                                                                       (25, OutputType.Default),
                                                                       (26, OutputType.Default),
                                                                       (30, OutputType.Default),
                                                                       (33, OutputType.Default),
                                                                       (113, OutputType.Default),
                                                                       (41, OutputType.Default),
                                                                       (48, OutputType.Default),
                                                                       (45, OutputType.Default),
                                                                       (44, OutputType.Default),
                                                                       (54, OutputType.Number),
                                                                       (38, OutputType.Default),
                                                                       (34, OutputType.Default),
                                                                       (115, OutputType.Number),
                                                                       })); //ТРНС

                _worksheetsList = worksheetsList;
                _allFields = worksheetsList.SelectMany(x => x.Fields).ToList();
                _allFieldsCombined = worksheetsList.SelectMany(x => x.FieldsCombined)
                    .GroupBy(x => x.Key)
                    .Select(x => (x.Key, x.SelectMany(y => y.Value)))
                    .ToDictionary(x => x.Key, x => x.Item2.ToList());
            }

            /*
            using (var xlPackage = new ExcelPackage(new FileInfo(locationWellsDoc)));
            {
                Dictionary<string, Action<string, locationWellsDoc>> MapFieldSetters { get; set; } =  new Dictionary<string, Action<string, locationWellsDoc>>()
                {
                    { "First Name", (s,g) => g.cnst_first_nm = s },
                    { "Last Name", (s,g) => g.cnst_Last_nm = s },
                };
            }
            */
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

                if (!worksheetFieldsCombined.TryGetValue(numberStr.ToLowerInvariant(), out List<FieldInfo> list))
                    list = worksheetFieldsCombined[numberStr] = new List<FieldInfo>();

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
                    WorksheetNumber = worksheetIndex // -1
                };

                list.Add(fieldInfo);
                worksheetFields.Add(fieldInfo);
            }

            return new WorksheetInfo(myWorksheet.Name, worksheetFields, worksheetFieldsCombined, requiredData, columnNames, columnMetrics);
        }

    }
}
