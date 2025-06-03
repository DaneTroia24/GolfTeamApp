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
    public class EventScoresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventScoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: EventScores - Admin, Coach, and Partner can view
        [Authorize(Roles = "Admin,Coach,Partner")]
        public async Task<IActionResult> Index()
        {
            IQueryable<EventScore> eventScores;

            if (User.IsInRole("Admin") || User.IsInRole("Coach"))
            {
                // Admins and Coaches can see all event scores
                eventScores = _context.EventScores
                    .Include(e => e.Athlete)
                    .Include(e => e.EnteredByPartner)
                    .Include(e => e.Event);
            }
            else if (User.IsInRole("Partner"))
            {
                // Partners can only see scores for their assigned athletes
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var partner = await _context.Partners.FirstOrDefaultAsync(p => p.UserId == userId);

                if (partner == null)
                {
                    TempData["Error"] = "Partner profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                eventScores = _context.EventScores
                    .Include(e => e.Athlete)
                    .Include(e => e.EnteredByPartner)
                    .Include(e => e.Event)
                    .Where(es => es.Athlete.PartnerId == partner.PartnerId);
            }
            else
            {
                return Forbid();
            }

            return View(await eventScores.ToListAsync());
        }

        // GET: EventScores/Details/5 - Admin, Coach, and Partner can view
        [Authorize(Roles = "Admin,Coach,Partner")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventScore = await _context.EventScores
                .Include(e => e.Athlete)
                .Include(e => e.EnteredByPartner)
                .Include(e => e.Event)
                .FirstOrDefaultAsync(m => m.ScoreId == id);

            if (eventScore == null)
            {
                return NotFound();
            }

            // Partners can only view scores for their athletes
            if (User.IsInRole("Partner"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var partner = await _context.Partners.FirstOrDefaultAsync(p => p.UserId == userId);

                if (partner == null || eventScore.Athlete.PartnerId != partner.PartnerId)
                {
                    return Forbid();
                }
            }
            // Admins and Coaches can view any score

            return View(eventScore);
        }

        // GET: EventScores/Create - Admin, Coach, and Partner can create
        [Authorize(Roles = "Admin,Coach,Partner")]
        public async Task<IActionResult> Create()
        {
            if (User.IsInRole("Admin") || User.IsInRole("Coach"))
            {
                // Admins and Coaches can create scores for any athlete
                ViewData["AthleteId"] = new SelectList(_context.Athletes, "AthleteId", "Name");
                ViewData["EnteredByPartnerId"] = new SelectList(_context.Partners, "PartnerId", "Name");
            }
            else if (User.IsInRole("Partner"))
            {
                // Partners can only create scores for their assigned athletes
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var partner = await _context.Partners.FirstOrDefaultAsync(p => p.UserId == userId);

                if (partner == null)
                {
                    TempData["Error"] = "Partner profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                var partnerAthletes = _context.Athletes.Where(a => a.PartnerId == partner.PartnerId);
                ViewData["AthleteId"] = new SelectList(partnerAthletes, "AthleteId", "Name");

                // Pre-select the partner as the one entering the score
                ViewData["EnteredByPartnerId"] = new SelectList(_context.Partners.Where(p => p.PartnerId == partner.PartnerId), "PartnerId", "Name", partner.PartnerId);
            }

            ViewData["EventId"] = new SelectList(_context.GolfEvents, "EventId", "Title");
            return View();
        }

        // POST: EventScores/Create - Admin, Coach, and Partner can create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coach,Partner")]
        public async Task<IActionResult> Create([Bind("ScoreId,AthleteId,EventId,EnteredByPartnerId,GolfScore,HolesCompleted")] EventScore eventScore)
        {
            // Automatically set the timestamp to current time
            eventScore.Timestamp = DateTime.Now;

            // Validation for Partners: ensure they can only enter scores for their athletes
            if (User.IsInRole("Partner"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var partner = await _context.Partners.FirstOrDefaultAsync(p => p.UserId == userId);

                if (partner == null)
                {
                    TempData["Error"] = "Partner profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Verify the athlete belongs to this partner
                var athlete = await _context.Athletes.FindAsync(eventScore.AthleteId);
                if (athlete == null || athlete.PartnerId != partner.PartnerId)
                {
                    ModelState.AddModelError("", "You can only enter scores for your assigned athletes.");
                    await PopulateDropdowns();
                    return View(eventScore);
                }

                // Force the partner to be the one entering the score
                eventScore.EnteredByPartnerId = partner.PartnerId;
            }
            // Admins and Coaches can create scores for any athlete without restrictions

            if (ModelState.IsValid)
            {
                _context.Add(eventScore);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns();
            return View(eventScore);
        }

        // GET: EventScores/Edit/5 - Admin, Coach, and Partner can edit
        [Authorize(Roles = "Admin,Coach,Partner")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventScore = await _context.EventScores.FindAsync(id);
            if (eventScore == null)
            {
                return NotFound();
            }

            // Partners can only edit scores they entered for their athletes
            if (User.IsInRole("Partner"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var partner = await _context.Partners.FirstOrDefaultAsync(p => p.UserId == userId);

                if (partner == null || eventScore.EnteredByPartnerId != partner.PartnerId)
                {
                    return Forbid();
                }
            }
            // Admins and Coaches can edit any score

            await PopulateDropdowns(eventScore);
            return View(eventScore);
        }

        // POST: EventScores/Edit/5 - Admin, Coach, and Partner can edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coach,Partner")]
        public async Task<IActionResult> Edit(int id, [Bind("ScoreId,AthleteId,EventId,EnteredByPartnerId,GolfScore,HolesCompleted")] EventScore eventScore)
        {
            if (id != eventScore.ScoreId)
            {
                return NotFound();
            }

            // Get the existing record to preserve the original timestamp
            var existingScore = await _context.EventScores.AsNoTracking().FirstOrDefaultAsync(e => e.ScoreId == id);
            if (existingScore != null)
            {
                eventScore.Timestamp = existingScore.Timestamp; // Preserve original timestamp
            }

            // Partners can only edit scores they entered for their athletes
            if (User.IsInRole("Partner"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var partner = await _context.Partners.FirstOrDefaultAsync(p => p.UserId == userId);

                if (partner == null || eventScore.EnteredByPartnerId != partner.PartnerId)
                {
                    return Forbid();
                }
            }
            // Admins and Coaches can edit any score without restrictions

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(eventScore);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventScoreExists(eventScore.ScoreId))
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

            await PopulateDropdowns(eventScore);
            return View(eventScore);
        }

        // GET: EventScores/Delete/5 - Admin and Coach can delete
        [Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventScore = await _context.EventScores
                .Include(e => e.Athlete)
                .Include(e => e.EnteredByPartner)
                .Include(e => e.Event)
                .FirstOrDefaultAsync(m => m.ScoreId == id);
            if (eventScore == null)
            {
                return NotFound();
            }

            return View(eventScore);
        }

        // POST: EventScores/Delete/5 - Admin and Coach can delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eventScore = await _context.EventScores.FindAsync(id);
            if (eventScore != null)
            {
                _context.EventScores.Remove(eventScore);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EventScoreExists(int id)
        {
            return _context.EventScores.Any(e => e.ScoreId == id);
        }

        private async Task PopulateDropdowns(EventScore eventScore = null)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Coach"))
            {
                ViewData["AthleteId"] = new SelectList(_context.Athletes, "AthleteId", "Name", eventScore?.AthleteId);
                ViewData["EnteredByPartnerId"] = new SelectList(_context.Partners, "PartnerId", "Name", eventScore?.EnteredByPartnerId);
            }
            else if (User.IsInRole("Partner"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var partner = await _context.Partners.FirstOrDefaultAsync(p => p.UserId == userId);

                if (partner != null)
                {
                    var partnerAthletes = _context.Athletes.Where(a => a.PartnerId == partner.PartnerId);
                    ViewData["AthleteId"] = new SelectList(partnerAthletes, "AthleteId", "Name", eventScore?.AthleteId);
                    ViewData["EnteredByPartnerId"] = new SelectList(_context.Partners.Where(p => p.PartnerId == partner.PartnerId), "PartnerId", "Name", eventScore?.EnteredByPartnerId ?? partner.PartnerId);
                }
            }

            ViewData["EventId"] = new SelectList(_context.GolfEvents, "EventId", "Title", eventScore?.EventId);
        }
    }
}