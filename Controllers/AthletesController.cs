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
    [Authorize]
    public class AthletesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AthletesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Athletes
        [Authorize(Roles = "Admin,Coach,Partner")]
        public async Task<IActionResult> Index()
        {
            // All roles can see all athletes
            var athletes = _context.Athletes.Include(a => a.Partner);
            return View(await athletes.ToListAsync());
        }

        // GET: Athletes/Details - Admin, Coach, and Partner can view
        [Authorize(Roles = "Admin,Coach,Partner")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var athlete = await _context.Athletes
                .Include(a => a.Partner)
                .Include(a => a.EventScores)
                    .ThenInclude(es => es.Event)
                .Include(a => a.EventScores)
                    .ThenInclude(es => es.EnteredByPartner)
                .FirstOrDefaultAsync(m => m.AthleteId == id);

            if (athlete == null)
            {
                return NotFound();
            }

            // All roles can view any athlete details
            // The view will handle what information is displayed based on role
            return View(athlete);
        }

        // GET: Athletes/Create - Admin and Coach can create new athletes
        [Authorize(Roles = "Admin,Coach")]
        public IActionResult Create()
        {
            ViewData["PartnerId"] = new SelectList(_context.Partners, "PartnerId", "Name");
            return View();
        }

        // POST: Athletes/Create - Admin and Coach can create new athletes
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> Create([Bind("AthleteId,Name,PictureUrl,SwingRating,PowerRating,UnderstandingRating,PartnerId")] Athlete athlete)
        {
            if (ModelState.IsValid)
            {
                _context.Add(athlete);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PartnerId"] = new SelectList(_context.Partners, "PartnerId", "Name", athlete.PartnerId);
            return View(athlete);
        }

        // GET: Athletes/Edit - Admin, Coach, and Partner can edit
        [Authorize(Roles = "Admin,Coach,Partner")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var athlete = await _context.Athletes.FindAsync(id);
            if (athlete == null)
            {
                return NotFound();
            }

            // Handle role-based access and form setup
            if (User.IsInRole("Partner"))
            {
                // Partners can only edit their assigned athletes
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var partner = await _context.Partners.FirstOrDefaultAsync(p => p.UserId == userId);

                if (partner == null || athlete.PartnerId != partner.PartnerId)
                {
                    return Forbid();
                }

                // Partners have limited editing - only ratings and picture
                ViewBag.PartnerEditMode = true;
            }
            else if (User.IsInRole("Admin") || User.IsInRole("Coach"))
            {
                // Admins and Coaches can edit any athlete and have full access
                ViewBag.PartnerEditMode = false;
                ViewData["PartnerId"] = new SelectList(_context.Partners, "PartnerId", "Name", athlete.PartnerId);
            }

            return View(athlete);
        }

        // POST: Athletes/Edit - Admin, Coach, and Partner can edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coach,Partner")]
        public async Task<IActionResult> Edit(int id, [Bind("AthleteId,Name,PictureUrl,SwingRating,PowerRating,UnderstandingRating,PartnerId")] Athlete athlete)
        {
            if (id != athlete.AthleteId)
            {
                return NotFound();
            }

            // Handle role-based restrictions
            if (User.IsInRole("Partner"))
            {
                // Partners can only edit their assigned athletes
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var partner = await _context.Partners.FirstOrDefaultAsync(p => p.UserId == userId);

                if (partner == null || athlete.PartnerId != partner.PartnerId)
                {
                    return Forbid();
                }

                var existingAthlete = await _context.Athletes.AsNoTracking().FirstOrDefaultAsync(a => a.AthleteId == id);
                if (existingAthlete != null)
                {
                    athlete.Name = existingAthlete.Name; // Partners can't change name
                    athlete.PartnerId = existingAthlete.PartnerId; // Partners can't reassign athletes
                    athlete.UserId = existingAthlete.UserId; // Preserve UserId
                }
            }
            else if (User.IsInRole("Admin") || User.IsInRole("Coach"))
            {
                // Admins and Coaches can edit everything, but preserve UserId
                var existingAthlete = await _context.Athletes.AsNoTracking().FirstOrDefaultAsync(a => a.AthleteId == id);
                if (existingAthlete != null)
                {
                    athlete.UserId = existingAthlete.UserId; // Preserve UserId
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(athlete);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AthleteExists(athlete.AthleteId))
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

            // Repopulate dropdowns if validation fails
            if (User.IsInRole("Admin") || User.IsInRole("Coach"))
            {
                ViewData["PartnerId"] = new SelectList(_context.Partners, "PartnerId", "Name", athlete.PartnerId);
            }
            return View(athlete);
        }

        // GET: Athletes/Delete/5 - Only Admin and Coach can get athletes
        [Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var athlete = await _context.Athletes
                .Include(a => a.Partner)
                .FirstOrDefaultAsync(m => m.AthleteId == id);
            if (athlete == null)
            {
                return NotFound();
            }

            return View(athlete);
        }

        // POST: Athletes/Delete/5 - Only Admin and Coach can delete athletes
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var athlete = await _context.Athletes.FindAsync(id);
            if (athlete != null)
            {
                _context.Athletes.Remove(athlete);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AthleteExists(int id)
        {
            return _context.Athletes.Any(e => e.AthleteId == id);
        }
    }
}
