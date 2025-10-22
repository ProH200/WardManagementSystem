using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
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
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.Models.ConsumablesSubsystem;
using Wellness_Wardens_Project.ViewModels;
using Excel = DocumentFormat.OpenXml.Spreadsheet;
using QColors = QuestPDF.Helpers.Colors;
using QDoc = QuestPDF.Fluent.Document;
using WDocx = DocumentFormat.OpenXml.Wordprocessing.Document; // for Word
using Word = DocumentFormat.OpenXml.Wordprocessing;

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
            return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }


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


        //Patient Folder on Docor dashboard

        // 🏥 PATIENT FOLDER REPORT - NEW ADDITION
        [HttpGet]
        public async Task<IActionResult> GeneratePatientFolderPdf(int admissionId)
        {
            try
            {
                var admission = await _context.PatientAdmissions
                    .Include(pa => pa.Patient)
                        .ThenInclude(p => p.PatientAllergies.Where(pa => !pa.IsDeleted))
                            .ThenInclude(pa => pa.Allergy)  // Include the actual Allergy
                    .Include(pa => pa.Patient)
                        .ThenInclude(p => p.PatientMedicalConditions.Where(pmc => !pmc.IsDeleted))
                            .ThenInclude(pmc => pmc.MedicalCondition)  // Include the actual MedicalCondition
                    .Include(pa => pa.Patient)
                        .ThenInclude(p => p.MedicalHistories)
                    .Include(pa => pa.Bed)
                        .ThenInclude(b => b.Room)
                            .ThenInclude(r => r.Ward)
                    .FirstOrDefaultAsync(pa => pa.AdmissionId == admissionId && !pa.IsDeleted);

                if (admission == null)
                {
                    return NotFound("Patient admission not found");
                }

                // Extract allergies and medical conditions from junction tables
                var allergies = admission.Patient.PatientAllergies?
                    .Where(pa => !pa.IsDeleted)
                    .Select(pa => pa.Allergy)
                    .ToList() ?? new List<Allergy>();

                var medicalConditions = admission.Patient.PatientMedicalConditions?
                    .Where(pmc => !pmc.IsDeleted)
                    .Select(pmc => pmc.MedicalCondition)
                    .ToList() ?? new List<MedicalCondition>();

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
                                column.Item().AlignCenter().Text("🏥 PATIENT MEDICAL FOLDER")
                                    .SemiBold().FontSize(18).FontColor(QColors.Blue.Darken3);

                                column.Item().AlignCenter().Text($"Generated on {DateTime.Now:MMMM dd, yyyy at HH:mm}")
                                    .FontSize(10).FontColor(QColors.Grey.Medium);

                                column.Item().PaddingTop(10).LineHorizontal(1).LineColor(QColors.Grey.Lighten1);
                            });

                        // Content
                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Spacing(15);

                                // PATIENT INFORMATION SECTION
                                column.Item().Background(QColors.Blue.Lighten5).Padding(15).Column(section =>
                                {
                                    section.Item().Text("👤 PATIENT INFORMATION").SemiBold().FontSize(14).FontColor(QColors.Blue.Darken3);
                                    section.Spacing(8);

                                    section.Item().Row(row =>
                                    {
                                        row.RelativeItem().Column(infoCol =>
                                        {
                                            infoCol.Item().Text($"Name: {admission.Patient.FirstName} {admission.Patient.LastName}").SemiBold();
                                            infoCol.Item().Text($"ID Number: {admission.Patient.IdentityNumber}");
                                            infoCol.Item().Text($"Date of Birth: {admission.Patient.DateOfBirth:dd MMM yyyy}");
                                            infoCol.Item().Text($"Gender: {admission.Patient.Gender}");
                                        });

                                        row.RelativeItem().Column(infoCol =>
                                        {
                                            infoCol.Item().Text($"Phone: {admission.Patient.PhoneNumber}");
                                            infoCol.Item().Text($"Email: {admission.Patient.Email}");
                                            infoCol.Item().Text($"Emergency Contact: {admission.Patient.EmergencyContact}");
                                        });
                                    });

                                    section.Item().Text($"Address: {admission.Patient.HomeAddress}").FontSize(10);
                                });

                                // CRITICAL MEDICAL INFORMATION - Two Column Layout
                                column.Item().Row(row =>
                                {
                                    // ALLERGIES
                                    row.RelativeItem().Background(QColors.Red.Lighten5).Padding(12).Column(allergyCol =>
                                    {
                                        allergyCol.Item().Text("⚠️ ALLERGIES").SemiBold().FontSize(12).FontColor(QColors.Red.Darken2);
                                        allergyCol.Spacing(5);

                                        if (allergies.Any())
                                        {
                                            foreach (var allergy in allergies)
                                            {
                                                allergyCol.Item().Text($"• {allergy.Name}");
                                                if (!string.IsNullOrEmpty(allergy.Description))
                                                {
                                                    allergyCol.Item().Text($"  {allergy.Description}").FontSize(9).FontColor(QColors.Grey.Medium);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            allergyCol.Item().Text("No allergies recorded").Italic().FontColor(QColors.Grey.Medium);
                                        }
                                    });

                                    // MEDICAL CONDITIONS
                                    row.RelativeItem().Background(QColors.Orange.Lighten5).Padding(12).Column(conditionCol =>
                                    {
                                        conditionCol.Item().Text("❤️ MEDICAL CONDITIONS").SemiBold().FontSize(12).FontColor(QColors.Orange.Darken2);
                                        conditionCol.Spacing(5);

                                        if (medicalConditions.Any())
                                        {
                                            foreach (var condition in medicalConditions)
                                            {
                                                conditionCol.Item().Text($"• {condition.Name}");
                                                if (!string.IsNullOrEmpty(condition.Description))
                                                {
                                                    conditionCol.Item().Text($"  {condition.Description}").FontSize(9).FontColor(QColors.Grey.Medium);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            conditionCol.Item().Text("No medical conditions recorded").Italic().FontColor(QColors.Grey.Medium);
                                        }
                                    });
                                });

                                // MEDICAL HISTORY
                                column.Item().Background(QColors.Teal.Lighten5).Padding(15).Column(historyCol =>
                                {
                                    historyCol.Item().Text("📋 MEDICAL HISTORY").SemiBold().FontSize(14).FontColor(QColors.Teal.Darken3);
                                    historyCol.Spacing(8);

                                    if (admission.Patient.MedicalHistories != null && admission.Patient.MedicalHistories.Any())
                                    {
                                        foreach (var history in admission.Patient.MedicalHistories.OrderByDescending(h => h.DateAdded).Take(10)) // Limit to last 10 entries
                                        {
                                            historyCol.Item().Background(QColors.White).Padding(8).Column(entryCol =>
                                            {
                                                entryCol.Item().Text(history.Notes);
                                                entryCol.Item().Text($"Recorded: {history.DateAdded:dd MMM yyyy HH:mm}")
                                                    .FontSize(9).FontColor(QColors.Grey.Medium);
                                            });
                                        }

                                        if (admission.Patient.MedicalHistories.Count > 10)
                                        {
                                            historyCol.Item().Text($"... and {admission.Patient.MedicalHistories.Count - 10} more entries")
                                                .Italic().FontSize(9).FontColor(QColors.Grey.Medium);
                                        }
                                    }
                                    else
                                    {
                                        historyCol.Item().Text("No medical history recorded").Italic().FontColor(QColors.Grey.Medium);
                                    }
                                });

                                // ADMISSION DETAILS
                                column.Item().Background(QColors.Grey.Lighten4).Padding(15).Column(admissionCol =>
                                {
                                    admissionCol.Item().Text("🏥 ADMISSION DETAILS").SemiBold().FontSize(14).FontColor(QColors.Grey.Darken3);
                                    admissionCol.Spacing(8);

                                    admissionCol.Item().Row(admissionRow =>
                                    {
                                        admissionRow.RelativeItem().Column(detailsCol =>
                                        {
                                            detailsCol.Item().Text($"Admission Date: {admission.AdmissionDate:dd MMM yyyy}");
                                            detailsCol.Item().Text($"Reason: {admission.Reason}");
                                        });

                                        admissionRow.RelativeItem().Column(locationCol =>
                                        {
                                            var wardName = admission.Bed?.Room?.Ward?.Name ?? "Not assigned";
                                            var roomNumber = admission.Bed?.Room?.RoomNumber ?? "Not assigned";
                                            var bedNumber = admission.Bed?.BedNumber ?? "Not assigned";

                                            locationCol.Item().Text($"Ward: {wardName}");
                                            locationCol.Item().Text($"Room: {roomNumber}");
                                            locationCol.Item().Text($"Bed: {bedNumber}");
                                        });
                                    });
                                });

                                // CHRONIC HEALTH INFORMATION
                                column.Item().Background(QColors.Green.Lighten5).Padding(15).Column(chronicCol =>
                                {
                                    chronicCol.Item().Text("💊 CHRONIC HEALTH INFORMATION").SemiBold().FontSize(14).FontColor(QColors.Green.Darken3);
                                    chronicCol.Spacing(8);

                                    chronicCol.Item().Row(chronicRow =>
                                    {
                                        chronicRow.RelativeItem().Column(conditionCol =>
                                        {
                                            conditionCol.Item().Text("Chronic Conditions:").SemiBold();
                                            conditionCol.Item().Text(string.IsNullOrEmpty(admission.Patient.ChronicCondition)
                                                ? "None recorded"
                                                : admission.Patient.ChronicCondition);
                                        });

                                        chronicRow.RelativeItem().Column(medicationCol =>
                                        {
                                            medicationCol.Item().Text("Chronic Medications:").SemiBold();
                                            medicationCol.Item().Text(string.IsNullOrEmpty(admission.Patient.ChronicMedication)
                                                ? "None recorded"
                                                : admission.Patient.ChronicMedication);
                                        });
                                    });
                                });

                                // FOOTER NOTE
                                column.Item().PaddingTop(20).AlignCenter()
                                    .Text("This document contains confidential patient information - Handle with care")
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
                                text.Span($" | Patient: {admission.Patient.FirstName} {admission.Patient.LastName} | Generated on {DateTime.Now:yyyy-MM-dd HH:mm}");
                            });
                    });
                });

                var pdfBytes = document.GeneratePdf();
                var fileName = $"patient-folder-{admission.Patient.FirstName}-{admission.Patient.LastName}-{DateTime.Now:yyyyMMdd}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating patient folder: {ex.Message}");
            }
        }

        // 🏥 PATIENT FOLDER REPORT - WORD FORMAT
        [HttpGet]
        public async Task<IActionResult> GeneratePatientFolderWord(int admissionId)
        {
            try
            {
                var admission = await _context.PatientAdmissions
                    .Include(pa => pa.Patient)
                        .ThenInclude(p => p.PatientAllergies.Where(pa => !pa.IsDeleted))
                            .ThenInclude(pa => pa.Allergy)  // Include the actual Allergy
                    .Include(pa => pa.Patient)
                        .ThenInclude(p => p.PatientMedicalConditions.Where(pmc => !pmc.IsDeleted))
                            .ThenInclude(pmc => pmc.MedicalCondition)  // Include the actual MedicalCondition
                    .Include(pa => pa.Patient)
                        .ThenInclude(p => p.MedicalHistories)
                    .Include(pa => pa.Bed)
                        .ThenInclude(b => b.Room)
                            .ThenInclude(r => r.Ward)
                    .FirstOrDefaultAsync(pa => pa.AdmissionId == admissionId && !pa.IsDeleted);

                if (admission == null)
                {
                    return NotFound("Patient admission not found");
                }

                // Extract allergies and medical conditions from junction tables
                var allergies = admission.Patient.PatientAllergies?
                    .Where(pa => !pa.IsDeleted)
                    .Select(pa => pa.Allergy)
                    .ToList() ?? new List<Allergy>();

                var medicalConditions = admission.Patient.PatientMedicalConditions?
                    .Where(pmc => !pmc.IsDeleted)
                    .Select(pmc => pmc.MedicalCondition)
                    .ToList() ?? new List<MedicalCondition>();

                using var memoryStream = new MemoryStream();

                using (var wordDoc = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
                {
                    var mainPart = wordDoc.AddMainDocumentPart();
                    mainPart.Document = new WDocx();
                    var body = mainPart.Document.AppendChild(new Word.Body());

                    // Title Page
                    body.AppendChild(CreateParagraph("PATIENT MEDICAL FOLDER", 20, true, "2E86AB", Word.JustificationValues.Center));
                    body.AppendChild(CreateParagraph($"Generated on {DateTime.Now:MMMM dd, yyyy at HH:mm}", 12, false, "666666", Word.JustificationValues.Center));
                    body.AppendChild(CreateParagraph("", 12)); // Empty line
                    body.AppendChild(CreateParagraph("", 12)); // Empty line

                    // PATIENT INFORMATION SECTION
                    body.AppendChild(CreateParagraph("PATIENT INFORMATION", 16, true, "2E86AB"));
                    body.AppendChild(CreateParagraph("", 12)); // Empty line

                    // Patient Info Table
                    var patientTable = new Word.Table();
                    var patientTableProps = new Word.TableProperties(
                        new Word.TableBorders(
                            new Word.TopBorder { Val = Word.BorderValues.Single, Size = 8 },
                            new Word.BottomBorder { Val = Word.BorderValues.Single, Size = 8 },
                            new Word.LeftBorder { Val = Word.BorderValues.Single, Size = 8 },
                            new Word.RightBorder { Val = Word.BorderValues.Single, Size = 8 },
                            new Word.InsideHorizontalBorder { Val = Word.BorderValues.Single, Size = 6 },
                            new Word.InsideVerticalBorder { Val = Word.BorderValues.Single, Size = 6 }
                        )
                    );
                    patientTable.AppendChild(patientTableProps);

                    // Patient Info Rows
                    var patientInfoRows = new[]
                    {
                new { Label = "Full Name", Value = $"{admission.Patient.FirstName} {admission.Patient.LastName}" },
                new { Label = "ID Number", Value = admission.Patient.IdentityNumber },
                new { Label = "Date of Birth", Value = admission.Patient.DateOfBirth.ToString("dd MMM yyyy") },
                new { Label = "Gender", Value = admission.Patient.Gender },
                new { Label = "Phone Number", Value = admission.Patient.PhoneNumber },
                new { Label = "Email", Value = admission.Patient.Email ?? "Not provided" },
                new { Label = "Emergency Contact", Value = admission.Patient.EmergencyContact },
                new { Label = "Home Address", Value = admission.Patient.HomeAddress }
            };

                    foreach (var info in patientInfoRows)
                    {
                        var row = new Word.TableRow();
                        row.Append(
                            CreateTableCell(info.Label, true, "E3F2FD"),
                            CreateTableCell(info.Value, false, "FFFFFF")
                        );
                        patientTable.AppendChild(row);
                    }

                    body.AppendChild(patientTable);
                    body.AppendChild(CreateParagraph("", 12)); // Empty line

                    // CRITICAL MEDICAL INFORMATION - Two Column Layout
                    body.AppendChild(CreateParagraph("CRITICAL MEDICAL INFORMATION", 16, true, "2E86AB"));
                    body.AppendChild(CreateParagraph("", 12)); // Empty line

                    var criticalTable = new Word.Table();
                    var criticalTableProps = new Word.TableProperties(
                        new Word.TableBorders(
                            new Word.TopBorder { Val = Word.BorderValues.Single, Size = 6 },
                            new Word.BottomBorder { Val = Word.BorderValues.Single, Size = 6 },
                            new Word.LeftBorder { Val = Word.BorderValues.Single, Size = 6 },
                            new Word.RightBorder { Val = Word.BorderValues.Single, Size = 6 }
                        )
                    );
                    criticalTable.AppendChild(criticalTableProps);

                    // ALLERGIES COLUMN
                    var allergiesCell = new Word.TableCell();
                    allergiesCell.AppendChild(new Word.TableCellProperties(new Word.Shading { Fill = "FFEBEE" }));
                    var allergiesContent = new Word.Paragraph(new Word.ParagraphProperties(
                        new Word.Shading { Fill = "FFEBEE" }));

                    allergiesContent.AppendChild(CreateRun("ALLERGIES", 14, true, "C62828"));
                    allergiesContent.AppendChild(new Word.Run(new Word.Break())); // Line break

                    if (allergies.Any())
                    {
                        foreach (var allergy in allergies)
                        {
                            allergiesContent.AppendChild(CreateRun($"• {allergy.Name}", 12, true, "000000"));
                            allergiesContent.AppendChild(new Word.Run(new Word.Break()));
                            if (!string.IsNullOrEmpty(allergy.Description))
                            {
                                allergiesContent.AppendChild(CreateRun($"  {allergy.Description}", 10, false, "666666"));
                                allergiesContent.AppendChild(new Word.Run(new Word.Break()));
                            }
                        }
                    }
                    else
                    {
                        allergiesContent.AppendChild(CreateRun("No allergies recorded", 12, false, "666666"));
                        allergiesContent.AppendChild(new Word.Run(new Word.Break()));
                    }
                    allergiesCell.AppendChild(allergiesContent);

                    // MEDICAL CONDITIONS COLUMN
                    var conditionsCell = new Word.TableCell();
                    conditionsCell.AppendChild(new Word.TableCellProperties(new Word.Shading { Fill = "FFF3E0" }));
                    var conditionsContent = new Word.Paragraph(new Word.ParagraphProperties(
                        new Word.Shading { Fill = "FFF3E0" }));

                    conditionsContent.AppendChild(CreateRun("MEDICAL CONDITIONS", 14, true, "EF6C00"));
                    conditionsContent.AppendChild(new Word.Run(new Word.Break())); // Line break

                    if (medicalConditions.Any())
                    {
                        foreach (var condition in medicalConditions)
                        {
                            conditionsContent.AppendChild(CreateRun($"• {condition.Name}", 12, true, "000000"));
                            conditionsContent.AppendChild(new Word.Run(new Word.Break()));
                            if (!string.IsNullOrEmpty(condition.Description))
                            {
                                conditionsContent.AppendChild(CreateRun($"  {condition.Description}", 10, false, "666666"));
                                conditionsContent.AppendChild(new Word.Run(new Word.Break()));
                            }
                        }
                    }
                    else
                    {
                        conditionsContent.AppendChild(CreateRun("No medical conditions recorded", 12, false, "666666"));
                        conditionsContent.AppendChild(new Word.Run(new Word.Break()));
                    }
                    conditionsCell.AppendChild(conditionsContent);

                    // Add cells to row
                    var criticalRow = new Word.TableRow();
                    criticalRow.Append(allergiesCell, conditionsCell);
                    criticalTable.AppendChild(criticalRow);

                    body.AppendChild(criticalTable);
                    body.AppendChild(CreateParagraph("", 12)); // Empty line

                    // MEDICAL HISTORY SECTION
                    body.AppendChild(CreateParagraph("MEDICAL HISTORY", 16, true, "2E86AB"));
                    body.AppendChild(CreateParagraph("", 12)); // Empty line

                    if (admission.Patient.MedicalHistories != null && admission.Patient.MedicalHistories.Any())
                    {
                        var historyEntries = admission.Patient.MedicalHistories
                            .OrderByDescending(h => h.DateAdded)
                            .Take(10) // Limit to last 10 entries
                            .ToList();

                        foreach (var history in historyEntries)
                        {
                            body.AppendChild(CreateParagraph(history.Notes, 12, false, "000000"));
                            body.AppendChild(CreateParagraph($"Recorded: {history.DateAdded:dd MMM yyyy HH:mm}", 10, false, "666666"));
                            body.AppendChild(CreateParagraph("", 8)); // Empty line
                        }

                        if (admission.Patient.MedicalHistories.Count > 10)
                        {
                            body.AppendChild(CreateParagraph($"... and {admission.Patient.MedicalHistories.Count - 10} more entries", 10, true, "666666"));
                        }
                    }
                    else
                    {
                        body.AppendChild(CreateParagraph("No medical history recorded", 12, false, "666666"));
                    }

                    body.AppendChild(CreateParagraph("", 12)); // Empty line

                    // ADMISSION DETAILS SECTION
                    body.AppendChild(CreateParagraph("ADMISSION DETAILS", 16, true, "2E86AB"));
                    body.AppendChild(CreateParagraph("", 12)); // Empty line

                    var admissionTable = new Word.Table();
                    admissionTable.AppendChild(new Word.TableProperties(
                        new Word.TableBorders(
                            new Word.TopBorder { Val = Word.BorderValues.Single, Size = 6 },
                            new Word.BottomBorder { Val = Word.BorderValues.Single, Size = 6 },
                            new Word.LeftBorder { Val = Word.BorderValues.Single, Size = 6 },
                            new Word.RightBorder { Val = Word.BorderValues.Single, Size = 6 }
                        )
                    ));

                    var admissionRows = new[]
                    {
                new { Label = "Admission Date", Value = admission.AdmissionDate?.ToString("dd MMM yyyy") ?? "Not specified" },
                new { Label = "Reason for Admission", Value = admission.Reason },
                new { Label = "Ward", Value = admission.Bed?.Room?.Ward?.Name ?? "Not assigned" },
                new { Label = "Room", Value = admission.Bed?.Room?.RoomNumber ?? "Not assigned" },
                new { Label = "Bed", Value = admission.Bed?.BedNumber ?? "Not assigned" }
            };

                    foreach (var rowInfo in admissionRows)
                    {
                        var row = new Word.TableRow();
                        row.Append(
                            CreateTableCell(rowInfo.Label, true, "F5F5F5"),
                            CreateTableCell(rowInfo.Value, false, "FFFFFF")
                        );
                        admissionTable.AppendChild(row);
                    }

                    body.AppendChild(admissionTable);
                    body.AppendChild(CreateParagraph("", 12)); // Empty line

                    // CHRONIC HEALTH INFORMATION
                    body.AppendChild(CreateParagraph("CHRONIC HEALTH INFORMATION", 16, true, "2E86AB"));
                    body.AppendChild(CreateParagraph("", 12)); // Empty line

                    var chronicTable = new Word.Table();
                    chronicTable.AppendChild(new Word.TableProperties(
                        new Word.TableBorders(
                            new Word.TopBorder { Val = Word.BorderValues.Single, Size = 6 },
                            new Word.BottomBorder { Val = Word.BorderValues.Single, Size = 6 },
                            new Word.LeftBorder { Val = Word.BorderValues.Single, Size = 6 },
                            new Word.RightBorder { Val = Word.BorderValues.Single, Size = 6 }
                        )
                    ));

                    var chronicRow = new Word.TableRow();
                    chronicRow.Append(
                        CreateTableCell("Chronic Conditions", true, "E8F5E8"),
                        CreateTableCell("Chronic Medications", true, "E8F5E8")
                    );
                    chronicTable.AppendChild(chronicRow);

                    var chronicDataRow = new Word.TableRow();
                    chronicDataRow.Append(
                        CreateTableCell(string.IsNullOrEmpty(admission.Patient.ChronicCondition) ? "None recorded" : admission.Patient.ChronicCondition, false, "FFFFFF"),
                        CreateTableCell(string.IsNullOrEmpty(admission.Patient.ChronicMedication) ? "None recorded" : admission.Patient.ChronicMedication, false, "FFFFFF")
                    );
                    chronicTable.AppendChild(chronicDataRow);

                    body.AppendChild(chronicTable);
                    body.AppendChild(CreateParagraph("", 12)); // Empty line

                    // CONFIDENTIALITY NOTICE
                    body.AppendChild(CreateParagraph("CONFIDENTIALITY NOTICE", 12, true, "666666", Word.JustificationValues.Center));
                    body.AppendChild(CreateParagraph("This document contains confidential patient information and should be handled with care.", 10, false, "666666", Word.JustificationValues.Center));
                    body.AppendChild(CreateParagraph($"Generated for: {admission.Patient.FirstName} {admission.Patient.LastName} | ID: {admission.Patient.IdentityNumber}", 9, false, "666666", Word.JustificationValues.Center));

                    mainPart.Document.Save();
                }

                memoryStream.Position = 0;
                var fileName = $"patient-folder-{admission.Patient.FirstName}-{admission.Patient.LastName}-{DateTime.Now:yyyyMMdd}.docx";
                return File(memoryStream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating patient folder: {ex.Message}");
            }
        }

        // Helper method for creating runs (text with formatting)
        private Word.Run CreateRun(string text, int fontSize, bool bold, string color)
        {
            var runProps = new Word.RunProperties();
            runProps.Append(new Word.FontSize { Val = (fontSize * 2).ToString() });
            runProps.Append(new Word.Color { Val = color });
            if (bold) runProps.Append(new Word.Bold());

            return new Word.Run(runProps, new Word.Text(text));
        }

        // Helper method for creating table cells with background color
        private Word.TableCell CreateTableCell(string text, bool isHeader, string backgroundColor = "FFFFFF")
        {
            var cell = new Word.TableCell();
            var props = new Word.TableCellProperties(
                new Word.TableCellWidth { Type = Word.TableWidthUnitValues.Auto },
                new Word.Shading { Fill = backgroundColor }
            );
            cell.AppendChild(props);

            var fontSize = isHeader ? 11 : 10;
            var fontWeight = isHeader;
            var fontColor = isHeader ? "FFFFFF" : "000000";

            var paragraph = CreateParagraph(text, fontSize, fontWeight, fontColor);
            cell.AppendChild(paragraph);
            return cell;
        }
    }
}