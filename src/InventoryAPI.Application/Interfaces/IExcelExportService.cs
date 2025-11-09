namespace InventoryAPI.Application.Interfaces;

/// <summary>
/// Service for exporting data to Excel format
/// </summary>
public interface IExcelExportService
{
    /// <summary>
    /// Export data to Excel format
    /// </summary>
    /// <typeparam name="T">Type of data to export</typeparam>
    /// <param name="data">Data to export</param>
    /// <param name="sheetName">Name of the Excel sheet</param>
    /// <returns>Excel file as byte array</returns>
    byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Sheet1") where T : class;
}
