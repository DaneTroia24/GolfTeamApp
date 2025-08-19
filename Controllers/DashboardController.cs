using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GolfTeamApp.Data;
using GolfTeamApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace GolfTeamApp.Controllers
{
    [Authorize]  // Require authentication for all dashboard actions
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }



        // Coach Dashboard - Overview of entire team
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> CoachDashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var coach = await _context.Coaches.FirstOrDefaultAsync(c => c.UserId == userId);

            if (coach == null)
            {
                // If no coach profile exists, redirect to create one
                TempData["Message"] = "Please complete your coach profile first.";
                return RedirectToAction("Create", "Coaches");
            }

            // Get comprehensive team statistics
            ViewBag.TotalAthletes = await _context.Athletes.CountAsync();
            ViewBag.TotalEvents = await _context.GolfEvents.CountAsync();
            ViewBag.TotalScores = await _context.EventScores.CountAsync();
            ViewBag.TotalPartners = await _context.Partners.CountAsync();
            ViewBag.AthletesWithPartners = await _context.Athletes.Where(a => a.PartnerId > 0).CountAsync();

            // Get additional stats for team status
            var latestScore = await _context.EventScores
                .OrderByDescending(s => s.Timestamp)
                .FirstOrDefaultAsync();
            ViewBag.LatestScoreDate = latestScore?.Timestamp.ToString("MMM dd, yyyy") ?? "No scores yet";

            var nextEvent = await _context.GolfEvents
                .Where(e => e.EventDate > DateTime.Now)
                .OrderBy(e => e.EventDate)
                .FirstOrDefaultAsync();
            ViewBag.NextEventDate = nextEvent?.EventDate.ToString("MMM dd, yyyy") ?? "No upcoming events";

            // Get recent events (show all events for coaches)
            var recentEvents = await _context.GolfEvents
                .Include(e => e.CreatedByCoach)
                .OrderByDescending(e => e.EventDate)
                .Take(5)
                .ToListAsync();

            ViewBag.CoachName = coach.Name;
            return View(recentEvents);
        }

        // Partner Dashboard - Show only their assigned athlete
        [Authorize(Roles = "Partner")]
        public async Task<IActionResult> PartnerDashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var partner = await _context.Partners.FirstOrDefaultAsync(p => p.UserId == userId);

            if (partner == null)
            {
                // If no partner profile exists, redirect to create one
                TempData["Message"] = "Please complete your partner profile first.";
                return RedirectToAction("Create", "Partners");
            }

            // Get the partner's assigned athlete(s)
            var athlete = await _context.Athletes
                .Include(a => a.Partner)
                .Include(a => a.EventScores)
                    .ThenInclude(es => es.Event)
                .FirstOrDefaultAsync(a => a.PartnerId == partner.PartnerId);

            ViewBag.PartnerName = partner.Name;
            return View(athlete);
        }

        // Athlete Dashboard - Read-only view of their own data
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> AthleteDashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var athlete = await _context.Athletes
                .Include(a => a.Partner)
                .Include(a => a.EventScores)
                    .ThenInclude(es => es.Event)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (athlete == null)
            {
                // If no athlete profile exists, show a message
                TempData["Message"] = "No athlete profile found. Please contact your coach.";
                return RedirectToAction("Index", "Home");
            }

            return View(athlete);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            // Get system statistics
            ViewBag.TotalAthletes = await _context.Athletes.CountAsync();
            ViewBag.TotalEvents = await _context.GolfEvents.CountAsync();
            ViewBag.TotalScores = await _context.EventScores.CountAsync();
            ViewBag.TotalPartners = await _context.Partners.CountAsync();
            ViewBag.TotalCoaches = await _context.Coaches.CountAsync();

            // Get users without profiles
            var allUsers = _userManager.Users.ToList();
            var usersWithoutProfiles = 0;

            foreach (var user in allUsers)
            {
                var hasCoachProfile = await _context.Coaches.AnyAsync(c => c.UserId == user.Id);
                var hasPartnerProfile = await _context.Partners.AnyAsync(p => p.UserId == user.Id);
                var hasAthleteProfile = await _context.Athletes.AnyAsync(a => a.UserId == user.Id);

                if (!hasCoachProfile && !hasPartnerProfile && !hasAthleteProfile)
                {
                    usersWithoutProfiles++;
                }
            }

            ViewBag.UsersWithoutProfiles = usersWithoutProfiles;
            ViewBag.TotalRegisteredUsers = allUsers.Count;

            // Get recent events for overview
            var recentEvents = await _context.GolfEvents
                .Include(e => e.CreatedByCoach)
                .OrderByDescending(e => e.EventDate)
                .Take(5)
                .ToListAsync();

            return View(recentEvents);
        }

        // Update the Index method in DashboardController to handle Admin role
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
            {
                return RedirectToAction("AdminDashboard");
            }
            else if (roles.Contains("Coach"))
            {
                return RedirectToAction("CoachDashboard");
            }
            else if (roles.Contains("Partner"))
            {
                return RedirectToAction("PartnerDashboard");
            }
            else if (roles.Contains("Athlete"))
            {
                return RedirectToAction("AthleteDashboard");
            }
            else
            {
                // No role assigned yet
                TempData["Message"] = "Please contact an administrator to assign your role.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
