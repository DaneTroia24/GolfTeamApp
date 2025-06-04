using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GolfTeamApp.Data;
using GolfTeamApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace GolfTeamApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDataController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminDataController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminData/AllData
        public async Task<IActionResult> AllData()
        {
            // Get all athletes with their partner information
            var athletes = await _context.Athletes
                .Include(a => a.Partner)
                .Include(a => a.EventScores)
                    .ThenInclude(es => es.Event)
                .Include(a => a.EventScores)
                    .ThenInclude(es => es.EnteredByPartner)
                .OrderBy(a => a.Name)
                .ToListAsync();

            // Get all events ordered by date
            var allEvents = await _context.GolfEvents
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            // Create view model
            var viewModel = new AdminDataViewModel
            {
                Athletes = athletes,
                AllEvents = allEvents
            };

            return View(viewModel);
        }
    }

    // View Model for the Admin Data page
    public class AdminDataViewModel
    {
        public List<Athlete> Athletes { get; set; } = new List<Athlete>();
        public List<GolfEvent> AllEvents { get; set; } = new List<GolfEvent>();
    }
}