// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Pantreats.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _sender;

        public RegisterConfirmationModel(UserManager<ApplicationUser> userManager, IEmailSender sender)
        {
            _userManager = userManager;
            _sender = sender;
        }

        public string Email { get; set; }

        public bool DisplayConfirmAccountLink { get; set; }

        public string EmailConfirmationUrl { get; set; }

        public string AccountType { get; set; } = string.Empty;

        public string ContinueUrl { get; set; } = string.Empty;

        public string ContinueLabel => AccountType == "student" ? "Go To Student Application" : "Go To Donor Dashboard";

        public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null, string accountType = null, string continueUrl = null)
        {
            if (email == null)
            {
                return RedirectToPage("/Index");
            }

            returnUrl ??= Url.Content("~/");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Unable to load user with email '{email}'.");
            }

            Email = email;
            AccountType = NormalizeAccountType(accountType);
            ContinueUrl = string.IsNullOrWhiteSpace(continueUrl) ? GetDefaultContinueUrl(AccountType) : continueUrl;

            DisplayConfirmAccountLink = true;
            if (DisplayConfirmAccountLink)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                EmailConfirmationUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId, code, returnUrl },
                    protocol: Request.Scheme);
            }

            return Page();
        }

        private static string NormalizeAccountType(string accountType)
        {
            return (accountType ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "student" => "student",
                "donor" => "donor",
                _ => string.Empty
            };
        }

        private static string GetDefaultContinueUrl(string accountType)
        {
            return accountType == "student" ? "/Student/Apply" : "/Donor/Dashboard";
        }
    }
}
