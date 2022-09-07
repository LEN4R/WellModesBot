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

        public string InfoText => $"\U0001F4C5 Дата создания бота: <b>20.04.2022</b>\n" +
                                                  $"\U0001F4BB Версия бота: <b>1.1.2</b>\n" +
                                                  $"\U0001F4BE Технологические режимы от <b>07.2022</b>";


        public string LogUsers => "logUsers.json";
        public string UserList => @"users.txt";
        public string RootList => @"root.txt";

#if !DEBUG
        public string BotToken => "5348869621:AAFeOl55384vMInbTORGsZwo9YVn-NoEv9w"; //WellModesBot
#else
        public string BotToken => System.IO.File.ReadAllText("wmbot.txt"); //WMBot
#endif

        internal void LoadSettings()
        {

        }
    }
}