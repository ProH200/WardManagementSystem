using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;

namespace Wellness_Wardens_Project.Controllers.AdminControllers
{
    [Authorize(Roles = "Admin")]
    public class WardsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WardsController(ApplicationDbContext context, UserManager<Employee> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
        }

        /* Ward Management
         =================*/
        //GET - Wards
        public async Task<IActionResult> ManageWards()
        {
            var wards = await _context.Wards
                        .Where(w => !w.IsDeleted)
                        .Include(w => w.Rooms)
                        .ToListAsync();
            return View(wards);
        }

        //GET - AddWard 
        [HttpGet]
        public IActionResult AddWard()
        {
            return View();
        }

        // POST - Ward/AddWard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWard(Ward ward)
        {
            if (!ModelState.IsValid)
            {
                // Check for duplicate ward name
                bool exists = await _context.Wards
                    .AnyAsync(w => w.Name == ward.Name && !w.IsDeleted);

                if (exists)
                {
                    ModelState.AddModelError("Name", "A ward with this name already exists.");
                    return View(ward);
                }

                // Add new ward
                _context.Wards.Add(ward);
                await _context.SaveChangesAsync();

                // Store success message for toast
                TempData["SuccessMessage"] = $"Ward '{ward.Name}' added successfully.";

                return RedirectToAction(nameof(ManageWards));
            }

            return View(ward);
        }


        //GET - Update Ward
        [HttpGet]
        public async Task<IActionResult> EditWard(int id)
        {
            var ward = await _context.Wards.FindAsync(id);
            if (ward == null)
            {
                return NotFound();
            }
            return View(ward);
        }

        // POST - Edit Ward
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWard(Ward ward)
        {
            if (!ModelState.IsValid)
            {
                bool exists = await _context.Wards
                    .AnyAsync(w => w.Name == ward.Name && w.WardId != ward.WardId && !w.IsDeleted);

                if (exists)
                {
                    ModelState.AddModelError("Name", "Another ward with this name already exists.");
                    return View(ward);
                }

                _context.Update(ward);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Ward '{ward.Name}' updated successfully.";
                return RedirectToAction(nameof(ManageWards));
            }

            return View(ward);
        }


        // POST - SoftDeleteWard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDeleteWard(int id)
        {
            var ward = await _context.Wards.FindAsync(id);
            if (ward == null) return NotFound();

            ward.IsDeleted = true;
            _context.Update(ward);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Ward '{ward.Name}' deleted successfully.";
            return RedirectToAction(nameof(ManageWards));
        }


        /*Rooms And Beds Management
        ===========================*/
        //GET - Rooms
        public async Task<IActionResult> ManageRooms()
        {
            var rooms = await _context.Rooms
                        .Where(r => !r.IsDeleted)
                        .Include(r => r.Ward)
                        .Include(r => r.Beds)
                        .ToListAsync();

            var wards = _context.Wards.ToList();
            ViewBag.Wards = wards;

            return View(rooms);
        }

        // GET: Add Room
        [HttpGet]
        public async Task<IActionResult> AddRoom()
        {
            ViewBag.Wards = await _context.Wards
                                    .Where(w => !w.IsDeleted)
                                    .ToListAsync();
            return View();
        }


        // POST: Add Room
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRoom(Room room)
        {
            if (!ModelState.IsValid)
            {
                // Check duplicate
                bool exists = await _context.Rooms
                    .AnyAsync(r => r.RoomNumber == room.RoomNumber && r.WardId == room.WardId && !r.IsDeleted);

                if (exists)
                {
                    ModelState.AddModelError("RoomNumber", "This room already exists in the selected ward.");
                    ViewBag.Wards = await _context.Wards.Where(w => !w.IsDeleted).ToListAsync();
                    return View(room);
                }

                _context.Rooms.Add(room);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Room '{room.RoomNumber}' added successfully.";
                return RedirectToAction(nameof(ManageRooms));

            }

            ViewBag.Wards = await _context.Wards.Where(w => !w.IsDeleted).ToListAsync();
            return View(room);
        }

        // GET: Edit Room
        [HttpGet]
        public async Task<IActionResult> EditRoom(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            ViewBag.Wards = await _context.Wards
                                    .Where(w => !w.IsDeleted)
                                    .ToListAsync();
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoom(Room room)
        {
            if (!ModelState.IsValid)
            {
                var existingRoom = await _context.Rooms
                 .FirstOrDefaultAsync(r => r.RoomId == room.RoomId);

                if (existingRoom == null)
                    return NotFound();

                // Check duplicate but exclude current room
                bool exists = await _context.Rooms
                    .AnyAsync(r => r.RoomNumber == room.RoomNumber &&
                                   r.WardId == existingRoom.WardId && // keep original ward
                                   r.RoomId != room.RoomId &&
                                   !r.IsDeleted);

                if (exists)
                {
                    ModelState.AddModelError("RoomNumber", "Another room with this number already exists in the ward.");
                    ViewBag.Wards = await _context.Wards.Where(w => !w.IsDeleted).ToListAsync();
                    return View(room);
                }

                // Only update editable fields
                existingRoom.RoomNumber = room.RoomNumber;
                existingRoom.RoomType = room.RoomType;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Room '{room.RoomNumber}' updated successfully.";
                return RedirectToAction(nameof(ManageRooms));
            }
            ViewBag.Wards = await _context.Wards.Where(w => !w.IsDeleted).ToListAsync();
            return View(room);

            
        }


        // POST: Soft delete room
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDeleteRoom(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            room.IsDeleted = true;
            _context.Update(room);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Room '{room.RoomNumber}' deleted successfully.";
            return RedirectToAction(nameof(ManageRooms));
        }

        /* Ward Management
         =================*/
        //GET: Beds
        [HttpGet]
        public async Task<IActionResult> ManageBeds()
        {
            var beds = await _context.Beds
                .Where(b => !b.IsDeleted)
                .Include(b => b.Room)
                    .ThenInclude(r => r.Ward)
                .Include(b => b.PatientAdmissions)
                    .ThenInclude(pa => pa.Discharges)
                .ToListAsync();

            return View(beds);
        }


        // GET: Bed/AddBed
        [HttpGet]
        public IActionResult AddBed(int roomId)
        {
            var bed = new Bed
            {
                RoomId = roomId,
                Status = "Available"
            };
            return View(bed);
        }

        // POST: Bed/AddBed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBed(Bed bed)
        {
            if (!ModelState.IsValid)
            {
                // Ensure Bed starts unassigned
                bed.Status = "Available";

                _context.Beds.Add(bed);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Bed '{bed.BedNumber}' added successfully.";
                return RedirectToAction("ManageRooms", "Wards");
            }
            return View(bed);
        }


        //POST - Edit Bed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBed(int id, Bed updatedBed)
        {
            if (id != updatedBed.BedId)
                return NotFound();

            if (!ModelState.IsValid)
            {
                var bed = await _context.Beds.FindAsync(id);
                if (bed == null || bed.IsDeleted)
                    return NotFound();

                // Duplicate check (excluding itself)
                var exists = await _context.Beds
                    .AnyAsync(b => b.BedNumber == updatedBed.BedNumber
                                   && b.RoomId == updatedBed.RoomId
                                   && b.BedId != id
                                   && !b.IsDeleted);

                if (exists)
                {
                    ModelState.AddModelError("BedNumber", "This bed number already exists in the selected room.");
                    return View(updatedBed);
                }

                //Update only allowed fields
                bed.BedNumber = updatedBed.BedNumber;
                bed.RoomId = updatedBed.RoomId;

                _context.Beds.Update(bed);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Bed '{bed.BedNumber}' updated successfully.";
                return RedirectToAction(nameof(ManageBeds));
            }
            
            return View(updatedBed);

        }


        // POST : Soft Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDeleteBed(int id)
        {
            var bed = await _context.Beds.FindAsync(id);
            if (bed == null)
                return NotFound();

            // Prevent deleting occupied beds
            if (bed.Status == "Occupied")
            {
                TempData["ErrorMessage"] = $"Bed '{bed.BedNumber}' is occupied and cannot be deleted.";
                return RedirectToAction("ManageBeds");
            }

            bed.IsDeleted = true;
            _context.Beds.Update(bed);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Bed '{bed.BedNumber}' deleted successfully.";
            return RedirectToAction("ManageBeds");
        }

    }
}

