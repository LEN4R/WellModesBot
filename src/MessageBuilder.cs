using System;
using System.Linq;
using System.Text;

namespace WellModesBot
{
    public class MessageBuilder
    {
        private readonly WellsDataService _dataService;

        public MessageBuilder(WellsDataService dataService)
        {
            _dataService = dataService;
        }

        public StringBuilder BuildMessage(int columnIndex)
        {
            var firstField = _dataService.GetFieldByIndex(columnIndex);
            var worksheet = _dataService.GetWorkSheetListByNumber(firstField.WorksheetNumber);
            var message = new StringBuilder();
            PrintFieldDataByColumnIndexes(firstField, message, worksheet);
            return message;
        }


        private void PrintFieldDataByColumnIndexes(FieldInfo field, StringBuilder message, WorksheetInfo info)
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

        public string BuildMessageByWellId(int id)
        {
            var field = _dataService.GetFieldByIndex(id);
            var messageBuilder = new StringBuilder();
            var worksheetNumber= _dataService.GetWorkSheetListByNumber(field.WorksheetNumber);

            PrintFieldDataByColumnIndexes(field, messageBuilder, worksheetNumber);

            return messageBuilder.ToString();
        }
    }
}