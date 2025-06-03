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
    [Authorize] // Require authentication for all actions
    public class PartnersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserRoleHelper _userRoleHelper;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public PartnersController(ApplicationDbContext context, UserRoleHelper userRoleHelper,
            SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userRoleHelper = userRoleHelper;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // GET: Partners - Admin, Coach, and Partner can view
        [Authorize(Roles = "Admin,Coach,Partner")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Partners.ToListAsync());
        }

        // GET: Partners/Details/5 - Admin, Coach, and Partner can view details
        [Authorize(Roles = "Admin,Coach,Partner")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var partner = await _context.Partners
                .FirstOrDefaultAsync(m => m.PartnerId == id);
            if (partner == null)
            {
                return NotFound();
            }

            return View(partner);
        }

        // GET: Partners/Create - Allow any authenticated user to create their profile OR admin/coaches to create partner profiles
        [Authorize]
        public IActionResult Create()
        {
            // If user is an Admin or Coach, they're creating a profile for someone else (don't check for existing)
            if (User.IsInRole("Admin") || User.IsInRole("Coach"))
            {
                return View();
            }

            // If user is NOT an Admin or Coach, check if they already have a partner profile (creating their own)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingPartner = _context.Partners.FirstOrDefault(p => p.UserId == userId);

            if (existingPartner != null)
            {
                TempData["Message"] = "You already have a partner profile.";
                return RedirectToAction("Details", new { id = existingPartner.PartnerId });
            }

            return View();
        }

        // POST: Partners/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("PartnerId,Name,Email,Phone")] Partner partner)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Determine if this is a self-registration or admin/coach creating for someone else
                if (User.IsInRole("Admin") || User.IsInRole("Coach"))
                {
                    // Admin or Coach is creating a partner profile for someone else
                    // DO NOT link to the admin's/coach's user account
                    // DO NOT assign Partner role to the admin/coach

                    _context.Add(partner);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Partner profile created successfully!";
                    return RedirectToAction(nameof(Index)); // Return to Partners list
                }
                else
                {
                    // User is creating their own partner profile
                    // Link to their user account and assign the Partner role

                    partner.UserId = userId;
                    _context.Add(partner);
                    await _context.SaveChangesAsync();

                    // Assign Partner role to the user
                    var roleAssigned = await _userRoleHelper.AssignPartnerRoleAsync(userId, partner.PartnerId);

                    if (roleAssigned)
                    {
                        // Refresh the user's authentication cookie to include new role
                        var user = await _userManager.FindByIdAsync(userId);
                        if (user != null)
                        {
                            await _signInManager.RefreshSignInAsync(user);
                        }

                        TempData["Success"] = "Partner profile created successfully!";
                        return RedirectToAction("PartnerDashboard", "Dashboard");
                    }
                    else
                    {
                        TempData["Error"] = "Profile created but there was an issue assigning the Partner role. Please contact support.";
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
            return View(partner);
        }

        // GET: Partners/Edit/5 - Admin can edit any, Coach and Partner can edit their own
        [Authorize(Roles = "Admin,Coach,Partner")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var partner = await _context.Partners.FindAsync(id);
            if (partner == null)
            {
                return NotFound();
            }

            // Admin can edit any partner profile
            if (User.IsInRole("Admin"))
            {
                return View(partner);
            }

            // Only allow partners to edit their own profile (or coaches to edit any)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Coach") && partner.UserId != userId)
            {
                return Forbid();
            }

            return View(partner);
        }

        // POST: Partners/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coach,Partner")]
        public async Task<IActionResult> Edit(int id, [Bind("PartnerId,Name,Email,Phone,UserId")] Partner partner)
        {
            if (id != partner.PartnerId)
            {
                return NotFound();
            }

            // Admin can edit any partner profile
            if (User.IsInRole("Admin"))
            {
                // Admin editing - preserve existing UserId
                var existingPartner = await _context.Partners.AsNoTracking().FirstOrDefaultAsync(p => p.PartnerId == id);
                if (existingPartner != null)
                {
                    partner.UserId = existingPartner.UserId;
                }
            }
            else
            {
                // Only allow partners to edit their own profile (or coaches to edit any)
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!User.IsInRole("Coach") && partner.UserId != userId)
                {
                    return Forbid();
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(partner);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PartnerExists(partner.PartnerId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction(nameof(Index));
                }
                else if (User.IsInRole("Partner"))
                {
                    return RedirectToAction("PartnerDashboard", "Dashboard");
                }
                else
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(partner);
        }

        // GET: Partners/Delete/5 - Only Admin can delete partners
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var partner = await _context.Partners
                .FirstOrDefaultAsync(m => m.PartnerId == id);
            if (partner == null)
            {
                return NotFound();
            }

            return View(partner);
        }

        // POST: Partners/Delete/5 - Only Admin can delete partners
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var partner = await _context.Partners.FindAsync(id);
            if (partner != null)
            {
                _context.Partners.Remove(partner);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PartnerExists(int id)
        {
            return _context.Partners.Any(e => e.PartnerId == id);
        }
    }
}