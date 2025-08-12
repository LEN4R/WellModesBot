using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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
        private static string token = File.ReadAllText("wmbot.txt");
        private static readonly string InstructionText = $"\U00000031\U000020E3 Введите номер скважины, бот выводит режимные данные.\n" +
                                                         $"\U00000032\U000020E3 Если номер скважины совпадает в нескольких месторождении, бот предложит выбор скважин.\n\n" +
                                                         $"\U00002139 Для быстрого вывода данных возможен ввод: [номер скважины]+[начало названия месторождения]. \n\U000025B6 " +
                                                         $"<b>Бот не привязан к регистру!</b>\U0001F4AA \n\n " +
                                                         $"\U000026A0 Бот не воспринимает '*', для получения информации скважин с индексом, необходимо ввести <b>Индекс!</b>";

        private static readonly string InfoText =        $"\U0001F4C5 Дата обновления бота: <b>10.08.2025</b>\n" +
                                                         $"\U0001F4BB Версия бота: <b>1.2</b>\n";

        private static ITelegramBotClient client;

        // === Конфигурация ===
        static string logUsers = Path.Combine(AppContext.BaseDirectory, "LogUsers.json");
        static string userList = @"users.txt";
        static string rootList = @"root.txt";
        static List<BotUpdate> botUpdate = new List<BotUpdate>();
        private static List<WorksheetInfo> _worksheetsList;
        private static List<FieldInfo> _allFields;
        private static Dictionary<string, List<FieldInfo>> _allFieldsCombined;
        string god = "947161854";

        static void Main(string[] args)
        {
            GetData();
            // ====== ИНИЦИАЛИЗАЦИЯ БОТА ======
            client = new TelegramBotClient(token);
        
            using var cts = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.CallbackQuery, UpdateType.Message]
            };

            client.StartReceiving(HandleUpdatesAsync, HandleErrorAsync, receiverOptions, cancellationToken: cts.Token);

            // Проверка на запуск
            var me = client.GetMe().Result;                                                         // Токен бота
            Console.WriteLine($"Bot ID: {me.Id} \nName: {me.FirstName}");                           // Токен отмены
            Console.ReadLine();                                                                     // Продолжайте запускать приложение до тех пор, пока не будет нажата клавиша
            cts.Cancel();                                                                           // Отправьте запрос на отмену, чтобы остановить бота

            //Запись всех обновлении бота
            try
            {
                var botUpdatesString = File.ReadAllText(logUsers);
                botUpdate = JsonConvert.DeserializeObject<List<BotUpdate>>(botUpdatesString) ?? botUpdate;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка десериализации обновлении бота {ex}");
            }
        }

        static async Task SendMessage(ITelegramBotClient botClient, long chatId, string text, ParseMode parseMode = ParseMode.Html, ReplyMarkup markup = null)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: parseMode,
                replyMarkup: markup
            );
        }

        // Метод обработки обновление бота
        private static async Task HandleUpdatesAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Загружаем список пользователей
            var listOfUsers = File.ReadAllLines(userList)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToHashSet();

            // Загружаем список администраторов
            var listOfRoot = File.ReadAllLines(rootList)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToHashSet();

            // Логирование действий (кроме твоего собственного ID)
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                if (update.Message.Chat.Id != 947161854)
                {
                    var timeZoneHourEkb = (update.Message.Date.Hour + 5) % 24;
                    var _botUpdate = new BotUpdate
                    {
                        id = update.Message.Chat.Id,
                        data = $"{update.Message.Date.Day}.{update.Message.Date.Month}.{update.Message.Date.Year} {timeZoneHourEkb}:{update.Message.Date.Minute:D2}",
                        text = update.Message.Text,
                        username = $"{update.Message.Chat.Username} {update.Message.From.FirstName} {update.Message.From.LastName}"
                    };

                    botUpdate.Add(_botUpdate);
                    var botUpdatesString = JsonConvert.SerializeObject(botUpdate, Formatting.Indented);
                    File.WriteAllText(logUsers, botUpdatesString);
                }
            }

            // === Обработка нажатий кнопок ===
            if (update.Type == UpdateType.CallbackQuery)
            {
                var callback = update.CallbackQuery;

                if (callback.Data == "regNewUser")
                {
                    string requestMessage =
                        $"📩 Запрос на доступ!\n" +
                        $"👤 Пользователь: {callback.From.LastName} {callback.From.FirstName}\n" +
                        $"🆔 Telegram ID: {callback.From.Id}";

                    var approveButton = new InlineKeyboardMarkup(new[]
                    {
            new[] { InlineKeyboardButton.WithCallbackData("✅ Добавить пользователя", $"reg_{callback.From.Id}") }});

                    foreach (var rootUserId in listOfRoot)
                    {
                        if (long.TryParse(rootUserId, out long chatId))
                            await botClient.SendMessage(chatId, requestMessage, parseMode: ParseMode.Html, replyMarkup: approveButton, cancellationToken: cancellationToken);
                    }

                    await botClient.SendMessage(
                        callback.Message.Chat.Id,
                        "✅ Ваш запрос отправлен администраторам. Ожидайте подтверждения.",
                        cancellationToken: cancellationToken
                    );
                }
                else if (callback.Data.StartsWith("reg_"))
                {
                    var regUserId = callback.Data.Replace("reg_", "");
                    if (long.TryParse(regUserId, out _))
                    {
                        File.AppendAllText(userList, Environment.NewLine + regUserId);
                        await botClient.SendMessage(callback.Message.Chat.Id, $"➕ Добавлен новый пользователь: {regUserId}");
                        await botClient.SendMessage(947161854, $"➕ Добавлен новый пользователь: {regUserId}");
                    }
                    else
                    {
                        await botClient.SendMessage(callback.Message.Chat.Id, "⚠ Неверный формат ID.");
                    }
                }
                else
                {
                    // Обрабатываем другие callback-и
                    await HandleCallbackQuery(callback);
                }
                return;
            }



            // === Обработка текстовых сообщений ===
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                var msg = update.Message;
                var text = msg.Text.Trim();
                // /start
                if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
                {
                    await botClient.SendPhoto(
                        chatId: msg.Chat.Id,
                        photo: "https://raw.githubusercontent.com/LEN4R/WellModesBot/main/pic/logo.jpg",
                        caption: $"\U0001F44B Здравствуйте {msg.From.LastName} {msg.From.FirstName}!\n\U0001F916 Меня зовут <u>{(await botClient.GetMe()).FirstName}</u>, я телеграмм бот!",
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken
                    );
                    await SendMessage(botClient, msg.Chat.Id, "\U00002139 Для начала работы <b>отправьте мне номер скважины</b>.", ParseMode.Html);
                    return;
                }

                // Если пользователя нет в списке
                if (!listOfUsers.Contains(msg.Chat.Id.ToString()) && !listOfRoot.Contains(msg.Chat.Id.ToString()))
                {
                    var keyboard = new InlineKeyboardMarkup(
                    [
                        [InlineKeyboardButton.WithCallbackData("\U0001F194 Запросить доступ?", "regNewUser")]
                    ]);
                    await SendMessage(botClient, msg.Chat.Id, "\U0000274C <b>ОШИБКА!</b> У вас нет доступа!", ParseMode.Html, keyboard);
                    return;
                }

                // Общие команды
                if (text.Equals("menu", StringComparison.OrdinalIgnoreCase) || text.Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    var markup = new InlineKeyboardMarkup(
                    [
                        [InlineKeyboardButton.WithCallbackData("\U00002755 Инструкция по боту", "instruction")],
                        [InlineKeyboardButton.WithCallbackData("\U00002139 Информация по боту", "info")],
                        [InlineKeyboardButton.WithCallbackData("\U0001F194 Узнать Telegram ID", "telegramID")]
                    ]);

                    await SendMessage(botClient, msg.Chat.Id, "\U00002705 Пожалуйста, выберите опцию:", ParseMode.Html, markup);
                    return;
                }

                // Админ-команды
                if (listOfRoot.Contains(msg.Chat.Id.ToString()))
                {
                    if (text.Equals("log", StringComparison.OrdinalIgnoreCase))
                    {
                        if (File.Exists(logUsers))
                        {
                            await using Stream fileUserLog = File.OpenRead(logUsers);
                            await botClient.SendDocument(msg.Chat.Id, new InputFileStream(fileUserLog, "LogUsers.json"), cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await SendMessage(botClient, msg.Chat.Id, "⚠ Лог-файл не найден.");
                        }
                        return;
                    }

                    if (text.Equals("dellog", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            if (File.Exists(logUsers))
                            {
                                File.Delete(logUsers);
                                await SendMessage(botClient, msg.Chat.Id, $"❌ Файл удалён: {logUsers}");
                            }
                            else
                            {
                                await SendMessage(botClient, msg.Chat.Id, $"⚠ Файл не найден: {logUsers}");
                            }
                        }
                        catch (Exception ex)
                        {
                            await SendMessage(botClient, msg.Chat.Id, $"⛔ Ошибка при удалении: {ex.Message}");
                        }
                        return;
                    }

                    if (text.StartsWith("reg "))
                    {
                        var regUserId = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);

                        if (long.TryParse(regUserId, out _))
                        {
                            File.AppendAllText(userList, Environment.NewLine + regUserId);
                            await botClient.SendMessage(msg.Chat.Id, $"➕ Добавлен новый пользователь: {regUserId}");
                            await botClient.SendMessage(947161854, $"➕ Добавлен новый пользователь: {regUserId}");
                        }
                        else
                        {
                            await botClient.SendMessage(msg.Chat.Id, "⚠ Неверный формат ID.");
                        }
                        return;
                    }

                    if (text.Equals("users", StringComparison.OrdinalIgnoreCase))
                    {
                        if (File.Exists(userList))
                        {
                            await using Stream fileUserLog = File.OpenRead(userList);
                            await botClient.SendDocument(msg.Chat.Id, new InputFileStream(fileUserLog, "UserList.txt"), cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await SendMessage(botClient, msg.Chat.Id, "⚠ Список пользователей не найден.");
                        }
                        return;
                    }
                }

                // Если это обычный пользователь с доступом — обрабатываем ввод (поиск скважин и т.п.)
                await ProcessMessage(msg, null);
            }
        }

        private static async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            var data = callbackQuery.Data;
            switch (data)
            {
                case "instruction":
                    await client.SendPhoto(callbackQuery.Message.Chat.Id,
                        photo: "https://raw.githubusercontent.com/LEN4R/WellModesBot/main/pic/pic_instruction.jpg",
                        caption: InstructionText,
                        parseMode: ParseMode.Html);
                    break;
                case "info":
                    await client.SendPhoto(callbackQuery.Message.Chat.Id,
                        photo: "https://raw.githubusercontent.com/LEN4R/WellModesBot/main/pic/pic_info.jpg",
                        caption: InfoText,
                        parseMode: ParseMode.Html);
                    break;
                case "telegramID":
                    await SendMessage(callbackQuery.Message.Chat.Id, $"{callbackQuery.Message.Chat.Id}", parseMode: ParseMode.Html);
                    break;
                default:
                    await SendFieldInfoByIndex(int.Parse(data), callbackQuery.Message.Chat.Id);
                    break;
            }
        }

        private static async Task ProcessMessage(Message msg, InlineKeyboardMarkup markup)
        {
            var message = new StringBuilder();
            if (_allFieldsCombined.TryGetValue(msg.Text, out var wellsList))
            {
                // Отправка данных с выбором месторождении.
                if (wellsList.Count > 1)
                {
                    message.Append("\U0001F50E Пожалуйста, выберите скважину:");
                    markup = new InlineKeyboardMarkup(wellsList.Select(x => 
                             new[] { InlineKeyboardButton.WithCallbackData(_worksheetsList[x.WorksheetNumber].Name + " " + x.FullName, _allFields.IndexOf(x).ToString()) }).ToArray());
                }
                else
                {
                    PrintFieldDataByColumnIndexes(wellsList[0], message, _worksheetsList[wellsList[0].WorksheetNumber]);
                }
                await SendMessage(msg.Chat.Id, message.ToString(), markup: markup, parseMode: ParseMode.Html);
            }
            else
            {
                // Отправка данных без выбора месторождении.
                var firstField = _allFields.FirstOrDefault(x => x.FullName.StartsWith(msg.Text, StringComparison.OrdinalIgnoreCase));
                if (firstField != null)
                {
                    PrintFieldDataByColumnIndexes(firstField, message, _worksheetsList[firstField.WorksheetNumber]);
                    await SendMessage(msg.Chat.Id, message.ToString(), parseMode: ParseMode.Html);
                }
                else
                {
                    await SendMessage(msg.Chat.Id, message: "\U0000274C ОШИБКА! Такой скважины нет!", parseMode: ParseMode.Html);
                }
            }
        }
        // Храним список ID сообщений для каждого чата
        private static readonly Dictionary<long, List<int>> messageHistory = new();

        private static async Task SendMessage(
            long chatId,
            string message,
            ParseMode parseMode = ParseMode.Html,
            InlineKeyboardMarkup markup = null,
            int? replyMessageId = null)
        {
            // Если есть старые сообщения — удаляем все
            if (messageHistory.TryGetValue(chatId, out var oldMessages))
            {
                foreach (var msgId in oldMessages)
                {
                    try
                    {
                        await client.DeleteMessage(chatId, msgId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Не удалось удалить сообщение {msgId}: {ex.Message}");
                    }
                }
                oldMessages.Clear();
            }
            else
            {
                messageHistory[chatId] = new List<int>();
            }

            // Отправляем новое сообщение
            var sentMessage = await client.SendMessage(
                chatId: chatId,
                text: message,
                parseMode: parseMode,
                replyMarkup: markup
            );

            // Сохраняем ID нового сообщения
            messageHistory[chatId].Add(sentMessage.MessageId);
        }

        private static async Task SendFieldInfoByIndex(int index, long chatId)
        {
            var firstField = _allFields[index];
            var worksheet = _worksheetsList[firstField.WorksheetNumber];
            var message = new StringBuilder();
            PrintFieldDataByColumnIndexes(firstField, message, worksheet);
            await SendMessage(chatId, message.ToString());
        }

        // Работа с Excel
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
            // Работа с Excel
            // var path = Directory.EnumerateFiles(Environment.CurrentDirectory).FirstOrDefault(x => x.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase));
            var path = @"Info.xlsx";
            Console.WriteLine($"Файл загружен:{path}");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var xlPackage = new ExcelPackage(new FileInfo(path)))
                    {
                    var worksheetsList = new List<WorksheetInfo>();
                    worksheetsList.Add(ReadWorksheet(xlPackage, 0, new[] { (4, OutputType.Default),     // Месторождение
                                                                           (6, OutputType.Default),     // № скв
                                                                           (7, OutputType.Default),     // Куст
                                                                           (2, OutputType.Default),     // Цех
                                                                           (16, OutputType.Default),    // Диам. экспл. колон.
                                                                           (10, OutputType.Default),    // Объект разработки/пласт
                                                                           (13, OutputType.PVR),        // верх
                                                                           (14, OutputType.PVR),        // низ
                                                                           (15, OutputType.Number),     // Удл. на в.д.
                                                                           (17, OutputType.Default),    // Тек. забой
                                                                           (21, OutputType.Default),    // Марка насоса
                                                                           (22, OutputType.Default),    // Глубина насоса
                                                                           (34, OutputType.Default),    // Доп. оборуд.
                                                                           (32, OutputType.MRP),        // МРП
                                                                           (28, OutputType.Default),    // N
                                                                           (35, OutputType.Default),    // D шт.
                                                                           (38, OutputType.Default),    // Ндин
                                                                           (39, OutputType.Default),    // Рзат. при Ндин.
                                                                           (41, OutputType.Number),     // Рдин. на ТМС
                                                                           (51, OutputType.Default),    // Рпл. внк
                                                                           (64, OutputType.Default),    // Сост. на конец мес/
                                                                           (54, OutputType.Number),     // Qж.ф.
                                                                           (55, OutputType.Number),     // % воды
                                                                           (56, OutputType.Number),     // Qн.ф.
                    })); //ТРДС
                    worksheetsList.Add(ReadWorksheet(xlPackage, 1, new[]  { (4, OutputType.Default),    // Месторождение
                                                                            (6, OutputType.Default),    // № скв
                                                                            (7, OutputType.Default),    // Куст
                                                                            (2, OutputType.Default),    // Цех
                                                                            (3, OutputType.KNS),        // БКНС, КНС
                                                                            (10, OutputType.Default),   // Блок 
                                                                           (11, OutputType.Default),    // Объект разработки
                                                                           (18, OutputType.PVR),        // верх
                                                                           (19, OutputType.PVR),        // низ
                                                                           (20, OutputType.Number),     // Удл. на в.д.
                                                                           (22, OutputType.Default),    // Иск. забой
                                                                           (23, OutputType.Default),    // Тек. забой
                                                                           (24, OutputType.Default),    // СЭ/Характер лифта
                                                                           (25, OutputType.Default),    // Длина подвески НКТ
                                                                           (29, OutputType.Default),    // Глубина пакера
                                                                           (32, OutputType.Default),    // Доп.оборуд. (длина хвост.)
                                                                           (114, OutputType.MRP),       // МРП
                                                                           (47, OutputType.Default),    // Рпл. внк
                                                                           (44, OutputType.Default),    // Нст.
                                                                           (43, OutputType.Default),    // Руст. стат.
                                                                           (53, OutputType.Number),     // Q
                                                                           (37, OutputType.Number),     // Pл.
                                                                           (33, OutputType.Default),    // Dшт.
                                                                           (116, OutputType.Default),   // Потребная закачка
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
            var worksheetFields = new List<FieldInfo>();                                                // список всех скважин (строк) с их данными
            var worksheetFieldsCombined = new Dictionary<string, List<FieldInfo>>();                    // быстрый поиск по номеру скважины (и его обрезанным вариантам)
            var columnNames = new List<string>();                                                       // названия колонок
            var columnMetrics = new List<string>();                                                     // единицы измерения для колонок
            // Получаем доступ к нужному листу
            var myWorksheet = xlPackage.Workbook.Worksheets[worksheetIndex];
            var totalRows = myWorksheet.Dimension.End.Row;
            var totalColumns = myWorksheet.Dimension.End.Column;
            // Читаем названия столбцов и единицы измерения
            for (int k = 2; k <= totalColumns; k++)
            {
                columnNames.Add(myWorksheet.Cells[14, k].Value?.ToString() ?? myWorksheet.Cells[13, k].Value?.ToString());
                columnMetrics.Add(myWorksheet.Cells[15, k].Value?.ToString());
            }
            // Читаем данные построчно
            for (int i = 22; i <= totalRows; i++)
            {
                var numberCell = myWorksheet.Cells[i, 8];
                var fieldNameCell = myWorksheet.Cells[i, 6];
                if (numberCell.Value == null || fieldNameCell.Value == null) continue;

                var numberStr = numberCell.Value.ToString();
                var fieldNameStr = fieldNameCell.Value.ToString();
                if (string.IsNullOrWhiteSpace(numberStr) || string.IsNullOrWhiteSpace(fieldNameStr)) continue;
                // Сохраняем данные строки. Читаем все значения строки (со 2-й колонки до последней) и складываем в список data.
                var data = new List<object>();
                for (int k = 2; k <= totalColumns; k++)
                    data.Add(myWorksheet.Cells[i, k].Value);
                // Создаём объект FieldInfo
                var fieldInfo = new FieldInfo
                {
                    Number = numberStr,                                                                 // номер скважины
                    FieldName = fieldNameStr,                                                           // название
                    RowIndex = i,                                                                       // номер строки в Excel
                    Data = data,                                                                        // все значения по колонкам           
                    WorksheetNumber = worksheetIndex                                                    // с какого листа это
                };
                // Индексация для быстрого поиска
                var numberBuilder = new StringBuilder(numberStr);
                while (numberBuilder.Length > 0)
                {
                    var key = numberBuilder.ToString().ToLowerInvariant();
                    if (!worksheetFieldsCombined.TryGetValue(key, out var list))
                        worksheetFieldsCombined[key] = list = new List<FieldInfo>();
                    list.Add(fieldInfo);
                    numberBuilder.Remove(numberBuilder.Length - 1, 1);
                }
                worksheetFields.Add(fieldInfo);
            }
            // Возвращаем результат
            return new WorksheetInfo(myWorksheet.Name, worksheetFields, worksheetFieldsCombined, requiredData, columnNames, columnMetrics);
        }

        static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
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
}
