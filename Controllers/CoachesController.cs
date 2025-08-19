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
using GolfTeamApp.Services;
using Microsoft.AspNetCore.Identity;

namespace GolfTeamApp.Controllers
{
    public class CoachesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserRoleHelper _userRoleHelper;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public CoachesController(ApplicationDbContext context, UserRoleHelper userRoleHelper,
            SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userRoleHelper = userRoleHelper;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // GET: Coaches - Admin and Coach can view
        [Authorize(Roles = "Admin,Coach,Partner")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Coaches.ToListAsync());
        }

        // GET: Coaches/Details - Admin and Coach can view details
        [Authorize(Roles = "Admin,Coach,Partner")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coach = await _context.Coaches
                .FirstOrDefaultAsync(m => m.CoachId == id);
            if (coach == null)
            {
                return NotFound();
            }

            return View(coach);
        }

        // GET: Coaches/Create - Anyone can create their own profile
        [Authorize]
        public IActionResult Create()
        {
            // Admin creating for someone else
            if (User.IsInRole("Admin"))
            {
                return View();
            }

            // User creating their own profile - check if they already have one
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingCoach = _context.Coaches.FirstOrDefault(c => c.UserId == userId);

            if (existingCoach != null)
            {
                TempData["Message"] = "You already have a coach profile.";
                return RedirectToAction("Details", new { id = existingCoach.CoachId });
            }

            return View();
        }

        // POST: Coaches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("CoachId,Name,Email,Phone")] Coach coach)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (User.IsInRole("Admin"))
                {
                    // Admin is creating a coach profile for someone else
                    _context.Add(coach);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Coach profile created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // User is creating their own coach profile
                    coach.UserId = userId;
                    _context.Add(coach);
                    await _context.SaveChangesAsync();

                    // Assign Coach role to the user using UserRoleHelper
                    var roleAssigned = await _userRoleHelper.AssignCoachRoleAsync(userId, coach.CoachId);

                    if (roleAssigned)
                    {
                        // Refresh the user's authentication cookie to include new role
                        var user = await _userManager.FindByIdAsync(userId);
                        if (user != null)
                        {
                            await _signInManager.RefreshSignInAsync(user);
                        }

                        TempData["Success"] = "Coach profile created successfully!";
                        return RedirectToAction("CoachDashboard", "Dashboard");
                    }
                    else
                    {
                        TempData["Error"] = "Profile created but there was an issue assigning the Coach role. Please contact support.";
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
            return View(coach);
        }

        // GET: Coaches/Edit - Only Admin can edit ANY coach, Coaches can edit their OWN
        [Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coach = await _context.Coaches.FindAsync(id);
            if (coach == null)
            {
                return NotFound();
            }

            // Admin can edit any coach
            if (User.IsInRole("Admin"))
            {
                return View(coach);
            }

            // Coaches can only edit their own profile
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (coach.UserId != userId)
            {
                return Forbid();
            }

            return View(coach);
        }

        // POST: Coaches/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> Edit(int id, [Bind("CoachId,Name,Email,Phone,UserId")] Coach coach)
        {
            if (id != coach.CoachId)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Admin can edit any coach
            if (User.IsInRole("Admin"))
            {
                // Admin editing - preserve existing UserId if not provided
                if (string.IsNullOrEmpty(coach.UserId))
                {
                    var existingCoach = await _context.Coaches.AsNoTracking().FirstOrDefaultAsync(c => c.CoachId == id);
                    if (existingCoach != null)
                    {
                        coach.UserId = existingCoach.UserId;
                    }
                }
            }
            else if (User.IsInRole("Coach"))
            {
                // Coaches can only edit their own profile
                var existingCoach = await _context.Coaches.AsNoTracking().FirstOrDefaultAsync(c => c.CoachId == id);
                if (existingCoach == null || existingCoach.UserId != userId)
                {
                    return Forbid(); // Coach trying to edit someone else's profile
                }

                // Preserve the UserId for the coach
                coach.UserId = existingCoach.UserId;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(coach);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CoachExists(coach.CoachId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                // Different redirects based on role
                if (User.IsInRole("Admin"))
                {
                    TempData["Success"] = "Coach profile updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Success"] = "Your profile has been updated successfully!";
                    return RedirectToAction("CoachDashboard", "Dashboard");
                }
            }

            return View(coach);
        }

        // GET: Coaches/Delete - Only Admin can delete coaches
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coach = await _context.Coaches
                .FirstOrDefaultAsync(m => m.CoachId == id);
            if (coach == null)
            {
                return NotFound();
            }

            return View(coach);
        }

        // POST: Coaches/Delete - Only Admin can delete coaches
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var coach = await _context.Coaches.FindAsync(id);
            if (coach != null)
            {
                _context.Coaches.Remove(coach);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CoachExists(int id)
        {
            return _context.Coaches.Any(e => e.CoachId == id);
        }
    }
}
