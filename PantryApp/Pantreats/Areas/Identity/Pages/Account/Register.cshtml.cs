// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Pantreats.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Pantreats.Services;

namespace Pantreats.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IEmailService _emailService;

        public RegisterModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public string AccountType { get; private set; } = string.Empty;

        public bool ShowRoleChooser => string.IsNullOrWhiteSpace(AccountType);

        public bool IsStudentRegistration => AccountType == "student";

        public bool IsDonorRegistration => AccountType == "donor";

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Display(Name = "Full name")]
            public string FullName { get; set; }

            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Address")]
            public string Address { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null, string accountType = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                Response.Redirect(GetSignedInRedirectUrl());
                return;
            }

            ReturnUrl = returnUrl;
            AccountType = NormalizeAccountType(accountType);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null, string accountType = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return LocalRedirect(GetSignedInRedirectUrl());
            }

            returnUrl ??= Url.Content("~/");
            AccountType = NormalizeAccountType(accountType);
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ShowRoleChooser)
            {
                ModelState.AddModelError(string.Empty, "Choose whether you're registering as a student or donor.");
                return Page();
            }

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                user.PhoneNumber = Input.PhoneNumber?.Trim();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    if (IsStudentRegistration)
                    {
                        await _userManager.AddToRoleAsync(user, "Students");
                    }

                    if (IsDonorRegistration)
                    {
                        await _userManager.AddToRoleAsync(user, "Donors");
                        await EnsureDonorProfileAsync(user.Id);
                    }

                    var codeSent = await SendNewVerificationCodeAsync(user.Id, Input.Email);

                    if (!codeSent)
                    {
                        ModelState.AddModelError(string.Empty, "Failed to send verification code. Please try again.");
                        return Page();
                    }

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new
                        {
                            email = Input.Email,
                            returnUrl,
                            accountType = AccountType,
                            continueUrl = GetContinueUrl(AccountType)
                        });
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(GetContinueUrl(AccountType) ?? returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        private async Task EnsureDonorProfileAsync(string userId)
        {
            var existingDonor = await _context.Donors.FirstOrDefaultAsync(donor => donor.UserId == userId);
            if (existingDonor != null)
            {
                existingDonor.Name = string.IsNullOrWhiteSpace(Input.FullName) ? existingDonor.Name : Input.FullName.Trim();
                existingDonor.PhoneNumber = string.IsNullOrWhiteSpace(Input.PhoneNumber) ? existingDonor.PhoneNumber : Input.PhoneNumber.Trim();
                existingDonor.Email = Input.Email;
                await _context.SaveChangesAsync();
                return;
            }

            _context.Donors.Add(new Donor
            {
                UserId = userId,
                Email = Input.Email,
                Name = string.IsNullOrWhiteSpace(Input.FullName) ? Input.Email : Input.FullName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim()
            });

            await _context.SaveChangesAsync();
        }

        private static string GetContinueUrl(string accountType)
        {
            return accountType switch
            {
                "student" => "/Student/Apply",
                "donor" => "/Donor/Dashboard",
                _ => null
            };
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

        private string GetSignedInRedirectUrl()
        {
            if (User.IsInRole("Admin"))
            {
                return Url.Action("Index", "Admin") ?? "/";
            }

            if (User.IsInRole("Donors"))
            {
                return Url.Action("Dashboard", "Donor") ?? "/";
            }

            return Url.Action("Index", "Home") ?? "/";
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }

            return (IUserEmailStore<IdentityUser>)_userStore;
        }
        private async Task<bool> SendNewVerificationCodeAsync(string userId, string email)
        {
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            _context.EmailVerificationCodes.Add(new EmailVerificationCode
            {
                UserId = userId,
                Code = code,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            });

            await _context.SaveChangesAsync();

            return await _emailService.SendVerificationCodeAsync(email, code);
        }
    }
}
