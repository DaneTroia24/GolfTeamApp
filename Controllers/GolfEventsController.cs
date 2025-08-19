using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GolfTeamApp.Data;
using GolfTeamApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace GolfTeamApp.Controllers
{
    [Authorize] // Require authentication for all actions
    public class GolfEventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GolfEventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: GolfEvents - Everyone can view events
        [Authorize(Roles = "Admin,Coach,Partner,Athlete")]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.GolfEvents.Include(g => g.CreatedByCoach);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: GolfEvents/Details - Everyone can view event details
        [Authorize(Roles = "Admin,Coach,Partner,Athlete")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var golfEvent = await _context.GolfEvents
                .Include(g => g.CreatedByCoach)
                .FirstOrDefaultAsync(m => m.EventId == id);
            if (golfEvent == null)
            {
                return NotFound();
            }

            return View(golfEvent);
        }

        // GET: GolfEvents/Create - Only Admin and Coach can create events
        [Authorize(Roles = "Admin,Coach")]
        public IActionResult Create()
        {
            ViewData["CreatedByCoachId"] = new SelectList(_context.Coaches, "CoachId", "Name");
            return View();
        }

        // POST: GolfEvents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> Create([Bind("EventId,Title,EventDate,StartTime,EndTime,Location,CreatedByCoachId")] GolfEvent golfEvent)
        {
            // Server-side validation for times
            if (golfEvent.EndTime <= golfEvent.StartTime)
            {
                ModelState.AddModelError("EndTime", "End time must be after start time.");
            }

            // Debug: Check what we received
            Console.WriteLine($"Received CreatedByCoachId: {golfEvent.CreatedByCoachId}");
            Console.WriteLine($"Event Date: {golfEvent.EventDate}");
            Console.WriteLine($"Start Time: {golfEvent.StartTime}");
            Console.WriteLine($"End Time: {golfEvent.EndTime}");

            // Debug: Check if coach exists
            var coachExists = await _context.Coaches.AnyAsync(c => c.CoachId == golfEvent.CreatedByCoachId);
            Console.WriteLine($"Coach exists: {coachExists}");

            // Debug: Show all validation errors
            if (!ModelState.IsValid)
            {
                foreach (var modelError in ModelState)
                {
                    foreach (var error in modelError.Value.Errors)
                    {
                        Console.WriteLine($"Validation Error - Field: {modelError.Key}, Error: {error.ErrorMessage}");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(golfEvent);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Golf event created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["CreatedByCoachId"] = new SelectList(_context.Coaches, "CoachId", "Name", golfEvent.CreatedByCoachId);
            return View(golfEvent);
        }

        // GET: GolfEvents/Edit - Only Admin and Coach can edit events
        [Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var golfEvent = await _context.GolfEvents.FindAsync(id);
            if (golfEvent == null)
            {
                return NotFound();
            }

            // Admin can edit any event, Coaches should only edit their own events
            if (User.IsInRole("Coach") && !User.IsInRole("Admin"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var coach = await _context.Coaches.FirstOrDefaultAsync(c => c.UserId == userId);

                if (coach == null || golfEvent.CreatedByCoachId != coach.CoachId)
                {
                    return Forbid(); // Coach can only edit their own events
                }
            }

            ViewData["CreatedByCoachId"] = new SelectList(_context.Coaches, "CoachId", "Name", golfEvent.CreatedByCoachId);
            return View(golfEvent);
        }

        // POST: GolfEvents/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> Edit(int id, [Bind("EventId,Title,EventDate,StartTime,EndTime,Location,CreatedByCoachId")] GolfEvent golfEvent)
        {
            if (id != golfEvent.EventId)
            {
                return NotFound();
            }

            if (golfEvent.EndTime <= golfEvent.StartTime)
            {
                ModelState.AddModelError("EndTime", "End time must be after start time.");
            }

            // Admin can edit any event, Coaches should only edit their own events
            if (User.IsInRole("Coach") && !User.IsInRole("Admin"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var coach = await _context.Coaches.FirstOrDefaultAsync(c => c.UserId == userId);

                if (coach == null || golfEvent.CreatedByCoachId != coach.CoachId)
                {
                    return Forbid();
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(golfEvent);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Golf event updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GolfEventExists(golfEvent.EventId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CreatedByCoachId"] = new SelectList(_context.Coaches, "CoachId", "Name", golfEvent.CreatedByCoachId);
            return View(golfEvent);
        }

        // GET: GolfEvents/Delete - Only Admin and Coach can delete events
        [Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var golfEvent = await _context.GolfEvents
                .Include(g => g.CreatedByCoach)
                .FirstOrDefaultAsync(m => m.EventId == id);
            if (golfEvent == null)
            {
                return NotFound();
            }

            // Admin can delete any event, Coaches should only delete their own events
            if (User.IsInRole("Coach") && !User.IsInRole("Admin"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var coach = await _context.Coaches.FirstOrDefaultAsync(c => c.UserId == userId);

                if (coach == null || golfEvent.CreatedByCoachId != coach.CoachId)
                {
                    return Forbid(); // Coach can only delete their own events
                }
            }

            return View(golfEvent);
        }

        // POST: GolfEvents/Delete - Only Admin and Coach can delete events
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var golfEvent = await _context.GolfEvents.FindAsync(id);
            if (golfEvent != null)
            {
                // Admin can delete any event, Coaches should only delete their own events
                if (User.IsInRole("Coach") && !User.IsInRole("Admin"))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var coach = await _context.Coaches.FirstOrDefaultAsync(c => c.UserId == userId);

                    if (coach == null || golfEvent.CreatedByCoachId != coach.CoachId)
                    {
                        return Forbid();
                    }
                }

                _context.GolfEvents.Remove(golfEvent);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GolfEventExists(int id)
        {
            return _context.GolfEvents.Any(e => e.EventId == id);
        }
    }
}
