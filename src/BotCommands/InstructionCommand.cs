using System.Threading.Tasks;

namespace WellModesBot.BotCommands
{
    public class InstructionCommand : Command
    {
        private readonly SettingsService _settingsService;

        public InstructionCommand(TelegramBotService botService, SettingsService settingsService)
            : base(botService)
        {
            _settingsService = settingsService;
        }

        public override async Task Execute(CommandParameters parameters)
        {
            await _botService.SendMessage(parameters.ChatId, new MessageInfo()
            {
                PhotoUrl = "https://raw.githubusercontent.com/LEN4R/WellModesBot/main/pic/pic_instruction.jpg",
                Text = _settingsService.InstructionText
            });
        }
    }
}
