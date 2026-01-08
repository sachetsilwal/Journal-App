using Journal.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Journal.Services;

public static class JournalPdfExporter
{
    public static byte[] Generate(IEnumerable<JournalEntry> entries, DateOnly from, DateOnly to)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text($"Journal Export ({from:yyyy-MM-dd} → {to:yyyy-MM-dd})")
                    .FontSize(18)
                    .SemiBold()
                    .AlignCenter();

                page.Content().Column(col =>
                {
                    foreach (var e in entries.OrderBy(e => e.EntryDate))
                    {
                        col.Item().PaddingBottom(10).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2);

                        col.Item().Text($"{e.EntryDate:dddd, dd MMM yyyy}")
                            .FontSize(14).SemiBold();

                        col.Item().Text($"Mood: {e.PrimaryMood}")
                            .FontColor(QuestPDF.Helpers.Colors.Blue.Medium);

                        if (!string.IsNullOrWhiteSpace(e.TagsCsv))
                            col.Item().Text($"Tags: {e.TagsCsv}");

                        col.Item().PaddingTop(5)
                            .Text(e.ContentMarkdown);
                    }
                });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Generated on ");
                        x.Span(DateTime.Now.ToString("g")).SemiBold();
                    });
            });
        }).GeneratePdf();
    }
}
