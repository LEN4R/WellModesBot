namespace WellModesBot.BotCommands
{
    public struct CommandParameters
    {
        public long ChatId { get; set; }
        public string OriginalText { get; set; }
        public string SenderLastName { get; set; }
        public string SenderFirstName { get; set; }
        public string BotName { get; set; }
    }
}
