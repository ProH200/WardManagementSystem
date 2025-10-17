using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models;
using Wellness_Wardens_Project.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Wellness_Wardens_Project.Models.ConsumablesSubsystem;
using DocumentFormat.OpenXml.InkML;



namespace Wellness_Wardens_Project.Controllers.ConsumablesController
{
    [Authorize(Roles = "Script Manager")]
    public class PrescriptionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PrescriptionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: List all prescriptions
        public async Task<IActionResult> Index()
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Employee)
                .Include(p => p.PrescriptionMedications)
                    .ThenInclude(pm => pm.Medication)
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.DateWritten)
                .ToListAsync();

            return View(prescriptions);
        }

        // GET: Pending prescriptions
        public async Task<IActionResult> Pending()
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Employee)
                .Include(p => p.PrescriptionMedications)
                    .ThenInclude(pm => pm.Medication)
                .Where(p => !p.IsProcessed && !p.IsDeleted)
                .OrderByDescending(p => p.DateWritten)
                .ToListAsync();

            return View(prescriptions);
        }

        // GET: Prescription details
        public async Task<IActionResult> Details(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Employee)
                .Include(p => p.PrescriptionMedications)
                    .ThenInclude(pm => pm.Medication)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id && !p.IsDeleted);

            if (prescription == null)
            {
                return NotFound();
            }

            return View(prescription);
        }

        // POST: Send to Pharmacy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToPharmacy(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription == null)
            {
                return NotFound();
            }

            prescription.IsProcessed = true;
            _context.Update(prescription);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Prescription sent to pharmacy successfully!";
            return RedirectToAction(nameof(Pending));
        }

        // POST: Mark as Delivered
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsDelivered(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription == null)
            {
                return NotFound();
            }

            if (!prescription.IsProcessed)
            {
                TempData["ErrorMessage"] = "Prescription must be sent to pharmacy before marking as delivered.";
                return RedirectToAction(nameof(Pending));
            }

            prescription.IsDelivered = true;
            prescription.DateDelivered = DateTime.Now;
            _context.Update(prescription);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Prescription marked as delivered to ward!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Return to Pending (UNDO processing if needed)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnToPending(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription == null)
            {
                return NotFound();
            }

            prescription.IsProcessed = false;
            prescription.IsDelivered = false;
            prescription.DateDelivered = null;
            _context.Update(prescription);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Prescription returned to pending status!";
            return RedirectToAction(nameof(Index));
        }

        // API endpoint to get current dashboard statistics
        [HttpGet]
        public async Task<JsonResult> GetDashboardStats()
        {
            try
            {
                var stats = new
                {
                    pendingCount = await _context.Prescriptions.CountAsync(p => !p.IsProcessed && !p.IsDeleted),
                    processedCount = await _context.Prescriptions.CountAsync(p => p.IsProcessed && !p.IsDelivered && !p.IsDeleted),
                    completedToday = await _context.Prescriptions.CountAsync(p =>
                        p.IsDelivered &&
                        p.DateDelivered.HasValue &&
                        p.DateDelivered.Value.Date == DateTime.Today &&
                        !p.IsDeleted)
                };
                return Json(stats);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    pendingCount = 0,
                    processedCount = 0,
                    completedToday = 0
                });
            }
        }

        // API endpoint to get pending count only
        [HttpGet]
        public async Task<JsonResult> GetPendingCount()
        {
            var pendingCount = await _context.Prescriptions
                .CountAsync(p => !p.IsProcessed && !p.IsDeleted);
            return Json(new { pendingCount });
        }



        // GET: Generate Pending Prescriptions Report
        public async Task<IActionResult> GeneratePendingReport()
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Employee)
                .Include(p => p.PrescriptionMedications)
                    .ThenInclude(pm => pm.Medication)
                .Where(p => !p.IsProcessed && !p.IsDeleted)
                .OrderByDescending(p => p.DateWritten)
                .ToListAsync();

            return GeneratePdfReport(prescriptions, "Pending Prescriptions Report");
        }

        // GET: Generate Processed & Delivered Report
        public async Task<IActionResult> GenerateProcessedReport()
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Employee)
                .Include(p => p.PrescriptionMedications)
                    .ThenInclude(pm => pm.Medication)
                .Where(p => p.IsProcessed && !p.IsDeleted)
                .OrderByDescending(p => p.DateWritten)
                .ToListAsync();

            return GeneratePdfReport(prescriptions, "Processed & Delivered Prescriptions Report");
        }

        // GET: Generate All Prescriptions Report
        public async Task<IActionResult> GenerateAllReport()
        {
            try
            {
                var prescriptions = await _context.Prescriptions
                    .Include(p => p.Employee)
                    .Include(p => p.PrescriptionMedications)
                        .ThenInclude(pm => pm.Medication)
                    .Where(p => !p.IsDeleted)
                    .OrderByDescending(p => p.DateWritten)
                    .ToListAsync();

                return GeneratePdfReport(prescriptions, "All Prescriptions Report");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateAllReport: {ex}");
                Console.WriteLine($"Inner Exception: {ex.InnerException}");

                return StatusCode(500, $"Server Error: {ex.Message}\nInner Exception: {ex.InnerException?.Message}\nStack Trace: {ex.StackTrace}");
            }
        }

        // Helper method to generate PDF using QuestPDF
        private IActionResult GeneratePdfReport(List<Prescription> prescriptions, string title)
        {
            try
            {
                Console.WriteLine($"Starting PDF generation for {prescriptions.Count} prescriptions");

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(12));

                        page.Header()
                            .AlignCenter()
                            .Text(title)
                            .SemiBold().FontSize(20).FontColor(Colors.Blue.Darken3);

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Spacing(10);

                                // Report summary
                                column.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(summaryColumn =>
                                {
                                    summaryColumn.Item().Text($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}");
                                    summaryColumn.Item().Text($"Total prescriptions: {prescriptions.Count}");
                                });

                                // Prescriptions table
                                if (prescriptions.Any())
                                {
                                    column.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2); // Reference
                                            columns.RelativeColumn(1.5f); // Date
                                            columns.RelativeColumn(2); // Doctor
                                            columns.RelativeColumn(1.5f); // Status
                                            columns.RelativeColumn(3); // Medications
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Reference").SemiBold();
                                            header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Date").SemiBold();
                                            header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Doctor").SemiBold();
                                            header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Status").SemiBold();
                                            header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Medications").SemiBold();
                                        });

                                        foreach (var prescription in prescriptions)
                                        {
                                            string reference;
                                            try
                                            {
                                                var employeeInitials = prescription.Employee != null
                                                    ? $"{(prescription.Employee.FirstName ?? "")[0]}{(prescription.Employee.LastName ?? "")[0]}"
                                                    : "NN";
                                                reference = $"{prescription.DateWritten:MMdd}-{employeeInitials}-{prescription.PrescriptionId % 1000:D3}";
                                            }
                                            catch (Exception refEx)
                                            {
                                                reference = $"Error-{prescription.PrescriptionId}";
                                                Console.WriteLine($"Error generating reference for prescription {prescription.PrescriptionId}: {refEx.Message}");
                                            }

                                            var status = prescription.IsDelivered ? "Delivered" :
                                                        prescription.IsProcessed ? "In Pharmacy" : "Pending";

                                            var medications = "None";
                                            try
                                            {
                                                if (prescription.PrescriptionMedications != null && prescription.PrescriptionMedications.Any())
                                                {
                                                    var medicationNames = prescription.PrescriptionMedications
                                                        .Where(pm => pm?.Medication != null)
                                                        .Select(pm => pm.Medication.Name ?? "Unknown Medication")
                                                        .ToList();

                                                    medications = medicationNames.Any()
                                                        ? string.Join(", ", medicationNames)
                                                        : "None";
                                                }
                                            }
                                            catch (Exception medEx)
                                            {
                                                medications = "Error loading medications";
                                                Console.WriteLine($"Error loading medications for prescription {prescription.PrescriptionId}: {medEx.Message}");
                                            }

                                            var doctorName = prescription.Employee != null
                                                ? $"{prescription.Employee.FirstName} {prescription.Employee.LastName}"
                                                : "Unknown Doctor";

                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(reference);
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(prescription.DateWritten.ToString("MMM dd, yyyy"));
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(doctorName);
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(status);
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(medications);
                                        }
                                    });
                                }
                                else
                                {
                                    column.Item().Background(Colors.Orange.Lighten5).Padding(20).AlignCenter().Text("No prescriptions found").Italic();
                                }

                                // Footer
                                column.Item().AlignRight().Text(txt =>
                                {
                                    txt.Span("Page ").FontSize(10);
                                    txt.CurrentPageNumber().FontSize(10);
                                    txt.Span(" of ").FontSize(10);
                                    txt.TotalPages().FontSize(10);
                                });
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Wellness Wardens - ").FontSize(10);
                                x.CurrentPageNumber().FontSize(10);
                                x.Span(" / ").FontSize(10);
                                x.TotalPages().FontSize(10);
                            });
                    });
                });

                Console.WriteLine("Document created, generating PDF bytes...");
                var pdfBytes = document.GeneratePdf();
                Console.WriteLine($"PDF generated successfully: {pdfBytes.Length} bytes");

                // Return PDF file
                var filename = $"{title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                return File(pdfBytes, "application/pdf", filename);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GeneratePdfReport: {ex}");
                Console.WriteLine($"Inner Exception: {ex.InnerException}");
                return StatusCode(500, $"PDF Generation Error: {ex.Message}\nInner Exception: {ex.InnerException?.Message}");
            }
        }


    }
}