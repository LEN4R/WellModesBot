using System.Threading.Tasks;

namespace WellModesBot.BotCommands
{
    public class GetContactsCommand : Command
    {
        public const string Key = "contacts";
        private readonly SettingsService _settingsService;

        public GetContactsCommand(TelegramBotService botService, SettingsService settingsService)
            : base(botService)
        {
            _settingsService = settingsService;
        }

        public override Task Execute(CommandParameters parameters)
        {
            return  _botService.SendContact(parameters.ChatId,
                phoneNumber: _settingsService.ContactPhoneNumber,
                firstName: _settingsService.ContactFirstName,
                lastName: _settingsService.ContactsLastName);
        }
    }
}
