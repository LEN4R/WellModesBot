namespace WellModesBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var settingsService = new SettingsService();

            settingsService.LoadSettings();

            var usersService = new UsersService();
            var logService = new LogService();
            var dataService = new WellsDataService();

            dataService.LoadData(settingsService);

            var messageBuilder = new MessageBuilder(dataService);

            var botService = new TelegramBotService(logService, settingsService, usersService, dataService, messageBuilder);

            botService.Start();
        }
    }
}