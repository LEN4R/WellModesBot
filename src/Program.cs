using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;

namespace WellModesBot
{
    class Program
    {
        private static string token = "5348869621:AAFeOl55384vMInbTORGsZwo9YVn-NoEv9w"; // token telegram
        private static TelegramBotClient client;
        private static List<string> _columnNames = new List<string>();
        private static List<FieldInfo> _allFields = new List<FieldInfo>();
        private static Dictionary<string, List<FieldInfo>> _fields = new Dictionary<string, List<FieldInfo>>();

        static void Main(string[] args)
        {
            GetData();
            client = new TelegramBotClient(token);
            client.StartReceiving();
            client.OnMessage += OnMessageHandler;
            Console.ReadLine();
            client.StopReceiving();
        }

        private static async void OnMessageHandler(object sender, MessageEventArgs e)
        {
            var msg = e.Message;
            if (msg.Text != null)
            {
                var message = new StringBuilder();

                switch (msg.Text)
                {
                    case "info":
                        msg = await client.SendTextMessageAsync(
                              chatId: msg.Chat.Id, text: $"Разработка: Галиев Ленар Разимович \nВерсия: 1.0.0 (Beta)",
                              replyToMessageId: msg.MessageId); 
                        msg = await client.SendStickerAsync(chatId: msg.Chat.Id, 
                              sticker: "https://tlgrm.ru/_/stickers/18f/4d5/18f4d57e-c910-3aef-9523-9a0d3bb60468/9.webp");
                    break;

                    default:
                        if (_fields.TryGetValue(msg.Text, out var list))
                        {
                            message.AppendLine();

                            for (int i = 0; i < list.Count; i++)
                                message.AppendLine($"{i + 1}. {list[i]}");

                            if (list.Count == 1)
                            {
                                PrintFieldDataByColumnIndexes(list[0], message, new[] { 5, 7, 8, 17, 11, 14, 15, 18, 22, 23, 33, 38, 39, 44, 51, 54, 55, 56 });
                            }

                            await SendMessage(msg, message.ToString());
                        }
                        else
                        {
                            var firstField = _allFields.FirstOrDefault(x => x.FullName.StartsWith(msg.Text, StringComparison.OrdinalIgnoreCase));

                            if (firstField != null)
                            {
                                PrintFieldDataByColumnIndexes(firstField, message, new[] { 5, 7, 8, 17, 11, 14, 15, 18, 22, 23, 33, 38, 39, 44, 51, 54, 55, 56 });
                                await SendMessage(msg, message.ToString());
                            }
                            else
                            {
                                await SendMessage(msg, "Введите номер скважины и начало названия месторождения (слитно!)");
                            }
                        }
                        break;
                }
            }
        }

        private static async Task SendMessage(Telegram.Bot.Types.Message msg, string message)
        {
            await client.SendTextMessageAsync(chatId: msg.Chat.Id, text: message,
                                              replyToMessageId: msg.MessageId);
        }

        private static void PrintFieldDataByColumnIndexes(FieldInfo field, StringBuilder message, int[] indexes)
        {
            var query = _columnNames
                .Select((x, i) =>
                {
                    return (key: x, value: field.Data[i]);
                })
                .Where(x =>
                {
                    return x.key != null;
                }).ToArray();

            foreach (var index in indexes)
            {
                message.AppendLine($"{query[index].key}: {query[index].value}");
            }
        }

        public static void GetData()
        {
            var path = @"ТРДС.xlsx";
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var xlPackage = new ExcelPackage(new FileInfo(path)))
            {
                var myWorksheet = xlPackage.Workbook.Worksheets.First(); //select sheet here

                var totalRows = myWorksheet.Dimension.End.Row;
                var totalColumns = myWorksheet.Dimension.End.Column;


                var row = myWorksheet.Row(1);

                for (int k = 2; k <= totalColumns; k++)
                {
                    _columnNames.Add(myWorksheet.Cells[14, k].Value?.ToString() ?? myWorksheet.Cells[13, k].Value?.ToString());
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

    internal class FieldInfo
    {
        public string Number { get; internal set; }
        public string FieldName { get; internal set; }
        public int RowIndex { get; internal set; }
        public List<object> Data { get; internal set; }
        public string FullName => Number + FieldName;
        public override string ToString()
        {
            return FullName;
        }
    }
}
