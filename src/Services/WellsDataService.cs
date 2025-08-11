using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WellModesBot
{
    public class WellsDataService
    {
        private List<WorksheetInfo> _worksheetsList;
        private List<FieldInfo> _allFields;
        private Dictionary<string, List<FieldInfo>> _allFieldsCombined;
        private Dictionary<string, Dictionary<string, WellsClusterInfo>> _wellsOrdersLists;

        internal void LoadData(SettingsService settingsService)
        {
            GetData();
        }

        internal FieldInfo GetFieldByIndex(int columnIndex)
        {
            return _allFields[columnIndex];
        }

        internal WorksheetInfo GetWorkSheetListByNumber(int worksheetNumber)
        {
            return _worksheetsList[worksheetNumber];
        }

        private void GetData()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            LoadWellsInfo();
            LoadWellsLocations();
        }

        internal Dictionary<string, WellsClusterInfo> FindClustersByNumber(string clusterNumber)
        {
            if (_wellsOrdersLists.TryGetValue(clusterNumber.ToLowerInvariant(), out var dictionary))
                return dictionary;

            return new Dictionary<string, WellsClusterInfo>();    
        }

        private void LoadWellsInfo()
        {
            //var path = Directory.EnumerateFiles(Environment.CurrentDirectory).FirstOrDefault(x => x.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)); // любое название файла Excel
            var path = @"Files/Info.xlsx";
            Console.WriteLine($"Файл загружен:{path}");

            using (var xlPackage = new ExcelPackage(new FileInfo(path)))
            {
                var worksheetsList = new List<WorksheetInfo>();
                worksheetsList.Add(ReadWorksheet(xlPackage, 0, new[] { (4, OutputType.Default),   // Месторождение
                                                                       (6, OutputType.Default),   // № скв
                                                                       (7, OutputType.Default),   // Куст
                                                                       (2, OutputType.Default),   // Цех
                                                                       (16, OutputType.Default),  // Диам. экспл. колон.
                                                                       (10, OutputType.Default),  // Объект разработки/пласт
                                                                       (13, OutputType.PVR),      // верх
                                                                       (14, OutputType.PVR),      // низ
                                                                       (15, OutputType.Number),   // Удл. на в.д.
                                                                       (17, OutputType.Default),  // Тек. забой
                                                                       (21, OutputType.Default),  // Марка насоса
                                                                       (22, OutputType.Default),  // Глубина насоса
                                                                       (34, OutputType.Default),  // Доп. оборуд.
                                                                       (32, OutputType.MRP),      // МРП
                                                                       (28, OutputType.Default),  // N
                                                                       (35, OutputType.Default),  // D шт.
                                                                       (38, OutputType.Default),  // Ндин
                                                                       (39, OutputType.Default),  // Рзат. при Ндин.
                                                                       (41, OutputType.Number),   // Рдин. на ТМС
                                                                       (51, OutputType.Default),  // Рпл. внк
                                                                       (64, OutputType.Default),  // Сост. на конец мес/
                                                                       (54, OutputType.Number),   // Qж.ф.
                                                                       (55, OutputType.Number),   // % воды
                                                                       (56, OutputType.Number),   // Qн.ф.
                                                                       })); //ТРДС
                worksheetsList.Add(ReadWorksheet(xlPackage, 1, new[] { (4, OutputType.Default),   // Месторождение
                                                                       (6, OutputType.Default),   // № скв
                                                                       (7, OutputType.Default),   // Куст
                                                                       (2, OutputType.Default),   // Цех
                                                                       (3, OutputType.KNS),       // БКНС, КНС
                                                                       (10, OutputType.Default),  // Блок 
                                                                       (11, OutputType.Default),  // Объект разработки
                                                                       (18, OutputType.PVR),      // верх
                                                                       (19, OutputType.PVR),      // низ
                                                                       (20, OutputType.Number),   // Удл. на в.д.
                                                                       (22, OutputType.Default),  // Иск. забой
                                                                       (23, OutputType.Default),  // Тек. забой
                                                                       (24, OutputType.Default),  // СЭ/Характер лифта
                                                                       (25, OutputType.Default),  // Длина подвески НКТ
                                                                       (29, OutputType.Default),  // Глубина пакера
                                                                       (32, OutputType.Default),  // Доп.оборуд. (длина хвост.)
                                                                       (114, OutputType.MRP),     // МРП
                                                                       (47, OutputType.Default),  // Рпл. внк
                                                                       (44, OutputType.Default),  // Нст.
                                                                       (43, OutputType.Default),  // Руст. стат.
                                                                       (53, OutputType.Number),   // Q
                                                                       (37, OutputType.Number),  // Pл.
                                                                       (33, OutputType.Default),  // Dшт.
                                                                       (116, OutputType.Default), // Потребная закачка
                                                                       })); //ТРНС

                _worksheetsList = worksheetsList;
                _allFields = worksheetsList.SelectMany(x => x.Fields).ToList();
                _allFieldsCombined = worksheetsList.SelectMany(x => x.FieldsCombined)
                    .GroupBy(x => x.Key)
                    .Select(x => (x.Key, x.SelectMany(y => y.Value)))
                    .ToDictionary(x => x.Key, x => x.Item2.ToList());
            }
        }

        private void LoadWellsLocations()
        {
            var wellsOrdersLists = new Dictionary<string, Dictionary<string, WellsClusterInfo>>();

            using (var xlPackage = new ExcelPackage(new FileInfo("Files/LocationOfWells.xlsx")))
            {
                var myWorksheet = xlPackage.Workbook.Worksheets[0];
                var totalRows = myWorksheet.Dimension.End.Row;
                var totalColumns = myWorksheet.Dimension.End.Column;

                for (int i = 1; i <= totalRows; i++)
                {
                    var clusterNumber = myWorksheet.Cells[i, 1];
                    var clusterName = myWorksheet.Cells[i, 2];

                    if (clusterNumber.Value == null || clusterName.Value == null)
                        continue;

                    var wellsOrderList = new List<string>(32);

                    for (int k = 3; k <= totalColumns; k++)
                    {
                        var cell = myWorksheet.Cells[i, k];

                        if (cell.Value == null)
                            break;

                        var stringValue = cell.Value.ToString();

                        if (string.IsNullOrWhiteSpace(stringValue))
                            break;

                        wellsOrderList.Add(stringValue);
                    }

                    var clusterNumberString = clusterNumber.Value.ToString().ToLowerInvariant();

                    if (!wellsOrdersLists.TryGetValue(clusterNumberString, out var dictionary))
                        wellsOrdersLists[clusterNumberString] = dictionary = new Dictionary<string, WellsClusterInfo>();

                    dictionary.Add(clusterName.Value.ToString(), new WellsClusterInfo() 
                    {
                        ClusterName = clusterName.ToString(),
                        WellsOrderList = wellsOrderList
                    });
                }

                _wellsOrdersLists = wellsOrdersLists;
            }
        }

        internal bool TryFindWellsByName(string name, out WellData data)
        {
            if (_allFieldsCombined.TryGetValue(name, out var list))
            {
                data = new WellData()
                {
                    Wells = list.Select(x =>
                    {
                        return new WellDataItem()
                        {
                            Id = _allFields.IndexOf(x),
                            FullName = x.FullName,
                            WorksheetNumber = x.WorksheetNumber
                        };
                    }).ToArray()
                };
                return true;
            }

            data = null;
            return false;
        }

        internal string GetWorkSheetNameByNumber(int worksheetNumber)
        {
            return _worksheetsList[worksheetNumber].Name;
        }

        internal bool TryFindWellIdByNamePrefix(string text, out int id)
        {
            var field = _allFields.FirstOrDefault(x => x.FullName.StartsWith(text, StringComparison.OrdinalIgnoreCase));

            if (field != null)
            {
                id = _allFields.IndexOf(field);
                return true;
            }

            id = -1;
            return false;
        }

        private static WorksheetInfo ReadWorksheet(ExcelPackage xlPackage, int worksheetIndex, (int, OutputType)[] requiredData)
        {
            var worksheetFields = new List<FieldInfo>();
            var worksheetFieldsCombined = new Dictionary<string, List<FieldInfo>>();
            var columnNames = new List<string>();
            var columnMetrics = new List<string>();

            var myWorksheet = xlPackage.Workbook.Worksheets[worksheetIndex];
            var totalRows = myWorksheet.Dimension.End.Row;
            var totalColumns = myWorksheet.Dimension.End.Column;

            for (int k = 2; k <= totalColumns; k++)
            {
                columnNames.Add(myWorksheet.Cells[14, k].Value?.ToString() ?? myWorksheet.Cells[13, k].Value?.ToString());
                columnMetrics.Add(myWorksheet.Cells[15, k].Value?.ToString());
            }

            for (int i = 22; i <= totalRows; i++)
            {
                var numberCell = myWorksheet.Cells[i, 8];
                var fieldNameCell = myWorksheet.Cells[i, 6];

                var number = numberCell.Value;
                var fieldName = fieldNameCell.Value;

                if (number == null || fieldName == null)
                    continue;

                var numberStr = number.ToString();
                var fieldNameStr = fieldName.ToString();

                if (string.IsNullOrWhiteSpace(numberStr) || string.IsNullOrWhiteSpace(fieldNameStr))
                    continue;

                //if (!worksheetFieldsCombined.TryGetValue(numberStr.ToLowerInvariant(), out List<FieldInfo> list)) // Укороченный поиск
                //    list = worksheetFieldsCombined[numberStr] = new List<FieldInfo>();

                var data = new List<object>();

                for (int k = 2; k <= totalColumns; k++)
                {
                    var dataCell = myWorksheet.Cells[i, k];
                    data.Add(dataCell.Value);
                }

                var fieldInfo = new FieldInfo()
                {
                    Number = numberStr,
                    FieldName = fieldNameStr,
                    RowIndex = i,
                    Data = data,
                    WorksheetNumber = worksheetIndex
                };

                var numberBuilder = new StringBuilder(numberStr);
                while (numberBuilder.Length > 0)
                {
                    var key = numberBuilder.ToString().ToLowerInvariant();
                    if (!worksheetFieldsCombined.TryGetValue(key, out List<FieldInfo> list))
                        list = worksheetFieldsCombined[key] = new List<FieldInfo>();

                    list.Add(fieldInfo);
                    numberBuilder.Remove(numberBuilder.Length - 1, 1);
                }

                //list.Add(fieldInfo);
                worksheetFields.Add(fieldInfo);
            }

            return new WorksheetInfo(myWorksheet.Name, worksheetFields, worksheetFieldsCombined, requiredData, columnNames, columnMetrics);
        }

        public string PrintFieldDataByIndex(int id)
        {
            var field = GetFieldByIndex(id);
            var worksheetInfo = GetWorkSheetListByNumber(field.WorksheetNumber);

            return PrintFieldDataByColumnIndexes(field, worksheetInfo);
        }

        private string PrintFieldDataByColumnIndexes(FieldInfo field, WorksheetInfo info)
        {
            var message = new StringBuilder();

            var query = info.ColumnNames
                .Select((x, i) => (key: x, value: field.Data[i], metrics: info.ColumnMetrics[i]))
                .Where(x => x.key != null).ToArray();

            foreach ((int, OutputType) index in info.RequiredData)
            {
                var queryIndex = query[index.Item1];

                if (queryIndex.value == null)
                    continue;

                var stringValue = queryIndex.value.ToString();

                if (string.IsNullOrWhiteSpace(stringValue))
                    continue;

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
                        if (queryIndex.metrics == "ат")
                            queryIndex.metrics = "атм";
                        if (double.TryParse(stringValue, out var result))
                            message.AppendLine($"{queryIndex.key}: {double.Parse(stringValue).ToString("0.00")} {queryIndex.metrics}");
                        else
                            message.AppendLine($"{queryIndex.key}: {queryIndex.value} {queryIndex.metrics}");
                        break;
                    case OutputType.MRP:
                        if (int.TryParse(stringValue, out var number))
                            message.AppendLine($"{queryIndex.key}: {int.Parse(stringValue) + DateTime.Now.Day - 1} {queryIndex.metrics} на {DateTime.Now.ToString("dd.MM.yyyy")}");
                        else
                            message.AppendLine($"{queryIndex.key}: {queryIndex.value} {queryIndex.metrics}");
                        break;
                    case OutputType.KNS:
                        string waterInjection = (queryIndex.value.ToString() == "0") ? "местная" : $"КНС-{queryIndex.value}";
                        message.AppendLine($"Закачка: {waterInjection}");
                        break;
                    default:
                        break;
                }
            }

            return message.ToString();
        }
    }
}
