// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Pantreats.Data;
using Pantreats.Models;
using Pantreats.Services;
using System.ComponentModel.DataAnnotations;

namespace Pantreats.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public RegisterConfirmationModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailService = emailService;
        }

        [BindProperty]
        [Required(ErrorMessage = "Please enter the 6-digit code.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits.")]
        public string Code { get; set; }

        public string Email { get; set; }

        public string ReturnUrl { get; set; }

        public string AccountType { get; set; } = string.Empty;

        public string ContinueUrl { get; set; } = string.Empty;

        public string ContinueLabel => AccountType == "student" ? "Go To Student Application" : "Go To Donor Dashboard";

        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null, string accountType = null, string continueUrl = null)
        {
            if (email == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Unable to load user with email '{email}'.");
            }

            Email = email;
            ReturnUrl = returnUrl ?? Url.Content("~/");
            AccountType = NormalizeAccountType(accountType);
            ContinueUrl = string.IsNullOrWhiteSpace(continueUrl) ? GetDefaultContinueUrl(AccountType) : continueUrl;

            return Page();
        }

        public async Task<IActionResult> OnPostConfirmAsync(string email, string returnUrl, string accountType, string continueUrl)
        {
            Email = email;
            ReturnUrl = returnUrl ?? Url.Content("~/");
            AccountType = NormalizeAccountType(accountType);
            ContinueUrl = string.IsNullOrWhiteSpace(continueUrl) ? GetDefaultContinueUrl(AccountType) : continueUrl;

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Unable to load user with email '{email}'.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var latestCode = await _context.EmailVerificationCodes
                .Where(existingCode => existingCode.UserId == user.Id)
                .OrderByDescending(existingCode => existingCode.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestCode == null)
            {
                StatusMessage = "No code on file. Please request a new one.";
                return Page();
            }

            if (latestCode.ExpiresAt < DateTime.UtcNow)
            {
                StatusMessage = "That code has expired. Please request a new one.";
                return Page();
            }

            if (latestCode.FailedAttempts >= 5)
            {
                StatusMessage = "Too many incorrect attempts. Please request a new code.";
                return Page();
            }

            if (latestCode.Code != Code.Trim())
            {
                latestCode.FailedAttempts += 1;
                await _context.SaveChangesAsync();
                StatusMessage = "Incorrect code. Please try again.";
                return Page();
            }

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> OnPostResendAsync(string email, string returnUrl, string accountType, string continueUrl)
        {
            Email = email;
            ReturnUrl = returnUrl ?? Url.Content("~/");
            AccountType = NormalizeAccountType(accountType);
            ContinueUrl = string.IsNullOrWhiteSpace(continueUrl) ? GetDefaultContinueUrl(AccountType) : continueUrl;

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Unable to load user with email '{email}'.");
            }

            var code = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            _context.EmailVerificationCodes.Add(new EmailVerificationCode
            {
                UserId = user.Id,
                Code = code,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            });

            await _context.SaveChangesAsync();

            var sent = await _emailService.SendVerificationCodeAsync(email, code);

            StatusMessage = sent
                ? "A new code has been sent to your email."
                : "Failed to send a new code. Please try again.";

            ModelState.Clear();
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