using System;
using Telegram.Bot;

namespace WellModesBot
{
    public class SettingsService
    {
        public string InstructionText => $"\U00000031\U000020E3 Введите номер скважины, бот выводит режимные данные.\n" +
                                                        $"\U00000032\U000020E3 Если номер скважины совпадает в нескольких месторождении, бот предложит выбор скважин.\n\n" +
                                                        $"\U00002139 Для быстрого вывода данных возможен ввод: [номер скважины]+[начало названия месторождения]. \n\U000025B6 <b>Бот не привязан к регистру!</b>\U0001F4AA \n\n " +
                                                        $"\U000026A0 Бот не воспринимает '*', для получения информации скважин с индексом, неодходимо ввеcти <b>Индекс!</b>";

        public string UserListFilePath => @"Files/users.txt";
        public string RootListFilePath => @"Files/root.txt";

#if !DEBUG
        public string BotTokenFilePath => "5348869621:AAFeOl55384vMInbTORGsZwo9YVn-NoEv9w"; //WellModesBot
#else
        public string BotTokenFilePath => System.IO.File.ReadAllText("Files/wmbot.txt"); //WMBot
#endif
        public long AdministratorId => 947161854;

        public string ContactPhoneNumber => "+79678888663";
        public string ContactFirstName => "Ленар";
        public string ContactsLastName => "Галиев";

        internal void LoadSettings()
        {

        }
    }
}