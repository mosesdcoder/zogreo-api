using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Domain.Enums;
using AppEntity = Zogreo.Domain.Entities.Application;
using QuestDocument = QuestPDF.Fluent.Document;

namespace Zogreo.Infrastructure.Documents;

public class QuestPdfOfferLetterGenerator(IFileStorage storage) : IOfferLetterGenerator
{
    public async Task<string> GenerateAsync(AppEntity application, Domain.Entities.Offer offer, CancellationToken ct = default)
    {
        var pdfBytes = GeneratePdf(application, offer);
        using var ms = new MemoryStream(pdfBytes);
        var proxy = new MemoryStreamFileProxy(ms, $"offer-{application.Id}.pdf");
        return await storage.SaveAsync(proxy, "offer-letters", ct);
    }

    private static byte[] GeneratePdf(AppEntity app, Domain.Entities.Offer offer)
    {
        var doc = QuestDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("Zogreo Bible & Technical Training Institute").FontSize(16).Bold().AlignCenter();
                    col.Item().Text("P.O. Box 12345-00100, Nairobi, Kenya").FontSize(9).AlignCenter();
                    col.Item().PaddingTop(10).LineHorizontal(1);
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Item().Text($"Date: {DateTimeOffset.UtcNow:d MMMM yyyy}");
                    col.Item().PaddingTop(10).Text($"Dear {app.User.FullName},");
                    col.Item().PaddingTop(10).Text(
                        $"We are pleased to offer you admission to the {app.Program.Name} programme " +
                        $"commencing with the {app.Intake.Name}.");
                    if (!string.IsNullOrEmpty(offer.Conditions))
                    {
                        col.Item().PaddingTop(10).Text("Conditions of Offer:").Bold();
                        col.Item().Text(offer.Conditions);
                    }
                    col.Item().PaddingTop(10).Text(
                        $"This offer expires on {offer.ExpiresAt:d MMMM yyyy}. " +
                        "Please accept and pay the acceptance fee before this date.");
                    col.Item().PaddingTop(20).Text("Fee Schedule:").Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(1); });
                        table.Header(h => { h.Cell().Text("Fee").Bold(); h.Cell().Text("Amount (KES)").Bold(); });
                        foreach (var inv in app.Invoices.Where(i => i.FeeCode != FeeCode.Application))
                        {
                            table.Cell().Text(inv.FeeCode.ToString());
                            table.Cell().Text(inv.Amount.ToString("N2"));
                        }
                    });
                    col.Item().PaddingTop(30).Text("Yours sincerely,");
                    col.Item().PaddingTop(20).Text("The Registrar").Bold();
                    col.Item().Text("Zogreo Bible & Technical Training Institute");
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Page "); t.CurrentPageNumber(); t.Span(" of "); t.TotalPages();
                });
            });
        });
        return doc.GeneratePdf();
    }

    private class MemoryStreamFileProxy(MemoryStream stream, string fileName) : IFileProxy
    {
        public string FileName => fileName;
        public string ContentType => "application/pdf";
        public long Length => stream.Length;
        public async Task CopyToAsync(Stream target, CancellationToken ct = default)
        {
            stream.Position = 0;
            await stream.CopyToAsync(target, ct);
        }
    }
}
