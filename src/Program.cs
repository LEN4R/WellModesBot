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
    }
    class Program
    {
        private static string token = "5348869621:AAFeOl55384vMInbTORGsZwo9YVn-NoEv9w";
        public static readonly string InfoStart = $"\U0000270C Добро пожаловать в WellModesBot! \n\U0000270F Для начала работы введите номер скважины.\n\n\U00002139 Вызов меню по команде: [Меню] & [Menu].";
        private static readonly string InstructionText = $"\U00000031\U000020E3 Введите номер скважины, например: '123'. WMB выведит информацию по скважине.\n" +
                                                         $"\U00000032\U000020E3 Если номер скважины совпадает в нескольких месторождении, то WMB предложит выбор между месторождениями.\n\n" +
                                                         $"\U00002139 Возможен ввод номер скважины+месторождение, например: если нужно 123 Равенское месторождение, то вводим: '123Р' или '123р' или '123Ра' и т.д 	\n\U000025B6 бот не привязан к регистру!\U0001F4AA \n\n " +
                                                         $"\U000026A0 Бот не понимает звездочки '*', для получения информации скважин с индексом, неодходимо ввеcти Индекс!";
        private static readonly string VersionText = $"[21.04.2022 Версия: 1.0.0 (Beta)] \n \U000025AA Добавлена возможность вывода скважин с разными месторождениями. \n\n" +
                                                     $"[23.04.2022 Версия: 1.0.1] \n \U000025AA При выводе данных добавлены единицы изменерия. \n \U000025AA Убран баг некорректного вывода скважин. \n\n " +
                                                     $"[25.04.2022 Версия: 1.0.1.1] \n \U000025AA Все текстовые команды переписаны в меню (команда: menu). \n \U000025AA Вывод скважин с разными местородения в качестве кнопок. \n\n " +
                                                     $"[28.04.2022 Версия: 1.0.2] \n \U000025AA Код полностью переписан под API.TelegramBot v17. Также бот залит на хостинг (доступен 24/7).";
        private static readonly string InfoText = $"\U0001F4C5 Дата создания бота: 20.04.2022\n\U0001F4BBТекущая версия бота: 1.0.2\n\U0001F4BEБаза данных от 04.2022";
      
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

            var receiverOptions = new ReceiverOptions { AllowedUpdates = new[]{ UpdateType.CallbackQuery, UpdateType.Message } };  // Настройка получении обновлени
            client.StartReceiving(HandleUpdatesAsync, HandleErrorAsync, receiverOptions, cancellationToken: cts.Token); // Функция получении обновлении от Telegram
            
            // Проверка на запуск
            var me = client.GetMeAsync().Result;
            Console.WriteLine($"Bot_id: {me.Id} \nBot_Name: {me.FirstName}");
            Console.ReadLine();
            cts.Cancel();

            //Запись всех обновлении бота
            try
            {
                var botUpdatesString = System.IO.File.ReadAllText(logUsers);
                botUpdate = JsonConvert.DeserializeObject<List<BotUpdate>>(botUpdatesString) ?? botUpdate;
            }
            catch(Exception ex)
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
                        messageText = update.Message.Text,
                        chatId = update.Message.Chat.Id,
                        username = update.Message.Chat.Username
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

                if(update.Message!.Type != MessageType.Text)
                    return;

                //Переменные
                /*
                chatId = update.Message.Chat.Id;
                messageText = update.Message.Text;
                messageId = update.Message.MessageId;
                username = update.Message.Chat.Username;
                firstName = update.Message.From.LastName;
                lastName = update.Message.From.LastName;
                Id = update.Message.From.Id;
                */
            }

            // Метод обработки сообщении бота
            async Task HandleMessage(ITelegramBotClient сlient, Message msg)
            {
                if (msg.Text == null)
                    return;

                InlineKeyboardMarkup markup = null;

                if (msg.Text == "test")
                {
                    await client.SendPhotoAsync(
                    msg.Chat.Id,
                    photo: "https://github.com/LEN4R/WellModesBot/blob/main/pic_instruction.png?raw=true",
                    caption: "<b>Ara bird</b>. <i>Source</i>: <a href=\"https://pixabay.com\">Pixabay</a>",
                    parseMode: ParseMode.Html);
                return;
                }

                if (msg.Text == "/start")
                {
                    await client.SendTextMessageAsync(msg.Chat.Id, text: InfoStart);
                    return;
                }
                else
                {
                    if (msg.Text == "menu" || msg.Text == "Menu" || msg.Text == "Меню" || msg.Text == "меню")
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

                    //Поиск скважин
                    //var ex1 = await Finder(_worksheetsList[0], msg, markup);
                    //var ex2 = await Finder(_worksheetsList[1], msg, markup);
                    //var allEx = _worksheetsList[0].Concat(_worksheetsList[1])

                    await ProcessMessage(msg, markup);

                    #region комментарии
                    /*if (ex1.Item1)
                    {
                        if (ex2.Item1)
                        {
                            if(ex1.Item3 != null && ex1.Item3 != null)
                            {

                            }
                            else
                            {

                            }
                        }
                    }
                    else
                    {
                        if(!ex2.Item1)
                            await SendMessage(msg.Chat.Id, "\U000026A0 ОШИБКА! Такой скважины нет!");
                    }*/

                    //if (!fff && !fff1)
                    //    await SendMessage(msg.Chat.Id, "\U000026A0 ОШИБКА! Такой скважины нет!");
                    #endregion 
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
                        markup = new InlineKeyboardMarkup(wellsList.Select(x =>
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(_worksheetsList[x.WorksheetNumber].Name + " " + x.FullName, _allFields.IndexOf(x).ToString())
                        }).ToArray());
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
                        await client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, text: InstructionText);
                        break;
                    case "info":
                        await client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, text: InfoText);
                        //await client.SendAnimationAsync(callbackQuery.Message.Chat.Id, animation: "https://www.dropbox.com/s/byuc1uuhn6kpcvz/pic_info.gif?dl=0", caption: "Waves");
                        break;
                    case "version":
                        await client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, text: VersionText);
                        //await client.SendStickerAsync(callbackQuery.Message.Chat.Id, sticker: "https://cdn.tlgrm.app/stickers/18f/4d5/18f4d57e-c910-3aef-9523-9a0d3bb60468/192/3.webp");
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
                    ApiRequestException apiRequestException => $"Ошибка Telegram Api: {apiRequestException.ErrorCode}",
                    _ => exception.ToString()
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

            foreach (var index in info.RequiredData)
            {
                message.AppendLine($"{query[index].key}: {query[index].value} {query[index].metrics}");
            }
        }

        public static void GetData()
        {
            var path = Directory.EnumerateFiles(Environment.CurrentDirectory).FirstOrDefault(x => x.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase));
            Console.WriteLine($"Файл загружен:{path}");
            //var path = @"ТРДС.xlsx";
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var xlPackage = new ExcelPackage(new FileInfo(path)))
            {
                var worksheetsList = new List<WorksheetInfo>();

                worksheetsList.Add(ReadWorksheet(xlPackage, 1, new[] { 5, 7, 8, 3, 17, 11, 14, 15, 18, 22, 23, 33, 34, 38, 39, 41, 51, 64, 54, 55, 56 }));
                worksheetsList.Add(ReadWorksheet(xlPackage, 2, new[] { 5, 7, 8, 3, 11, 12, 19, 20, 23, 24, 25, 26, 30, 33, 41, 48, 45, 44, 54, 38, 34 }));

                _worksheetsList = worksheetsList;

                _allFields = worksheetsList.SelectMany(x => x.Fields).ToList();
                _allFieldsCombined = worksheetsList.SelectMany(x => x.FieldsCombined)
                    .GroupBy(x => x.Key)
                    .Select(x => (x.Key, x.SelectMany(y => y.Value)))
                    .ToDictionary(x => x.Key, x => x.Item2.ToList());
            }
        }

        private static WorksheetInfo ReadWorksheet(ExcelPackage xlPackage, int worksheetIndex, int[] requiredData)
        {
            var worksheetFields = new List<FieldInfo>();
            var worksheetFieldsCombined = new Dictionary<string, List<FieldInfo>>();
            var columnNames = new List<string>();
            var columnMetrics = new List<string>();

            var myWorksheet = xlPackage.Workbook.Worksheets[worksheetIndex];
            var totalRows = myWorksheet.Dimension.End.Row;
            var totalColumns = myWorksheet.Dimension.End.Column;
            //var metrics = myWorksheet.Dimension.End.Column;
            //var row = myWorksheet.Row(1);

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
                    WorksheetNumber = worksheetIndex - 1
                };

                list.Add(fieldInfo);
                worksheetFields.Add(fieldInfo);

            }

            return new WorksheetInfo(myWorksheet.Name, worksheetFields, worksheetFieldsCombined, requiredData, columnNames, columnMetrics);
        }
    }
}
