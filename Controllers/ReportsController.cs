using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models.ConsumablesSubsystem;
using Wellness_Wardens_Project.ViewModels;
using QDoc = QuestPDF.Fluent.Document;
using WDocx = DocumentFormat.OpenXml.Wordprocessing.Document; // for Word
using Word = DocumentFormat.OpenXml.Wordprocessing;
using Excel = DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using QColors = QuestPDF.Helpers.Colors;

namespace Wellness_Wardens_Project.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // 📄 View all wards
        public IActionResult WardReport()
        {
            var wards = _context.Wards
                .Include(w => w.Rooms)
                    .ThenInclude(r => r.Beds)
                .ToList();
            return View(wards);
        }

        // 📊 STOCK MANAGEMENT REPORTS - NEW ADDITIONS

        // 📈 Generate Stock Management Report
        [HttpGet]
        public async Task<IActionResult> GenerateStockReport(string reportType)
        {
            try
            {
                switch (reportType?.ToLower())
                {
                    case "weekly":
                        return await GenerateWeeklyStockTakeReport();
                    case "lowstock":
                        return await GenerateLowStockReport();
                    case "all":
                        return await GenerateAllConsumablesReport();
                    default:
                        return BadRequest("Invalid report type");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating report: {ex.Message}");
            }
        }

        // 🆕 NEW: Multi-format Weekly Stock Take Report
        [HttpGet]
        public async Task<IActionResult> GenerateWeeklyStockTakeReport(string format = "excel")
        {
            try
            {
                var consumables = await _context.Consumables
                    .Include(c => c.Ward)
                    .Where(c => !c.IsDeleted)
                    .OrderBy(c => c.Ward.Name)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                var viewModel = consumables.Select(c => new ConsumableViewModel
                {
                    ConsumableId = c.ConsumableId,
                    Name = c.Name,
                    QuantityAvailable = c.QuantityAvailable,
                    WardName = c.Ward?.Name ?? "Unknown Ward",
                    StockStatus = c.QuantityAvailable == 0 ? "Out of Stock" :
                                 c.QuantityAvailable < 5 ? "Critical" :
                                 c.QuantityAvailable < 20 ? "Low" : "Adequate",
                    StatusColor = c.QuantityAvailable == 0 ? "danger" :
                                 c.QuantityAvailable < 5 ? "warning" :
                                 c.QuantityAvailable < 20 ? "info" : "success",
                    IsCritical = c.QuantityAvailable < 5
                }).ToList();

                return format.ToLower() switch
                {
                    "pdf" => await GenerateWeeklyStockTakePdf(viewModel),
                    "word" => await GenerateWeeklyStockTakeWord(viewModel),
                    "excel" => await GenerateWeeklyStockTakeExcel(viewModel),
                    _ => BadRequest("Invalid format. Use 'pdf', 'word', or 'excel'.")
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating report: {ex.Message}");
            }
        }

        // 🆕 NEW: PDF Generation for Weekly Stock Take
        private async Task<IActionResult> GenerateWeeklyStockTakePdf(List<ConsumableViewModel> consumables)
        {
            var document = QDoc.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(QColors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // Header
                    page.Header()
                        .Column(column =>
                        {
                            column.Item().AlignCenter().Text("🏥 WEEKLY STOCK TAKE REPORT")
                                .SemiBold().FontSize(16).FontColor(QColors.Blue.Darken3);

                            column.Item().AlignCenter().Text($"Generated on {DateTime.Now:MMMM dd, yyyy}")
                                .FontSize(10).FontColor(QColors.Grey.Medium);

                            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(QColors.Grey.Lighten1);
                        });

                    // Summary Section
                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(15);

                            // Summary Statistics
                            var totalItems = consumables.Count;
                            var lowStockCount = consumables.Count(c => c.QuantityAvailable < 20);
                            var criticalCount = consumables.Count(c => c.QuantityAvailable < 5);
                            var outOfStockCount = consumables.Count(c => c.QuantityAvailable == 0);

                            column.Item().Background(QColors.Grey.Lighten4).Padding(15).Column(summaryCol =>
                            {
                                summaryCol.Item().Text("📊 QUICK SUMMARY").SemiBold().FontSize(12);
                                summaryCol.Spacing(5);
                                summaryCol.Item().Text($"• Total Items: {totalItems}");
                                summaryCol.Item().Text($"• Low Stock Items (<20): {lowStockCount}").FontColor(QColors.Orange.Darken2);
                                summaryCol.Item().Text($"• Critical Items (<5): {criticalCount}").FontColor(QColors.Red.Darken2);
                                summaryCol.Item().Text($"• Out of Stock: {outOfStockCount}").FontColor(QColors.Red.Medium);
                            });

                            // Stock Take Table
                            if (consumables.Any())
                            {
                                column.Item().Text("📦 INVENTORY LIST").SemiBold().FontSize(12);

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3); // Item Name
                                        columns.RelativeColumn(2); // Ward
                                        columns.RelativeColumn(1); // Quantity
                                        columns.RelativeColumn(1.5f); // Status
                                        columns.RelativeColumn(2); // Action Required
                                    });

                                    // Table Header
                                    table.Header(header =>
                                    {
                                        header.Cell().Background(QColors.Blue.Medium).Padding(8).Text("Item Name").FontColor(QColors.White).SemiBold();
                                        header.Cell().Background(QColors.Blue.Medium).Padding(8).Text("Ward").FontColor(QColors.White).SemiBold();
                                        header.Cell().Background(QColors.Blue.Medium).Padding(8).Text("Qty").FontColor(QColors.White).SemiBold();
                                        header.Cell().Background(QColors.Blue.Medium).Padding(8).Text("Status").FontColor(QColors.White).SemiBold();
                                        header.Cell().Background(QColors.Blue.Medium).Padding(8).Text("Action").FontColor(QColors.White).SemiBold();
                                    });

                                    // Table Rows
                                    foreach (var item in consumables.OrderBy(c => c.WardName).ThenBy(c => c.Name))
                                    {
                                        var backgroundColor = item.QuantityAvailable == 0 ? QColors.Red.Lighten4 :
                                                            item.QuantityAvailable < 5 ? QColors.Orange.Lighten4 :
                                                            item.QuantityAvailable < 20 ? QColors.Yellow.Lighten4 : QColors.White;

                                        var actionRequired = item.QuantityAvailable < 20 ? "ORDER MORE" : "None";

                                        table.Cell().Background(backgroundColor).Padding(6).Text(item.Name);
                                        table.Cell().Background(backgroundColor).Padding(6).Text(item.WardName);
                                        table.Cell().Background(backgroundColor).Padding(6).AlignCenter().Text(item.QuantityAvailable.ToString());
                                        table.Cell().Background(backgroundColor).Padding(6).Text(item.StockStatus);
                                        table.Cell().Background(backgroundColor).Padding(6).Text(actionRequired);
                                    }
                                });
                            }
                            else
                            {
                                column.Item().Background(QColors.Green.Lighten4).Padding(20).AlignCenter()
                                    .Text("✅ No consumable items found in the system.").SemiBold();
                            }

                            // Footer Note
                            column.Item().PaddingTop(20).AlignCenter()
                                .Text("This report is generated for internal stock management purposes.")
                                .Italic().FontSize(9).FontColor(QColors.Grey.Medium);
                        });

                    // Footer
                    page.Footer()
                          .AlignCenter()
                          .Text(text =>
                          {
                            text.DefaultTextStyle(TextStyle.Default.FontSize(8).FontColor(QColors.Grey.Medium));
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                            text.Span($" | Generated by Ward Management System on {DateTime.Now:yyyy-MM-dd HH:mm}");
                          });
                });
            });

            var pdfBytes = document.GeneratePdf();
            var fileName = $"weekly-stock-take-{DateTime.Now:yyyy-MM-dd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // 🆕 NEW: Word Document Generation for Weekly Stock Take
        private async Task<IActionResult> GenerateWeeklyStockTakeWord(List<ConsumableViewModel> consumables)
        {
            using var memoryStream = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new WDocx();
                var body = mainPart.Document.AppendChild(new Word.Body());

                // Title
                body.AppendChild(CreateParagraph("WEEKLY STOCK TAKE REPORT", 16, true, "0D47A1", Word.JustificationValues.Center));
                body.AppendChild(CreateParagraph($"Generated on {DateTime.Now:MMMM dd, yyyy}", 12, false, "666666", Word.JustificationValues.Center));
                body.AppendChild(CreateParagraph("", 12)); // Empty line

                // Summary Section
                var totalItems = consumables.Count;
                var lowStockCount = consumables.Count(c => c.QuantityAvailable < 20);
                var criticalCount = consumables.Count(c => c.QuantityAvailable < 5);
                var outOfStockCount = consumables.Count(c => c.QuantityAvailable == 0);

                body.AppendChild(CreateParagraph("QUICK SUMMARY", 14, true, "000000"));
                body.AppendChild(CreateParagraph($"• Total Items: {totalItems}", 12));
                body.AppendChild(CreateParagraph($"• Low Stock Items (<20): {lowStockCount}", 12, false, "FF9800"));
                body.AppendChild(CreateParagraph($"• Critical Items (<5): {criticalCount}", 12, false, "F44336"));
                body.AppendChild(CreateParagraph($"• Out of Stock: {outOfStockCount}", 12, false, "D32F2F"));
                body.AppendChild(CreateParagraph("", 12)); // Empty line

                if (consumables.Any())
                {
                    // Create table
                    var table = new Word.Table();

                    // Table properties
                    var tableProps = new Word.TableProperties(
                        new Word.TableBorders(
                            new Word.TopBorder { Val = Word.BorderValues.Single, Size = 8 },
                            new Word.BottomBorder { Val = Word.BorderValues.Single, Size = 8 },
                            new Word.LeftBorder { Val = Word.BorderValues.Single, Size = 8 },
                            new Word.RightBorder { Val = Word.BorderValues.Single, Size = 8 },
                            new Word.InsideHorizontalBorder { Val = Word.BorderValues.Single, Size = 6 },
                            new Word.InsideVerticalBorder { Val = Word.BorderValues.Single, Size = 6 }
                        )
                    );
                    table.AppendChild(tableProps);

                    // Table header row
                    var headerRow = new Word.TableRow();
                    headerRow.Append(
                        CreateTableCell("Item Name", true),
                        CreateTableCell("Ward", true),
                        CreateTableCell("Quantity", true),
                        CreateTableCell("Status", true),
                        CreateTableCell("Action Required", true)
                    );
                    table.AppendChild(headerRow);

                    // Table data rows
                    foreach (var item in consumables.OrderBy(c => c.WardName).ThenBy(c => c.Name))
                    {
                        var actionRequired = item.QuantityAvailable < 20 ? "ORDER MORE" : "None";

                        var dataRow = new Word.TableRow();
                        dataRow.Append(
                            CreateTableCell(item.Name, false),
                            CreateTableCell(item.WardName, false),
                            CreateTableCell(item.QuantityAvailable.ToString(), false),
                            CreateTableCell(item.StockStatus, false),
                            CreateTableCell(actionRequired, false)
                        );
                        table.AppendChild(dataRow);
                    }

                    body.AppendChild(table);
                }
                else
                {
                    body.AppendChild(CreateParagraph("No consumable items found in the system.", 12, false, "666666", Word.JustificationValues.Center));
                }

                // Footer note
                body.AppendChild(CreateParagraph("", 12));
                body.AppendChild(CreateParagraph("This report is generated for internal stock management purposes.", 10, false, "999999", Word.JustificationValues.Center));

                mainPart.Document.Save();
            }

            memoryStream.Position = 0;
            var fileName = $"weekly-stock-take-{DateTime.Now:yyyy-MM-dd}.docx";
            return File(memoryStream.ToArray(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName);
        }

        // 🆕 NEW: Helper method for Word table cells
        private Word.TableCell CreateTableCell(string text, bool isHeader)
        {
            var cell = new Word.TableCell();
            var props = new Word.TableCellProperties(
                new Word.TableCellWidth { Type = Word.TableWidthUnitValues.Auto }
            );
            cell.AppendChild(props);

            var paragraph = CreateParagraph(text, isHeader ? 11 : 10, isHeader, isHeader ? "FFFFFF" : "000000");

            if (isHeader)
            {
                var paragraphProps = paragraph.Descendants<Word.ParagraphProperties>().First();
                paragraphProps.AppendChild(new Word.Shading { Fill = "2E86AB" }); // Blue background for header
            }

            cell.AppendChild(paragraph);
            return cell;
        }

        // 📋 Weekly Stock Take Report (Excel - OpenXML) - KEEP EXISTING
        private async Task<IActionResult> GenerateWeeklyStockTakeExcel()
        {
            var consumables = await _context.Consumables
                .Include(c => c.Ward)
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Ward.Name)
                .ThenBy(c => c.Name)
                .ToListAsync();

            using var memoryStream = new MemoryStream();
            using (var spreadsheet = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
            {
                // Create workbook parts
                var workbookPart = spreadsheet.AddWorkbookPart();
                workbookPart.Workbook = new Excel.Workbook();

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new Excel.SheetData();
                worksheetPart.Worksheet = new Excel.Worksheet(sheetData);

                var sheets = spreadsheet.WorkbookPart.Workbook.AppendChild(new Excel.Sheets());
                var sheet = new Excel.Sheet()
                {
                    Id = spreadsheet.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Weekly Stock Take"
                };
                sheets.Append(sheet);

                // Add header row
                var headerRow = new Excel.Row();
                headerRow.Append(
                    new Excel.Cell() { CellValue = new Excel.CellValue("Item Name"), DataType = Excel.CellValues.String },
                    new Excel.Cell() { CellValue = new Excel.CellValue("Ward"), DataType = Excel.CellValues.String },
                    new Excel.Cell() { CellValue = new Excel.CellValue("Quantity Available"), DataType = Excel.CellValues.String },
                    new Excel.Cell() { CellValue = new Excel.CellValue("Stock Status"), DataType = Excel.CellValues.String },
                    new Excel.Cell() { CellValue = new Excel.CellValue("Last Updated"), DataType = Excel.CellValues.String },
                    new Excel.Cell() { CellValue = new Excel.CellValue("Action Required"), DataType = Excel.CellValues.String }
                );
                sheetData.AppendChild(headerRow);

                // Add data rows
                foreach (var item in consumables)
                {
                    var stockStatus = item.QuantityAvailable == 0 ? "Out of Stock" :
                                    item.QuantityAvailable < 5 ? "Critical" :
                                    item.QuantityAvailable < 20 ? "Low" : "Adequate";

                    var actionRequired = item.QuantityAvailable < 20 ? "ORDER MORE" : "None";

                    var row = new Excel.Row();
                    row.Append(
                        new Excel.Cell() { CellValue = new Excel.CellValue(item.Name), DataType = Excel.CellValues.String },
                        new Excel.Cell() { CellValue = new Excel.CellValue(item.Ward?.Name ?? "Unknown"), DataType = Excel.CellValues.String },
                        new Excel.Cell() { CellValue = new Excel.CellValue(item.QuantityAvailable.ToString()), DataType = Excel.CellValues.Number },
                        new Excel.Cell() { CellValue = new Excel.CellValue(stockStatus), DataType = Excel.CellValues.String },
                        new Excel.Cell() { CellValue = new Excel.CellValue(DateTime.Now.ToString("yyyy-MM-dd")), DataType = Excel.CellValues.String },
                        new Excel.Cell() { CellValue = new Excel.CellValue(actionRequired), DataType = Excel.CellValues.String }
                    );
                    sheetData.AppendChild(row);
                }

                workbookPart.Workbook.Save();
            }

            memoryStream.Position = 0;
            var fileName = $"weekly-stock-take-{DateTime.Now:yyyy-MM-dd}.xlsx";
            return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // 🆕 NEW: Excel version that works with the multi-format method
        private async Task<IActionResult> GenerateWeeklyStockTakeExcel(List<ConsumableViewModel> consumables)
        {
            using var memoryStream = new MemoryStream();
            using (var spreadsheet = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = spreadsheet.AddWorkbookPart();
                workbookPart.Workbook = new Excel.Workbook();

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new Excel.SheetData();
                worksheetPart.Worksheet = new Excel.Worksheet(sheetData);

                var sheets = spreadsheet.WorkbookPart.Workbook.AppendChild(new Excel.Sheets());
                var sheet = new Excel.Sheet()
                {
                    Id = spreadsheet.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Weekly Stock Take"
                };
                sheets.Append(sheet);

                // Add header row
                var headerRow = new Excel.Row();
                headerRow.Append(
                    new Excel.Cell() { CellValue = new Excel.CellValue("Item Name"), DataType = Excel.CellValues.String },
                    new Excel.Cell() { CellValue = new Excel.CellValue("Ward"), DataType = Excel.CellValues.String },
                    new Excel.Cell() { CellValue = new Excel.CellValue("Quantity Available"), DataType = Excel.CellValues.String },
                    new Excel.Cell() { CellValue = new Excel.CellValue("Stock Status"), DataType = Excel.CellValues.String },
                    new Excel.Cell() { CellValue = new Excel.CellValue("Action Required"), DataType = Excel.CellValues.String }
                );
                sheetData.AppendChild(headerRow);

                // Add data rows
                foreach (var item in consumables.OrderBy(c => c.WardName).ThenBy(c => c.Name))
                {
                    var actionRequired = item.QuantityAvailable < 20 ? "ORDER MORE" : "None";

                    var row = new Excel.Row();
                    row.Append(
                        new Excel.Cell() { CellValue = new Excel.CellValue(item.Name), DataType = Excel.CellValues.String },
                        new Excel.Cell() { CellValue = new Excel.CellValue(item.WardName), DataType = Excel.CellValues.String },
                        new Excel.Cell() { CellValue = new Excel.CellValue(item.QuantityAvailable.ToString()), DataType = Excel.CellValues.Number },
                        new Excel.Cell() { CellValue = new Excel.CellValue(item.StockStatus), DataType = Excel.CellValues.String },
                        new Excel.Cell() { CellValue = new Excel.CellValue(actionRequired), DataType = Excel.CellValues.String }
                    );
                    sheetData.AppendChild(row);
                }

                workbookPart.Workbook.Save();
            }

            memoryStream.Position = 0;
            var fileName = $"weekly-stock-take-{DateTime.Now:yyyy-MM-dd}.xlsx";
            return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);}


        // ⚠️ Low Stock Alert Report (PDF - QuestPDF)
        private async Task<IActionResult> GenerateLowStockReport()
        {
            var lowStockItems = await _context.Consumables
                .Include(c => c.Ward)
                .Where(c => !c.IsDeleted && c.QuantityAvailable < 20)
                .OrderBy(c => c.QuantityAvailable)
                .ThenBy(c => c.Ward.Name)
                .ToListAsync();

            var document = QDoc.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(QColors.White); // FIX: QColors instead of Colors
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .AlignCenter()
                        .Text("🚨 LOW STOCK ALERT REPORT")
                        .SemiBold().FontSize(18).FontColor(QColors.Red.Medium); // FIX: QColors instead of Colors

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(20);

                            // Summary Section
                            column.Item().Background(QColors.Grey.Lighten3).Padding(15).Column(summaryColumn => // FIX: QColors
                            {
                                summaryColumn.Item().Text($"📅 Report Date: {DateTime.Now:MMMM dd, yyyy}").SemiBold();
                                summaryColumn.Item().Text($"📦 Total Low Stock Items: {lowStockItems.Count}").SemiBold();
                                summaryColumn.Item().Text($"🔴 Critical Items (<5): {lowStockItems.Count(c => c.QuantityAvailable < 5)}").SemiBold().FontColor(QColors.Red.Medium); // FIX: QColors
                                summaryColumn.Item().Text($"🟡 Low Stock Items (5-20): {lowStockItems.Count(c => c.QuantityAvailable >= 5)}").SemiBold().FontColor(QColors.Orange.Medium); // FIX: QColors
                            });

                            // Items Table
                            if (lowStockItems.Any())
                            {
                                column.Item().Text("📋 Items Requiring Immediate Attention:").SemiBold().FontSize(14);

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(2);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Background(QColors.Blue.Medium).Padding(8).Text("Item Name").FontColor(QColors.White).SemiBold(); // FIX: QColors
                                        header.Cell().Background(QColors.Blue.Medium).Padding(8).Text("Ward").FontColor(QColors.White).SemiBold(); // FIX: QColors
                                        header.Cell().Background(QColors.Blue.Medium).Padding(8).Text("Qty").FontColor(QColors.White).SemiBold(); // FIX: QColors
                                        header.Cell().Background(QColors.Blue.Medium).Padding(8).Text("Status").FontColor(QColors.White).SemiBold(); // FIX: QColors
                                    });

                                    foreach (var item in lowStockItems)
                                    {
                                        var status = item.QuantityAvailable == 0 ? "OUT OF STOCK" :
                                                   item.QuantityAvailable < 5 ? "CRITICAL" : "LOW";
                                        var backgroundColor = item.QuantityAvailable == 0 ? QColors.Red.Lighten4 : // FIX: QColors
                                                            item.QuantityAvailable < 5 ? QColors.Orange.Lighten4 : QColors.Yellow.Lighten4; // FIX: QColors

                                        table.Cell().Background(backgroundColor).Padding(8).Text(item.Name);
                                        table.Cell().Background(backgroundColor).Padding(8).Text(item.Ward?.Name ?? "Unknown");
                                        table.Cell().Background(backgroundColor).Padding(8).AlignCenter().Text(item.QuantityAvailable.ToString());
                                        table.Cell().Background(backgroundColor).Padding(8).Text(status).SemiBold();
                                    }
                                });

                                // Recommendation Section
                                column.Item().Background(QColors.Orange.Lighten5).Padding(15).Column(recommendationColumn => // FIX: QColors
                                {
                                    recommendationColumn.Item().Text("💡 Recommended Actions:").SemiBold().FontSize(14);
                                    recommendationColumn.Item().Text("• Place immediate orders for critical items (marked in RED)");
                                    recommendationColumn.Item().Text("• Review and replenish low stock items (marked in YELLOW)");
                                    recommendationColumn.Item().Text("• Consider increasing reorder levels for frequently low items");
                                });
                            }
                            else
                            {
                                column.Item().Background(QColors.Green.Lighten4).Padding(20).AlignCenter().Text("🎉 No low stock items! All inventory levels are adequate.").SemiBold().FontSize(14); // FIX: QColors
                            }

                            column.Item().PaddingTop(20).AlignCenter().Text("📊 Generated by Ward Management System").Italic().FontSize(10);
                        });
                });
            });

            var pdfBytes = document.GeneratePdf();
            var fileName = $"low-stock-alert-{DateTime.Now:yyyy-MM-dd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        // 📦 All Consumables Inventory Report (Excel - OpenXML)
        private async Task<IActionResult> GenerateAllConsumablesReport()
        {
            var consumables = await _context.Consumables
                .Include(c => c.Ward)
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Ward.Name)
                .ThenBy(c => c.Name)
                .ToListAsync();

            using var memoryStream = new MemoryStream();
            using (var spreadsheet = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = spreadsheet.AddWorkbookPart();
                workbookPart.Workbook = new Excel.Workbook();

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new Excel.SheetData();
                worksheetPart.Worksheet = new Excel.Worksheet(sheetData);

                var sheets = spreadsheet.WorkbookPart.Workbook.AppendChild(new Excel.Sheets());
                var sheet = new Excel.Sheet()
                {
                    Id = spreadsheet.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "All Consumables"
                };
                sheets.Append(sheet);

                // Add header row
                var headerRow = new Excel.Row();
                headerRow.Append(
                    new Excel.Cell() { CellValue = new Excel.CellValue("Item Name"), DataType = Excel.CellValues.String },
                    new Excel.Cell() { CellValue = new Excel.CellValue("Ward"), DataType = Excel.CellValues.String },
                    new Excel.Cell() { CellValue = new Excel.CellValue("Quantity Available"), DataType = Excel.CellValues.String },
                    new Excel.Cell() { CellValue = new Excel.CellValue("Stock Status"), DataType = Excel.CellValues.String },
                    new Excel.Cell() { CellValue = new Excel.CellValue("Recommended Order Qty"), DataType = Excel.CellValues.String }
                );
                sheetData.AppendChild(headerRow);

                // Add data rows
                foreach (var item in consumables)
                {
                    var stockStatus = item.QuantityAvailable == 0 ? "Out of Stock" :
                                    item.QuantityAvailable < 5 ? "Critical" :
                                    item.QuantityAvailable < 20 ? "Low" : "Adequate";

                    var recommendedOrder = item.QuantityAvailable < 20 ? Math.Max(50, item.QuantityAvailable * 3).ToString() : "None";

                    var row = new Excel.Row();
                    row.Append(
                        new Excel.Cell() { CellValue = new Excel.CellValue(item.Name), DataType = Excel.CellValues.String },
                        new Excel.Cell() { CellValue = new Excel.CellValue(item.Ward?.Name ?? "Unknown"), DataType = Excel.CellValues.String },
                        new Excel.Cell() { CellValue = new Excel.CellValue(item.QuantityAvailable.ToString()), DataType = Excel.CellValues.Number },
                        new Excel.Cell() { CellValue = new Excel.CellValue(stockStatus), DataType = Excel.CellValues.String },
                        new Excel.Cell() { CellValue = new Excel.CellValue(recommendedOrder), DataType = Excel.CellValues.String }
                    );
                    sheetData.AppendChild(row);
                }

                workbookPart.Workbook.Save();
            }

            memoryStream.Position = 0;
            var fileName = $"all-consumables-{DateTime.Now:yyyy-MM-dd}.xlsx";
            return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // 📊 API endpoint to get out of stock count for the chart
        [HttpGet]
        public async Task<IActionResult> GetOutOfStockCount()
        {
            var outOfStockCount = await _context.Consumables
                .CountAsync(c => !c.IsDeleted && c.QuantityAvailable == 0);

            return Json(new { count = outOfStockCount });
        }

        

        // 📑 Export ALL wards to PDF
        public IActionResult AllWardsReportPdf()
        {
            var wards = _context.Wards
                .Include(w => w.Rooms)
                    .ThenInclude(r => r.Beds)
                .ToList();

            var pdf = QDoc.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Helvetica"));

                    // Header
                    page.Header().Text("🏥 Hospital Wards Report")
                        .FontSize(24).Bold().FontColor("#0d6efd").AlignCenter();

                    // Content
                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        foreach (var ward in wards)
                        {
                            col.Item().Text($"Ward: {ward.Name}")
                                .FontSize(16).Bold().FontColor("#333");

                            col.Item().Text($"📝 Description: {ward.Description}")
                                .FontSize(13).Italic().FontColor("#555");

                            foreach (var room in ward.Rooms)
                            {
                                col.Item().Text($"🚪 Room {room.RoomNumber}")
                                    .FontSize(14).Bold().FontColor("#0d6efd");

                                if (room.Beds.Any())
                                {
                                    foreach (var bed in room.Beds)
                                    {
                                        var status = bed.Status ?? "Unknown";
                                        var emoji = status switch
                                        {
                                            "Occupied" => "🔴",
                                            "Available" => "🟢",
                                            "Maintenance" => "🟡",
                                            _ => "⚪"
                                        };

                                        col.Item().Text($"   🛏️ Bed {bed.BedNumber} - {emoji} {status}")
                                            .FontSize(12).FontColor("#444");
                                    }
                                }
                                else
                                {
                                    col.Item().Text("   ❌ No beds assigned")
                                        .Italic().FontColor("#999");
                                }
                            }

                            col.Item().Text(new string('-', 35)).FontSize(10).FontColor("#ccc");
                        }
                    });

                    // Footer
                    page.Footer().AlignCenter().Text($"Generated on {DateTime.Now:dd MMM yyyy HH:mm}")
                        .FontSize(10).Italic().FontColor("#666");
                });
            });

            var bytes = pdf.GeneratePdf();
            return File(bytes, "application/pdf", "AllWardsReport.pdf");
        }

        // 📝 Export ALL wards to Word
        public IActionResult AllWardsReportWord()
        {
            var wards = _context.Wards
                .Include(w => w.Rooms)
                    .ThenInclude(r => r.Beds)
                .ToList();

            using var stream = new MemoryStream();
            using (var wordDoc = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new WDocx();
                var body = new Word.Body();

                // Title
                body.Append(CreateParagraph("🏥 Hospital Wards Report", 24, bold: true, color: "0d6efd", align: Word.JustificationValues.Center));

                foreach (var ward in wards)
                {
                    body.Append(CreateParagraph($"Ward: {ward.Name}", 16, bold: true, color: "333333"));
                    body.Append(CreateParagraph($"📝 Description: {ward.Description}", 14, italic: true, color: "555555"));

                    foreach (var room in ward.Rooms)
                    {
                        body.Append(CreateParagraph($"🚪 Room {room.RoomNumber}", 14, bold: true, color: "0d6efd"));

                        if (room.Beds.Any())
                        {
                            foreach (var bed in room.Beds)
                            {
                                var status = bed.Status ?? "Unknown";
                                var emoji = status switch
                                {
                                    "Occupied" => "🔴",
                                    "Available" => "🟢",
                                    "Maintenance" => "🟡",
                                    _ => "⚪"
                                };

                                body.Append(CreateParagraph($"   🛏️ Bed {bed.BedNumber} - {emoji} {status}", 12, color: "444444"));
                            }
                        }
                        else
                        {
                            body.Append(CreateParagraph("   ❌ No beds assigned", 12, italic: true, color: "999999"));
                        }
                    }

                    body.Append(CreateParagraph(new string('─', 35), 10, color: "cccccc"));
                }

                // Footer
                body.Append(CreateParagraph($"Generated on {DateTime.Now:dd MMM yyyy HH:mm}", 10, italic: true, color: "666666", align: Word.JustificationValues.Center));

                mainPart.Document.Append(body);
                mainPart.Document.Save();
            }

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "AllWardsReport.docx");
        }

        // Helper for Word Paragraphs
        private Word.Paragraph CreateParagraph(string text,
                                          int fontSize,
                                          bool bold = false,
                                          string color = "000000",
                                          Word.JustificationValues? align = null,
                                          bool italic = false,
                                          bool underline = false)
        {
            var runProps = new Word.RunProperties();
            runProps.Append(new Word.FontSize { Val = (fontSize * 2).ToString() });
            runProps.Append(new Word.Color { Val = color });

            if (bold) runProps.Append(new Word.Bold());
            if (italic) runProps.Append(new Word.Italic());
            if (underline) runProps.Append(new Word.Underline { Val = Word.UnderlineValues.Single });

            var run = new Word.Run(runProps, new Word.Text(text));

            var paraProps = new Word.ParagraphProperties(new Word.Justification { Val = align ?? Word.JustificationValues.Left });
            return new Word.Paragraph(paraProps, run);
        }
    }
}