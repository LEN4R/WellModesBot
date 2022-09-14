namespace WellModesBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var resourcesService = new ResourcesService();
            var settingsService = new SettingsService();

            settingsService.LoadSettings();
            resourcesService.LoadResources();

            var usersService = new UsersService();
            var logService = new LogService();
            var dataService = new WellsDataService();

            dataService.LoadData(settingsService);

            var botService = new TelegramBotService(logService, settingsService, usersService, dataService);

            botService.Start();
        }
    }
}