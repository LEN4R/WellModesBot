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
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;

namespace WellModesBot
{
    class Program
    {
        private static string token = "5348869621:AAFeOl55384vMInbTORGsZwo9YVn-NoEv9w";
        private static string Url = "https://v2.d-f.pw/app/application/6310/";
        private static TelegramBotClient client;
        private static List<string> _columnNames = new List<string>();
        private static List<string> _columnMetrics = new List<string>();
        private static List<FieldInfo> _allFields = new List<FieldInfo>();
        private static Dictionary<string, List<FieldInfo>> _fields = new Dictionary<string, List<FieldInfo>>();
        private static readonly int[] RequiredData = new[] { 5, 7, 8, 3, 17, 11, 14, 15, 18, 22, 23, 33, 34, 38, 39, 51, 54, 55, 56 };


        static async void Main(string[] args)
        {
            GetData();
            client = new TelegramBotClient(token); // Токен бота
            using var cts = new CancellationTokenSource(); // Токен отмены
            var receiverOptions = new ReceiverOptions{ AllowedUpdates = { }}; // Настройка получении обновлени

            client.StartReceiving(HandleUpdatesAsync, HandleErrorAsync, receiverOptions, cancellationToken: cts.Token); // Функция получении обновлении от Telegram
            
            // Проверка на запуск
            var me = await client.GetMeAsync();
            Console.WriteLine($"Bot_id: {me.Id} \nBot_Name: {me.FirstName}");
            Console.ReadLine();
            cts.Cancel();


            // Метод обработки обновление бота
            async Task HandleUpdatesAsync(ITelegramBotClient сlient, Update update, CancellationToken cancellationToken)
            {
                if(update.Type == UpdateType.Message && update?.Message?.Text != null)
                {
                    await HandleMessage(сlient, update.Message);
                    return;
                }

                if(update.Type == UpdateType.CallbackQuery)
                {
                    await HandleCallbackQuery(сlient, update.CallbackQuery);
                    return;
                }

            }

            // Метод обработки сообщении бота
            async Task HandleMessage(ITelegramBotClient сlient, Message msg)
            {
                if (msg.Text == null)
                    return;

                InlineKeyboardMarkup markup = null;

                if (msg.Text == "menu" || msg.Text == "Menu" || msg.Text == "Меню" || msg.Text == "меню")
                {
                    markup = new InlineKeyboardMarkup(
                        new[]
                        {
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

                if (_fields.TryGetValue(msg.Text, out var list))
                {
                    var message = new StringBuilder();
                    if (list.Count > 1)
                    {
                        message.Append("\U0001F50E Пожалуйста, выберите скважину:");
                        markup = new InlineKeyboardMarkup(list.Select(x => new[]
                        {
                        InlineKeyboardButton.WithCallbackData(x.FullName, _allFields.IndexOf(x).ToString())
                    }).ToArray());
                    }
                    else
                    {
                        PrintFieldDataByColumnIndexes(list[0], message, RequiredData);
                    }

                    await SendMessage(msg.Chat.Id, message.ToString(), markup: markup);
                }
                else
                {
                    await SendFieldInfoByName(msg.Text, msg.Chat.Id, RequiredData);
                }


                async Task SendFieldInfoByName(string name, long chatId, int[] requiredData)
                {
                    var message = new StringBuilder();
                    var firstField = _allFields.FirstOrDefault(x => x.FullName.StartsWith(name, StringComparison.OrdinalIgnoreCase));
                    if (firstField != null)
                    {
                        PrintFieldDataByColumnIndexes(firstField, message, requiredData);
                        await SendMessage(chatId, message.ToString());
                    }
                    else
                    {
                        await SendMessage(chatId, "\U000026A0 Такой скважины нет!");
                    }
                }

            }


            async Task HandleCallbackQuery(ITelegramBotClient сlient, CallbackQuery callbackQuery)
            {

                //var chatId = ev.CallbackQuery.From.Id;
                var data = callbackQuery.Data;

                switch (data)
                {
                    case "info":
                        await client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, text: $"Дата создания бота: 19.04.2022\nТекущая версия: 1.0.2");
                        await client.SendStickerAsync(callbackQuery.Message.Chat.Id, sticker: "https://tlgrm.ru/_/stickers/18f/4d5/18f4d57e-c910-3aef-9523-9a0d3bb60468/9.webp");
                        break;
                    case "version":
                        await client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, text: 
                                                  $"[21.04.2022 Версия: 1.0.0 (Beta)] \n * Добавлена возможность вывода скважин с разными месторождениями. \n\n" +
                                                  $"[23.04.2022 Версия: 1.0.1] \n * При выводе данных добавлены единицы изменерия. \n * Убран баг вывода при вводе info \n * Убран баг некорректного вывода скважин (555) \n\n " +
                                                  $"[25.04.2022 Версия: 1.0.2] \n * Все текстовые команды переписаны в меню (команда: menu). \n * Вывод скважин с разными местородения в качестве кнопок.");

                        await client.SendStickerAsync(callbackQuery.Message.Chat.Id,
                              sticker: "https://cdn.tlgrm.app/stickers/18f/4d5/18f4d57e-c910-3aef-9523-9a0d3bb60468/192/3.webp");
                        break;
                    default:
                        await SendFieldInfoByIndex(int.Parse(data), callbackQuery.Message.Chat.Id, RequiredData);
                        break;
                }
            }


            // Обработка ошибок бота
            Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
            {
                var ErrorMessage = exception switch
                {
                    ApiRequestException apiRequestException => $"Ошибка Telegram Api: {apiRequestException.ErrorCode}",
                    _ => exception.ToString()
                };
                Console.WriteLine(ErrorMessage);
                return Task.CompletedTask;
            }
        }

        private static async Task SendFieldInfoByIndex(int index, long chatId, int[] requiredData)
        {
            var firstField = _allFields[index];
            var message = new StringBuilder();
            PrintFieldDataByColumnIndexes(firstField, message, requiredData);
            await SendMessage(chatId, message.ToString());
        }

        private static async Task SendMessage(long chatId, string message, int replyMessageId = 0, InlineKeyboardMarkup markup = null)
        {
            await client.SendTextMessageAsync(chatId: chatId, text: message, replyToMessageId: replyMessageId, replyMarkup: markup);
        }

        private static void PrintFieldDataByColumnIndexes(FieldInfo field, StringBuilder message2, int[] indexes)
        {
            var query = _columnNames
                .Select((x, i) =>
                {
                    return (key: x, value: field.Data[i], metrics: _columnMetrics[i]);
                })
                .Where(x =>
                {
                    return x.key != null;
                }).ToArray();

            foreach (var index in indexes)
            {
                message2.AppendLine($"{query[index].key}: {query[index].value} {query[index].metrics}");
            }
        }

        public static void GetData()
        {
            var path = @"ТРДС.xlsx";
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var xlPackage = new ExcelPackage(new FileInfo(path)))
            {
                var myWorksheet = xlPackage.Workbook.Worksheets.First();
                var totalRows = myWorksheet.Dimension.End.Row;
                var totalColumns = myWorksheet.Dimension.End.Column;
                var metrics = myWorksheet.Dimension.End.Column;

                var row = myWorksheet.Row(1);

                for (int k = 2; k <= totalColumns; k++)
                {
                    _columnNames.Add(myWorksheet.Cells[14, k].Value?.ToString() ?? myWorksheet.Cells[13, k].Value?.ToString());
                    _columnMetrics.Add(myWorksheet.Cells[15, k].Value?.ToString());
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

                    if (!_fields.TryGetValue(numberStr.ToLowerInvariant(), out List<FieldInfo> list))
                        list = _fields[numberStr] = new List<FieldInfo>();

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
                        Data = data
                    };

                    list.Add(fieldInfo);
                    _allFields.Add(fieldInfo);
                }
            }
        }
    }
}
