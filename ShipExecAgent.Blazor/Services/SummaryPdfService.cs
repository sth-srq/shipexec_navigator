using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ShipExecAgent.Services;

public class SummaryPdfService
{
    /// <summary>
    /// Generates a styled PDF from the AI-generated company summary text.
    /// Returns the raw PDF bytes.
    /// </summary>
    public byte[] GenerateSummaryPdf(string companyName, string summaryText)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginTop(0.75f, Unit.Inch);
                page.MarginBottom(0.75f, Unit.Inch);
                page.MarginHorizontal(1f, Unit.Inch);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI", "Arial", "sans-serif"));

                page.Header().Element(header =>
                {
                    header.Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"ShipExec Company Summary").FontSize(18).Bold().FontColor("#1e3a5f");
                        });

                        col.Item().PaddingTop(4).Text(companyName).FontSize(12).FontColor("#337ab7").SemiBold();
                        col.Item().PaddingTop(2).Text($"Generated {DateTime.Now:MMMM d, yyyy 'at' h:mm tt}").FontSize(8).FontColor("#888888");
                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#337ab7");
                    });
                });

                page.Content().PaddingTop(12).Element(content =>
                {
                    content.Column(col =>
                    {
                        col.Spacing(2);
                        RenderSummaryContent(col, summaryText);
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8).FontColor("#999999"));
                    text.Span("ShipExec Navigator — Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void RenderSummaryContent(ColumnDescriptor col, string summaryText)
    {
        var lines = summaryText.Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            // Blank line → small spacing
            if (string.IsNullOrWhiteSpace(line))
            {
                col.Item().PaddingTop(4);
                continue;
            }

            var trimmed = line.TrimStart();

            // H1-level headers (lines starting with # or all-caps section headers followed by colon/dashes)
            if (trimmed.StartsWith("# "))
            {
                col.Item().PaddingTop(10).Text(trimmed[2..].Trim())
                    .FontSize(14).Bold().FontColor("#1e3a5f");
                continue;
            }

            // H2-level headers (## prefix)
            if (trimmed.StartsWith("## "))
            {
                col.Item().PaddingTop(8).Text(trimmed[3..].Trim())
                    .FontSize(12).Bold().FontColor("#2c5282");
                continue;
            }

            // H3-level headers (### prefix)
            if (trimmed.StartsWith("### "))
            {
                col.Item().PaddingTop(6).Text(trimmed[4..].Trim())
                    .FontSize(11).SemiBold().FontColor("#337ab7");
                continue;
            }

            // **Bold section header** lines (e.g. "**Company Overview**" on its own line)
            if (trimmed.StartsWith("**") && trimmed.EndsWith("**") && trimmed.Length > 4
                && !trimmed[2..^2].Contains("**"))
            {
                col.Item().PaddingTop(8).Text(trimmed[2..^2].Trim())
                    .FontSize(12).Bold().FontColor("#2c5282");
                continue;
            }

            // Markdown-style separator lines (---, ___, ***)
            if (trimmed.Length >= 3 && trimmed.Distinct().Count() == 1
                && (trimmed[0] == '-' || trimmed[0] == '_' || trimmed[0] == '*'))
            {
                col.Item().PaddingVertical(4).LineHorizontal(0.5f).LineColor("#cccccc");
                continue;
            }

            // Bullet lines (- item or * item or • item)
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("• "))
            {
                var bulletText = trimmed[2..].Trim();
                col.Item().PaddingLeft(12).Row(row =>
                {
                    row.ConstantItem(12).Text("•").FontSize(10).FontColor("#337ab7");
                    row.RelativeItem().Text(text => RenderInlineMarkdown(text, bulletText));
                });
                continue;
            }

            // Sub-bullet lines (indented with - or *)
            if ((line.StartsWith("  ") || line.StartsWith("\t")) &&
                (trimmed.StartsWith("- ") || trimmed.StartsWith("* ")))
            {
                var subText = trimmed[2..].Trim();
                col.Item().PaddingLeft(28).Row(row =>
                {
                    row.ConstantItem(12).Text("◦").FontSize(9).FontColor("#888888");
                    row.RelativeItem().Text(text => RenderInlineMarkdown(text, subText));
                });
                continue;
            }

            // Regular paragraph text
            col.Item().Text(text => RenderInlineMarkdown(text, trimmed));
        }
    }

    /// <summary>
    /// Renders inline markdown-style bold (**text**) within a text span.
    /// </summary>
    private static void RenderInlineMarkdown(TextDescriptor text, string content)
    {
        text.DefaultTextStyle(x => x.FontSize(10).LineHeight(1.5f));

        var remaining = content;
        while (remaining.Length > 0)
        {
            var boldStart = remaining.IndexOf("**");
            if (boldStart < 0)
            {
                text.Span(remaining);
                break;
            }

            var boldEnd = remaining.IndexOf("**", boldStart + 2);
            if (boldEnd < 0)
            {
                text.Span(remaining);
                break;
            }

            if (boldStart > 0)
                text.Span(remaining[..boldStart]);

            text.Span(remaining[(boldStart + 2)..boldEnd]).Bold();
            remaining = remaining[(boldEnd + 2)..];
        }
    }
}
