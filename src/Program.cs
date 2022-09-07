
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
    class Program
    {
        static void Main(string[] args)
        {
            var settingsService = new SettingsService();

            settingsService.LoadSettings();

            var dataService = new DataService();

            dataService.LoadData(settingsService);

            var messageBuilder = new MessageBuilder(dataService);

            var botService = new BotService(settingsService, dataService, messageBuilder);

            botService.Start();
        }
    }
}