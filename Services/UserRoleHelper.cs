using Microsoft.AspNetCore.Identity;
using GolfTeamApp.Data;
using GolfTeamApp.Models;

namespace GolfTeamApp.Services
{
    public class UserRoleHelper
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UserRoleHelper(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<bool> AssignCoachRoleAsync(string userId, int coachId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Add Coach role
            var result = await _userManager.AddToRoleAsync(user, "Coach");

            // Update Coach record with UserId
            var coach = await _context.Coaches.FindAsync(coachId);
            if (coach != null)
            {
                coach.UserId = userId;
                await _context.SaveChangesAsync();
            }

            return result.Succeeded;
        }

        public async Task<bool> AssignPartnerRoleAsync(string userId, int partnerId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Add Partner role
            var result = await _userManager.AddToRoleAsync(user, "Partner");

            // Update Partner record with UserId
            var partner = await _context.Partners.FindAsync(partnerId);
            if (partner != null)
            {
                partner.UserId = userId;
                await _context.SaveChangesAsync();
            }

            return result.Succeeded;
        }

        public async Task<bool> AssignAthleteRoleAsync(string userId, int athleteId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Add Athlete role
            var result = await _userManager.AddToRoleAsync(user, "Athlete");

            // Update Athlete record with UserId
            var athlete = await _context.Athletes.FindAsync(athleteId);
            if (athlete != null)
            {
                athlete.UserId = userId;
                await _context.SaveChangesAsync();
            }

            return result.Succeeded;
        }
    }
}