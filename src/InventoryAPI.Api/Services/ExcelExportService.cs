using InventoryAPI.Application.Interfaces;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Reflection;

namespace InventoryAPI.Api.Services;

/// <summary>
/// Service for exporting data to Excel format using EPPlus
/// </summary>
public class ExcelExportService : IExcelExportService
{
    public ExcelExportService()
    {
        // Set EPPlus license context (NonCommercial or Commercial)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// Export data to Excel format
    /// </summary>
    public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Sheet1") where T : class
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);

        var dataList = data.ToList();
        if (!dataList.Any())
        {
            return package.GetAsByteArray();
        }

        // Get properties to export (exclude complex types)
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => IsSimpleType(p.PropertyType))
            .ToList();

        // Add headers
        for (int i = 0; i < properties.Count; i++)
        {
            var cell = worksheet.Cells[1, i + 1];
            cell.Value = SplitCamelCase(properties[i].Name);
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }

        // Add data rows
        for (int row = 0; row < dataList.Count; row++)
        {
            var item = dataList[row];
            for (int col = 0; col < properties.Count; col++)
            {
                var cell = worksheet.Cells[row + 2, col + 1];
                var value = properties[col].GetValue(item);

                // Format values based on type
                if (value is DateTime dateTime)
                {
                    cell.Value = dateTime;
                    cell.Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                }
                else if (value is DateOnly dateOnly)
                {
                    cell.Value = dateOnly.ToDateTime(TimeOnly.MinValue);
                    cell.Style.Numberformat.Format = "yyyy-mm-dd";
                }
                else if (value is decimal || value is double || value is float)
                {
                    cell.Value = value;
                    cell.Style.Numberformat.Format = "#,##0.00";
                }
                else if (value is bool boolValue)
                {
                    cell.Value = boolValue ? "Yes" : "No";
                }
                else if (value is Enum)
                {
                    cell.Value = value.ToString();
                }
                else
                {
                    cell.Value = value?.ToString() ?? string.Empty;
                }

                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }
        }

        // Auto-fit columns
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        // Freeze header row
        worksheet.View.FreezePanes(2, 1);

        return package.GetAsByteArray();
    }

    /// <summary>
    /// Check if type is simple (exportable)
    /// </summary>
    private static bool IsSimpleType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType.IsPrimitive
               || underlyingType.IsEnum
               || underlyingType == typeof(string)
               || underlyingType == typeof(decimal)
               || underlyingType == typeof(DateTime)
               || underlyingType == typeof(DateOnly)
               || underlyingType == typeof(TimeOnly)
               || underlyingType == typeof(Guid);
    }

    /// <summary>
    /// Split camel case for better header names
    /// </summary>
    private static string SplitCamelCase(string input)
    {
        return System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1").Trim();
    }
}
