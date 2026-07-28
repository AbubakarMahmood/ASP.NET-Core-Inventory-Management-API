using ClosedXML.Excel;
using InventoryAPI.Application.Interfaces;
using System.Reflection;

namespace InventoryAPI.Api.Services;

/// <summary>
/// Service for exporting data to Excel format using ClosedXML
/// </summary>
public class ExcelExportService : IExcelExportService
{
    /// <summary>
    /// Export data to Excel format
    /// </summary>
    public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Sheet1") where T : class
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        var dataList = data.ToList();
        if (!dataList.Any())
        {
            return Save(workbook);
        }

        // Get properties to export (exclude complex types)
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => IsSimpleType(p.PropertyType))
            .ToList();

        // Add headers
        for (int i = 0; i < properties.Count; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = SplitCamelCase(properties[i].Name);
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        // Add data rows
        for (int row = 0; row < dataList.Count; row++)
        {
            var item = dataList[row];
            for (int col = 0; col < properties.Count; col++)
            {
                var cell = worksheet.Cell(row + 2, col + 1);
                var value = properties[col].GetValue(item);

                // Format values based on type
                if (value is DateTime dateTime)
                {
                    cell.Value = dateTime;
                    cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
                }
                else if (value is DateOnly dateOnly)
                {
                    cell.Value = dateOnly.ToDateTime(TimeOnly.MinValue);
                    cell.Style.DateFormat.Format = "yyyy-mm-dd";
                }
                else if (value is decimal decimalValue)
                {
                    cell.Value = decimalValue;
                    cell.Style.NumberFormat.Format = "#,##0.00";
                }
                else if (value is double doubleValue)
                {
                    cell.Value = doubleValue;
                    cell.Style.NumberFormat.Format = "#,##0.00";
                }
                else if (value is float floatValue)
                {
                    cell.Value = floatValue;
                    cell.Style.NumberFormat.Format = "#,##0.00";
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

                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        worksheet.SheetView.FreezeRows(1);

        return Save(workbook);
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

    private static byte[] Save(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
