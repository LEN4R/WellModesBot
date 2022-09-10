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
            //var path = Directory.EnumerateFiles(Environment.CurrentDirectory).FirstOrDefault(x => x.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase));
            var path = @"Info.xlsx";
            Console.WriteLine($"Файл загружен:{path}");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

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
    }
}
