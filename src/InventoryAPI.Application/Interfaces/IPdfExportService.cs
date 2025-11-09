namespace InventoryAPI.Application.Interfaces;

/// <summary>
/// Service for exporting data to PDF format
/// </summary>
public interface IPdfExportService
{
    /// <summary>
    /// Export data to PDF format
    /// </summary>
    /// <typeparam name="T">Type of data to export</typeparam>
    /// <param name="data">Data to export</param>
    /// <param name="title">PDF document title</param>
    /// <returns>PDF file as byte array</returns>
    byte[] ExportToPdf<T>(IEnumerable<T> data, string title) where T : class;
}
