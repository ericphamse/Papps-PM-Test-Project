using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using CvPipeline.Api.Cv.Domain;

namespace CvPipeline.Api.Cv.Infrastructure.Documents;

public class CvDocumentRenderer
{
    private const string NavyHex = "1B2A4A";
    private const string OrangeHex = "C0592A";
    private const string LightBlueHex = "EEF1F7";
    private const string FontName = "Calibri";
    
    public byte[] Render(CvDocument doc)
    {
        using var stream = new MemoryStream();
        using var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = wordDoc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        var body = mainPart.Document.Body!;

        string fullName = doc.FullName.Value ?? "";
        string roleTitle = doc.RoleTitle.Value ?? "";
        string level = doc.Level.Value ?? "";

        var headerPart = AddPageHeader(mainPart, fullName, roleTitle);
        var footerPart = AddPageFooter(mainPart);
        body.AppendChild(new SectionProperties(
            new PageMargin { Top = 720, Bottom = 720, Left = 720, Right = 720 },
            new HeaderReference { Type = HeaderFooterValues.Default, Id = mainPart.GetIdOfPart(headerPart) },
            new FooterReference { Type = HeaderFooterValues.Default, Id = mainPart.GetIdOfPart(footerPart) }));

        body.AppendChild(CreateParagraph($"{fullName} | {roleTitle} | {level}", bold: true, fontSize: 24, color: "1B2A4A"));

        body.AppendChild(CreateDetailsTable(doc));

        body.AppendChild(CreateHeading("Professional Profile"));
        body.AppendChild(CreateParagraph(doc.ProfessionalProfile.Value ?? ""));

        body.AppendChild(CreateHeading("Career Synopsis"));
        body.AppendChild(CreateTwoColumnTable(
            (doc.CareerSynopsis.Value ?? new())
                .Select(e => ($"{e.Title} ({e.Organisation})", $"{e.StartYear} – {e.EndYear}"))
                .ToList()));
                
        body.AppendChild(CreateHeading("Role Suitability"));
        body.AppendChild(CreateParagraph(doc.RoleSuitability.Value ?? ""));

        body.AppendChild(CreateHeading("Core Competencies"));
        foreach (var c in doc.CoreCompetencies.Value ?? new())
            body.AppendChild(CreateBullet(c.Text));

        body.AppendChild(CreateHeading("Commendations and Awards"));
        body.AppendChild(CreateTwoColumnTable(
            (doc.CommendationsAndAwards.Value ?? new())
                .Select(c => (c.Description, c.Year))
                .ToList()));

        body.AppendChild(CreateHeading("Qualifications"));
        body.AppendChild(CreateTwoColumnTable(
            (doc.QualificationsDetailed.Value ?? new())
                .Select(q => ($"{q.Qualification}{(q.Institution != null ? $", {q.Institution}" : "")}", q.Year))
                .ToList()));

        if (doc.CareerHighlights.Value?.Count > 0)
        {
            body.AppendChild(CreateHeading("Career Highlights"));
            foreach (var h in doc.CareerHighlights.Value)
            {
                body.AppendChild(CreateParagraph(h.Heading, bold: true));
                body.AppendChild(CreateBullet(h.Bullet));
            }
        }

        mainPart.Document.Save();
        wordDoc.Dispose();
        return stream.ToArray();
    }

    private static Table CreateDetailsTable(CvDocument doc)
    {
        var table = new Table();
        var referees = doc.Referees.Value ?? new();

        var rows = new[]
        {
            ("FULL NAME", doc.FullName.Value ?? ""),
            ("PROPOSED ROLE", doc.ProposedRole.Value ?? ""),
            ("QUALIFICATIONS", string.Join("; ", doc.Qualifications.Value ?? new())),
            ("SECURITY CLEARANCE", doc.SecurityClearance.Value ?? ""),
            ("YEARS OF EXPERIENCE", doc.YearsOfExperience.Value ?? ""),
            ("AVAILABILITY", doc.Availability.Value ?? ""),
            ("LOCATION", doc.Location.Value ?? ""),
            ("REFEREES", string.Join("\n", referees.Select(r => $"{r.Name} | {r.Position}, {r.Organisation} | {r.Phone}")))
        };

        foreach (var (label, value) in rows)
        {
            var row = new TableRow();

            // Label cell — navy background
            var labelPara = new Paragraph(new Run(
                new RunProperties(
                    new Bold(),
                    new Color { Val = "FFFFFF" },
                    new FontSize { Val = "18" }),
                new Text(label)));
            labelPara.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { After = "0" });
            var labelCell = new TableCell(labelPara);
            labelCell.TableCellProperties = new TableCellProperties(
                new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "1B2A4A" },
                new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "2000" });
            row.AppendChild(labelCell);

            // Value cell
            var valuePara = new Paragraph(new Run(
                new RunProperties(new FontSize { Val = "20" }),
                new Text(value)));
            valuePara.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { After = "0" });
            var valueCell = new TableCell(valuePara);
            valueCell.TableCellProperties = new TableCellProperties(
                new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "EEF1F7" });
            row.AppendChild(valueCell);

            table.AppendChild(row);
        }

        return table;
    }

    private static Paragraph CreateParagraph(string text, bool bold = false, int fontSize = 20, string? color = null)
    {
        var runProps = new RunProperties();
        if (bold) runProps.AppendChild(new Bold());
        if (color != null) runProps.AppendChild(new Color { Val = color });
        runProps.AppendChild(new FontSize { Val = fontSize.ToString() });
        runProps.AppendChild(new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" });

        var paragraph = new Paragraph(new Run(runProps, new Text(text)));
        paragraph.ParagraphProperties = new ParagraphProperties(
            new SpacingBetweenLines { After = "60" });
        return paragraph;
    }

    private static Paragraph CreateHeading(string text)
    {
        var runProps = new RunProperties(
            new Bold(),
            new Color { Val = "C0592A" },
            new FontSize { Val = "22" });

        var para = new Paragraph(new Run(runProps, new Text(text)));
        para.ParagraphProperties = new ParagraphProperties(
            new SpacingBetweenLines { Before = "120", After = "60" });
        return para;
    }

    private static Paragraph CreateBullet(string text)
    {
        var para = new Paragraph(new Run(
            new RunProperties(
                new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
                new FontSize { Val = "20" }),
            new Text("• " + text)));
        para.ParagraphProperties = new ParagraphProperties(
            new Indentation { Left = "360" },
            new SpacingBetweenLines { After = "20" });
        return para;
    }

    private static HeaderPart AddPageHeader(MainDocumentPart mainPart, string fullName, string roleTitle)
    {
        var headerPart = mainPart.AddNewPart<HeaderPart>();
        var header = new Header();

        var table = new Table();
        table.AppendChild(new TableProperties(
            new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
            new TableBorders(
                new TopBorder { Val = BorderValues.None },
                new BottomBorder { Val = BorderValues.None },
                new LeftBorder { Val = BorderValues.None },
                new RightBorder { Val = BorderValues.None },
                new InsideHorizontalBorder { Val = BorderValues.None },
                new InsideVerticalBorder { Val = BorderValues.None })));

        var row = new TableRow();

        var logoCell = new TableCell();
        logoCell.AppendChild(new TableCellProperties(
            new Shading { Val = ShadingPatternValues.Clear, Fill = "FFFFFF" },
            new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "700" },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }));
        logoCell.AppendChild(new Paragraph(new Run(
            new RunProperties(
                new Bold(),
                new Color { Val = "1B2A4A" },
                new FontSize { Val = "24" },
                new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" }),
            new Text("PappsPM"))));
        row.AppendChild(logoCell);

        // Name cell
        var nameCell = new TableCell();
        nameCell.AppendChild(new TableCellProperties(
            new Shading { Val = ShadingPatternValues.Clear, Fill = "C0592A" },
            new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "4300" },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }));
        var namePara = new Paragraph(new Run(
            new RunProperties(
                new Bold(),
                new Color { Val = "FFFFFF" },
                new FontSize { Val = "26" },
                new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" }),
            new Text($"{fullName} | {roleTitle}")));
        namePara.AppendChild(new ParagraphProperties(
            new Justification { Val = JustificationValues.Center },
            new SpacingBetweenLines { Before = "120", After = "120" }));
        nameCell.AppendChild(namePara);
        row.AppendChild(nameCell);

        table.AppendChild(row);
        header.AppendChild(table);
        headerPart.Header = header;
        headerPart.Header.Save();
        return headerPart;
    }

    private static FooterPart AddPageFooter(MainDocumentPart mainPart)
    {
        var footerPart = mainPart.AddNewPart<FooterPart>();
        var footer = new Footer();

        var para = new Paragraph();
        para.AppendChild(new ParagraphProperties(
            new ParagraphBorders(new TopBorder { Val = BorderValues.Single, Color = "CCCCCC", Size = 4 }),
            new Tabs(new TabStop { Val = TabStopValues.Right, Position = 9000 })));

        para.AppendChild(new Run(
            new RunProperties(
                new FontSize { Val = "16" },
                new Color { Val = "888888" },
                new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
                new Italic()),
            new Text("Papps Project Manager Pty Ltd") { Space = SpaceProcessingModeValues.Preserve }));

        para.AppendChild(new Run(
            new RunProperties(
                new FontSize { Val = "16" },
                new Color { Val = "888888" },
                new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" }),
            new Text(" |Capability Statement — Ref 17427") { Space = SpaceProcessingModeValues.Preserve }));

        para.AppendChild(new Run(new TabChar()));

        para.AppendChild(new Run(
            new RunProperties(
                new FontSize { Val = "16" },
                new Color { Val = "888888" },
                new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" }),
            new Text("Page ") { Space = SpaceProcessingModeValues.Preserve }));

        var beginRun = new Run(new RunProperties(new FontSize { Val = "16" }));
        beginRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.Begin });
        para.AppendChild(beginRun);
        para.AppendChild(new Run(new RunProperties(new FontSize { Val = "16" }), new FieldCode(" PAGE ")));
        var endRun = new Run(new RunProperties(new FontSize { Val = "16" }));
        endRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.End });
        para.AppendChild(endRun);

        footer.AppendChild(para);
        footerPart.Footer = footer;
        footerPart.Footer.Save();
        return footerPart;
    }

    private static Table CreateTwoColumnTable(List<(string Left, string Right)> rows)
    {
        var table = new Table();
        table.AppendChild(new TableProperties(
            new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
            new TableBorders(
                new TopBorder { Val = BorderValues.None },
                new BottomBorder { Val = BorderValues.None },
                new LeftBorder { Val = BorderValues.None },
                new RightBorder { Val = BorderValues.None },
                new InsideHorizontalBorder { Val = BorderValues.None },
                new InsideVerticalBorder { Val = BorderValues.None })));

        foreach (var (left, right) in rows)
        {
            var row = new TableRow();

            var leftCell = new TableCell();
            leftCell.AppendChild(new TableCellProperties(
                new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "3800" }));
            var leftPara = new Paragraph(new Run(
                new RunProperties(
                    new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
                    new FontSize { Val = "20" }),
                new Text(left)));
            leftPara.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { After = "0" });
            leftCell.AppendChild(leftPara);
            row.AppendChild(leftCell);

            var rightCell = new TableCell();
            rightCell.AppendChild(new TableCellProperties(
                new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "1200" }));
            var rightPara = new Paragraph(new Run(
                new RunProperties(
                    new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
                    new FontSize { Val = "20" }),
                new Text(right)));
            rightPara.ParagraphProperties = new ParagraphProperties(
                new Justification { Val = JustificationValues.Right },
                new SpacingBetweenLines { After = "0" });
            rightCell.AppendChild(rightPara);
            row.AppendChild(rightCell);

            table.AppendChild(row);
        }

        return table;
    }
}