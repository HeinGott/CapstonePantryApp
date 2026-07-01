using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pantreats.Models;
using System.Security.Claims;

namespace Pantreats.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public StudentController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var applications = await _context.UserApplications
                .AsNoTracking()
                .OrderByDescending(application => application.RegistrationDate)
                .ThenByDescending(application => application.ApplicationId)
                .ToListAsync();

            var latestApplications = applications
                .GroupBy(application => application.UserId)
                .Select(group => group.First())
                .ToList();

            var userIds = latestApplications
                .Select(application => application.UserId)
                .Distinct()
                .ToList();

            var usersById = await _context.Users
                .AsNoTracking()
                .Where(user => userIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id);

            var summaries = latestApplications
                .Select(application => new StudentApplicationSummaryViewModel
                {
                    ApplicationId = application.ApplicationId,
                    StudentId = application.StudentId,
                    FullName = BuildFullName(application.FirstName, application.MiddleName, application.LastName),
                    Email = usersById.TryGetValue(application.UserId, out var user) ? user.Email ?? string.Empty : string.Empty,
                    ApplicationStatus = string.IsNullOrWhiteSpace(application.ApplicationStatus)
                        ? ApplicationStatuses.Pending
                        : application.ApplicationStatus,
                    SubmittedAt = application.RegistrationDate
                })
                .OrderBy(summary => summary.FullName)
                .ToList();

            var model = new StudentAdminIndexViewModel
            {
                PendingApplications = summaries
                    .Where(summary => summary.ApplicationStatus == ApplicationStatuses.Pending)
                    .OrderBy(summary => summary.SubmittedAt)
                    .ToList(),
                ApprovedStudents = summaries
                    .Where(summary => summary.ApplicationStatus == ApplicationStatuses.Approved)
                    .OrderBy(summary => summary.FullName)
                    .ToList()
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var application = await _context.UserApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(foundApplication => foundApplication.ApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(foundUser => foundUser.Id == application.UserId);

            var email = user?.Email ?? string.Empty;

            var orders = await _context.Orders
                .AsNoTracking()
                .Include(order => order.OrderItems)
                .Include(order => order.OrderFulfilment)
                .Where(order =>
                    order.UserId == application.UserId ||
                    (!string.IsNullOrWhiteSpace(email) &&
                     (order.UserId == email || order.Email == email)))
                .OrderByDescending(order => order.OrderDate)
                .ToListAsync();

            var normalizedStatus = string.IsNullOrWhiteSpace(application.ApplicationStatus)
                ? ApplicationStatuses.Pending
                : application.ApplicationStatus;

            var model = new StudentDetailsViewModel
            {
                ApplicationId = application.ApplicationId,
                StudentId = application.StudentId,
                FullName = BuildFullName(application.FirstName, application.MiddleName, application.LastName),
                FirstName = application.FirstName,
                MiddleName = application.MiddleName,
                LastName = application.LastName,
                Email = email,
                PhoneNumber = application.PhoneNum,
                Campus = application.Campus,
                DateOfBirth = application.DOB,
                Gender = application.Gender,
                StudentStatus = application.StudentStatus,
                HasTransportation = application.HasTransportation,
                EmploymentStatus = application.EmploymentStatus,
                EmployedHouseMembers = application.EmployedHouseMembers,
                HouseholdBabiesToddlers = application.HouseholdBabiesToddlers,
                HouseholdChildren = application.HouseholdBabiesChildren,
                HouseholdTeens = application.HouseholdTeens,
                HouseholdAdults = application.HouseholdAdults,
                HouseholdTotal = application.HouseholdBabiesToddlers +
                                 application.HouseholdBabiesChildren +
                                 application.HouseholdTeens +
                                 application.HouseholdAdults,
                HasSnap = application.HasSNAP,
                HasWic = application.HasWIC,
                HasTanf = application.HasTANF,
                InterestedInSnap = application.IsInterestedInSNAP,
                InterestedInWic = application.IsInterestedInWIC,
                InterestedInTanf = application.IsInterestedInTANF,
                ApplicationStatus = normalizedStatus,
                SubmittedAt = application.RegistrationDate,
                ReviewedAt = application.ReviewedAt,
                ReviewNotes = application.ReviewNotes,
                TotalOrders = orders.Count,
                ShowOrderHistory = normalizedStatus == ApplicationStatuses.Approved,
                OrderHistory = orders
                    .Select(order => new StudentHistoryItemViewModel
                    {
                        OrderId = order.OrderId,
                        OrderDate = order.OrderDate,
                        Status = order.OrderFulfilment?.OrderStatus ?? "Submitted",
                        ItemCount = order.OrderItems.Sum(orderItem => orderItem.OrderQuantity),
                        PointsUsed = order.Total
                    })
                    .ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> Apply()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var application = await GetLatestApplicationForUserAsync(userId);

            if (application?.ApplicationStatus == ApplicationStatuses.Pending ||
                application?.ApplicationStatus == ApplicationStatuses.Approved)
            {
                return RedirectToAction(nameof(Status));
            }

            var model = application == null
                ? new UserApplicationViewModel()
                : new UserApplicationViewModel
                {
                    StudentId = application.StudentId,
                    FirstName = application.FirstName,
                    MiddleName = application.MiddleName,
                    LastName = application.LastName,
                    DOB = application.DOB,
                    PhoneNum = application.PhoneNum,
                    Gender = application.Gender,
                    StudentStatus = application.StudentStatus,
                    Campus = application.Campus,
                    HouseholdBabiesToddlers = application.HouseholdBabiesToddlers,
                    HouseholdBabiesChildren = application.HouseholdBabiesChildren,
                    HouseholdTeens = application.HouseholdTeens,
                    HouseholdAdults = application.HouseholdAdults,
                    HasTransportation = application.HasTransportation ?? false,
                    EmploymentStatus = application.EmploymentStatus,
                    EmployedHouseMembers = application.EmployedHouseMembers,
                    HasSNAP = application.HasSNAP,
                    HasWIC = application.HasWIC,
                    HasTANF = application.HasTANF,
                    IsInterestedInSNAP = application.IsInterestedInSNAP,
                    IsInterestedInWIC = application.IsInterestedInWIC,
                    IsInterestedInTANF = application.IsInterestedInTANF
                };

            return View(model);
        }

        public async Task<IActionResult> Status()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var application = await GetLatestApplicationForUserAsync(userId);

            if (application == null)
            {
                return RedirectToAction(nameof(Apply));
            }

            var model = new StudentApplicationStatusViewModel
            {
                ApplicationId = application.ApplicationId,
                FullName = BuildFullName(application.FirstName, application.MiddleName, application.LastName),
                StudentId = application.StudentId,
                ApplicationStatus = string.IsNullOrWhiteSpace(application.ApplicationStatus)
                    ? ApplicationStatuses.Pending
                    : application.ApplicationStatus,
                SubmittedAt = application.RegistrationDate,
                ReviewedAt = application.ReviewedAt,
                ReviewNotes = application.ReviewNotes,
                CanEditApplication = application.ApplicationStatus == ApplicationStatuses.Rejected
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserApplicationForm(UserApplicationViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            if (model.DOB > DateTime.Today)
            {
                ModelState.AddModelError("DOB", "Date of birth cannot be in the future.");
            }

            if (!ModelState.IsValid)
            {
                return View("Apply", model);
            }

            var application = await GetLatestApplicationForUserAsync(userId);

            if (application == null)
            {
                application = new UserApplication
                {
                    UserId = userId
                };

                _context.UserApplications.Add(application);
            }

            application.StudentId = model.StudentId;
            application.FirstName = model.FirstName;
            application.MiddleName = model.MiddleName;
            application.LastName = model.LastName;
            application.DOB = model.DOB;
            application.PhoneNum = model.PhoneNum;
            application.Gender = model.Gender;
            application.StudentStatus = model.StudentStatus;
            application.HouseholdBabiesToddlers = model.HouseholdBabiesToddlers;
            application.HouseholdBabiesChildren = model.HouseholdBabiesChildren;
            application.HouseholdTeens = model.HouseholdTeens;
            application.HouseholdAdults = model.HouseholdAdults;
            application.HasTransportation = model.HasTransportation;
            application.EmploymentStatus = model.EmploymentStatus;
            application.EmployedHouseMembers = model.EmployedHouseMembers;
            application.HasSNAP = model.HasSNAP;
            application.HasWIC = model.HasWIC;
            application.HasTANF = model.HasTANF;
            application.IsInterestedInSNAP = model.IsInterestedInSNAP;
            application.IsInterestedInWIC = model.IsInterestedInWIC;
            application.IsInterestedInTANF = model.IsInterestedInTANF;
            application.Campus = model.Campus;
            application.IsActive = false;
            application.ApplicationStatus = ApplicationStatuses.Pending;
            application.RegistrationDate = DateTime.UtcNow;
            application.ReviewedAt = null;
            application.ReviewedByUserId = null;
            application.ReviewNotes = null;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Status));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? reviewNotes)
        {
            var application = await _context.UserApplications
                .FirstOrDefaultAsync(foundApplication => foundApplication.ApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            application.ApplicationStatus = ApplicationStatuses.Approved;
            application.IsActive = true;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            application.ReviewNotes = string.IsNullOrWhiteSpace(reviewNotes) ? null : reviewNotes.Trim();

            var user = await _userManager.FindByIdAsync(application.UserId);
            if (user != null && !await _userManager.IsInRoleAsync(user, "Students"))
            {
                await _userManager.AddToRoleAsync(user, "Students");
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reviewNotes)
        {
            var application = await _context.UserApplications
                .FirstOrDefaultAsync(foundApplication => foundApplication.ApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            application.ApplicationStatus = ApplicationStatuses.Rejected;
            application.IsActive = false;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            application.ReviewNotes = string.IsNullOrWhiteSpace(reviewNotes) ? null : reviewNotes.Trim();

            var user = await _userManager.FindByIdAsync(application.UserId);
            if (user != null && await _userManager.IsInRoleAsync(user, "Students"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Students");
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteApplication(int id)
        {
            var application = await _context.UserApplications
                .FirstOrDefaultAsync(foundApplication => foundApplication.ApplicationId == id);

            if (application == null)
            {
                TempData["StatusMessage"] = "That student application no longer exists.";
                TempData["StatusType"] = "error";
                return RedirectToAction(nameof(Index));
            }

            _context.UserApplications.Remove(application);

            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Student application deleted.";
            TempData["StatusType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        private async Task<UserApplication?> GetLatestApplicationForUserAsync(string userId)
        {
            return await _context.UserApplications
                .OrderByDescending(application => application.RegistrationDate)
                .ThenByDescending(application => application.ApplicationId)
                .FirstOrDefaultAsync(application => application.UserId == userId);
        }

        private static string BuildFullName(string firstName, string middleName, string lastName)
        {
            return string.Join(" ", new[] { firstName, middleName, lastName }
                .Where(namePart => !string.IsNullOrWhiteSpace(namePart)));
        }
    }
}
