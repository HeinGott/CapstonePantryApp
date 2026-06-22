// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Pantreats.Data;
using Pantreats.Models;

namespace Pantreats.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public ConfirmEmailModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code, string returnUrl = null)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (!result.Succeeded)
            {
                StatusMessage = "Error confirming your email.";
                return Page();
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            var destination = await GetPostConfirmationDestinationAsync(user, returnUrl);
            if (!string.IsNullOrWhiteSpace(destination))
            {
                return LocalRedirect(destination);
            }

            StatusMessage = "Thank you for confirming your email.";
            return Page();
        }

        private async Task<string> GetPostConfirmationDestinationAsync(ApplicationUser user, string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return returnUrl;
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return "/Admin";
            }

            if (await _userManager.IsInRoleAsync(user, "Donors"))
            {
                return "/Donor/Dashboard";
            }

            if (await _userManager.IsInRoleAsync(user, "Students"))
            {
                var latestStatus = await _context.UserApplications
                    .AsNoTracking()
                    .Where(application => application.UserId == user.Id)
                    .OrderByDescending(application => application.RegistrationDate)
                    .ThenByDescending(application => application.ApplicationId)
                    .Select(application => application.ApplicationStatus)
                    .FirstOrDefaultAsync();

                return latestStatus == ApplicationStatuses.Approved
                    ? "/Shop"
                    : "/Student/Status";
            }

            if (await _userManager.IsInRoleAsync(user, "Volunteers"))
            {
                return "/Volunteer/ApplyVolunteer";
            }

            return "/";
        }
    }
}
