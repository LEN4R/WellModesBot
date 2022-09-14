using NLog;
using System;

namespace WellModesBot
{
    public class LogService
    {
        private readonly Logger _logger;

        public LogService()
        {
            _logger = LogManager.GetLogger("Main");
        }

        internal void LogMessage(BotUpdate update)
        {
            _logger.Info($"\nID: {update.id}\nUSERNAME: {update.username}\nTEXT: {update.text}\nDATA: {update.data}");
        }
    }
}
