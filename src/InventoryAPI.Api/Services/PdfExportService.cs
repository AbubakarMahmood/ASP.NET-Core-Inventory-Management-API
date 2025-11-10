using InventoryAPI.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection;

namespace InventoryAPI.Api.Services;

/// <summary>
/// Service for exporting data to PDF format using QuestPDF
/// </summary>
public class PdfExportService : IPdfExportService
{
    public PdfExportService()
    {
        // Set QuestPDF license (Community license for open-source projects)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Export data to PDF format
    /// </summary>
    public byte[] ExportToPdf<T>(IEnumerable<T> data, string title) where T : class
    {
        var dataList = data.ToList();

        // Get properties to export (exclude complex types)
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => IsSimpleType(p.PropertyType))
            .ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .AlignLeft()
                            .Text(title)
                            .FontSize(20)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        row.RelativeItem()
                            .AlignRight()
                            .Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);
                    });

                page.Content()
                    .PaddingVertical(0.5f, Unit.Centimetre)
                    .Table(table =>
                    {
                        // Define columns
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var _ in properties)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        // Header
                        table.Header(header =>
                        {
                            foreach (var property in properties)
                            {
                                header.Cell()
                                    .Element(CellStyle)
                                    .Background(Colors.Blue.Lighten3)
                                    .Padding(5)
                                    .Text(SplitCamelCase(property.Name))
                                    .Bold()
                                    .FontSize(10);
                            }

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.Border(1).BorderColor(Colors.Grey.Lighten2);
                            }
                        });

                        // Data rows
                        foreach (var item in dataList)
                        {
                            foreach (var property in properties)
                            {
                                var value = property.GetValue(item);
                                var displayValue = FormatValue(value);

                                table.Cell()
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5)
                                    .Text(displayValue)
                                    .FontSize(9);
                            }
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                        text.FontSize(9);
                        text.FontColor(Colors.Grey.Darken1);
                    });
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Format value for display in PDF
    /// </summary>
    private static string FormatValue(object? value)
    {
        if (value == null) return string.Empty;

        return value switch
        {
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            DateOnly dateOnly => dateOnly.ToString("yyyy-MM-dd"),
            decimal or double or float => string.Format("{0:N2}", value),
            bool boolValue => boolValue ? "Yes" : "No",
            Enum => value.ToString() ?? string.Empty,
            Guid guid => guid.ToString().Substring(0, 8) + "...",
            _ => value.ToString() ?? string.Empty
        };
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
