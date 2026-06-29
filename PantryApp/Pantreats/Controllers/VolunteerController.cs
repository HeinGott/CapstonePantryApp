using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pantreats.Data;
using Pantreats.Models;
using System.Security.Claims;

namespace Pantreats.Controllers
{
    [Authorize]
    public class VolunteerController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public VolunteerController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var applications = await _context.VolunteerApplications
                .AsNoTracking()
                .OrderByDescending(application => application.SubmittedDate)
                .ThenByDescending(application => application.VolunteerApplicationId)
                .ToListAsync();

            var latestApplications = applications
                .GroupBy(application => application.UserId)
                .Select(group => group.First())
                .ToList();

            var summaries = latestApplications
                .Select(application => new VolunteerApplicationSummaryViewModel
                {
                    VolunteerApplicationId = application.VolunteerApplicationId,
                    FullName = BuildFullName(application.FirstName, application.LastName),
                    Email = application.Email ?? string.Empty,
                    Year = application.Year ?? string.Empty,
                    ApplicationStatus = string.IsNullOrWhiteSpace(application.ApplicationStatus)
                        ? ApplicationStatuses.Pending
                        : application.ApplicationStatus,
                    SubmittedAt = application.SubmittedDate
                })
                .OrderBy(summary => summary.FullName)
                .ToList();

            var model = new VolunteerAdminIndexViewModel
            {
                PendingApplications = summaries
                    .Where(summary => summary.ApplicationStatus == ApplicationStatuses.Pending)
                    .OrderBy(summary => summary.SubmittedAt)
                    .ToList(),
                ApprovedVolunteers = summaries
                    .Where(summary => summary.ApplicationStatus == ApplicationStatuses.Approved)
                    .OrderBy(summary => summary.FullName)
                    .ToList()
            };

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var application = await _context.VolunteerApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(foundApplication => foundApplication.VolunteerApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            var normalizedStatus = string.IsNullOrWhiteSpace(application.ApplicationStatus)
                ? ApplicationStatuses.Pending
                : application.ApplicationStatus;

            var model = new VolunteerDetailsViewModel
            {
                VolunteerApplicationId = application.VolunteerApplicationId,
                FullName = BuildFullName(application.FirstName, application.LastName),
                FirstName = application.FirstName,
                LastName = application.LastName,
                Email = application.Email ?? string.Empty,
                PhoneNumber = application.PhoneNum,
                Year = application.Year ?? string.Empty,

                HasVolunteeredBefore = application.HasVolunteeredBefore,
                PreviousCapacity = application.PreviousCapacity,
                ReasonForVolunteering = application.ReasonForVolunteering,

                VolunteerFrequency = application.VolunteerFrequency,
                OtherFrequency = application.OtherFrequency,

                MonMorning = application.MonMorning,
                MonAfternoon = application.MonAfternoon,
                TueMorning = application.TueMorning,
                TueAfternoon = application.TueAfternoon,
                WedMorning = application.WedMorning,
                WedAfternoon = application.WedAfternoon,
                ThuMorning = application.ThuMorning,
                ThuAfternoon = application.ThuAfternoon,
                FriMorning = application.FriMorning,
                FriAfternoon = application.FriAfternoon,
                SatMorning = application.SatMorning,
                SatAfternoon = application.SatAfternoon,
                SunMorning = application.SunMorning,
                SunAfternoon = application.SunAfternoon,

                ApplicationStatus = normalizedStatus,
                SubmittedAt = application.SubmittedDate,
                ReviewedAt = application.ReviewedAt,
                ReviewNotes = application.ReviewNotes
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? reviewNotes)
        {
            var application = await _context.VolunteerApplications
                .FirstOrDefaultAsync(foundApplication => foundApplication.VolunteerApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            application.ApplicationStatus = ApplicationStatuses.Approved;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            application.ReviewNotes = string.IsNullOrWhiteSpace(reviewNotes) ? null : reviewNotes.Trim();

            // Flip IsVolunteer on the matching student application (same UserId)
            var userApplication = await _context.UserApplications
                .Where(studentApplication => studentApplication.UserId == application.UserId)
                .OrderByDescending(studentApplication => studentApplication.RegistrationDate)
                .ThenByDescending(studentApplication => studentApplication.ApplicationId)
                .FirstOrDefaultAsync();

            if (userApplication != null)
            {
                userApplication.IsVolunteer = true;
            }

            // Full role switch: gain Volunteers, lose Students
            var user = await _userManager.FindByIdAsync(application.UserId);
            if (user != null)
            {
                if (!await _userManager.IsInRoleAsync(user, "Volunteers"))
                {
                    await _userManager.AddToRoleAsync(user, "Volunteers");
                }

                if (await _userManager.IsInRoleAsync(user, "Students"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Students");
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reviewNotes)
        {
            var application = await _context.VolunteerApplications
                .FirstOrDefaultAsync(foundApplication => foundApplication.VolunteerApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            application.ApplicationStatus = ApplicationStatuses.Rejected;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            application.ReviewNotes = string.IsNullOrWhiteSpace(reviewNotes) ? null : reviewNotes.Trim();

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Students")]
        public IActionResult ApplyVolunteer()
        {
            var accessResult = EnsureApprovedStudentAccess();
            if (accessResult != null)
            {
                return accessResult;
            }

            var model = new VolunteerApplicationViewModel();
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Students")]
        public IActionResult VolunteerApplicationForm(VolunteerApplicationViewModel model)
        {

            var accessResult = EnsureApprovedStudentAccess();
            if (accessResult != null)
            {
                return accessResult;
            }
            // Re-show the form if validation fails
            if (!ModelState.IsValid)
            {
                return View("ApplyVolunteer", model);
            }

            // Map ViewModel → Entity
            var application = new VolunteerApplication
            {
                UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value,

                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNum = model.PhoneNum,
                Email = model.Email,
                Year = model.Year,

                HasVolunteeredBefore = model.HasVolunteeredBefore,
                PreviousCapacity = model.PreviousCapacity,
                ReasonForVolunteering = model.ReasonForVolunteering,

                VolunteerFrequency = model.VolunteerFrequency,
                OtherFrequency = model.OtherFrequency,

                MonMorning = model.MonMorning,
                MonAfternoon = model.MonAfternoon,
                TueMorning = model.TueMorning,
                TueAfternoon = model.TueAfternoon,
                WedMorning = model.WedMorning,
                WedAfternoon = model.WedAfternoon,
                ThuMorning = model.ThuMorning,
                ThuAfternoon = model.ThuAfternoon,
                FriMorning = model.FriMorning,
                FriAfternoon = model.FriAfternoon,
                SatMorning = model.SatMorning,
                SatAfternoon = model.SatAfternoon,
                SunMorning = model.SunMorning,
                SunAfternoon = model.SunAfternoon
            };

            // Save to database
            _context.VolunteerApplications.Add(application);
            _context.SaveChanges();

            return RedirectToAction("Status");
        }

        [Authorize(Roles = "Students,Volunteers")]
        public IActionResult Status()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var application = _context.VolunteerApplications
                .Where(volunteerApplication => volunteerApplication.UserId == userId)
                .OrderByDescending(volunteerApplication => volunteerApplication.SubmittedDate)
                .ThenByDescending(volunteerApplication => volunteerApplication.VolunteerApplicationId)
                .FirstOrDefault();

            if (application == null)
            {
                return RedirectToAction(nameof(ApplyVolunteer));
            }

            var normalizedStatus = string.IsNullOrWhiteSpace(application.ApplicationStatus)
                ? ApplicationStatuses.Pending
                : application.ApplicationStatus;

            var model = new VolunteerApplicationStatusViewModel
            {
                VolunteerApplicationId = application.VolunteerApplicationId,
                FullName = BuildFullName(application.FirstName, application.LastName),
                Year = application.Year ?? string.Empty,
                ApplicationStatus = normalizedStatus,
                SubmittedAt = application.SubmittedDate,
                ReviewedAt = application.ReviewedAt,
                ReviewNotes = application.ReviewNotes,
                CanEditApplication = normalizedStatus == ApplicationStatuses.Rejected
            };

            return View(model);
        }

        [Authorize(Roles = "Volunteers")]
        public async Task<IActionResult> Dashboard()
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Include(order => order.OrderItems)
                .Include(order => order.OrderFulfilment)
                .Where(order => string.IsNullOrWhiteSpace(order.OrderSource) || order.OrderSource != Order.SourceKiosk)
                .OrderByDescending(order => order.OrderDate)
                .ToListAsync();

            var pendingOrders = orders
                .Where(order =>
                    string.Equals(
                        OrderFulfilment.NormalizeStatus(order.OrderFulfilment?.OrderStatus),
                        OrderFulfilment.StatusOrderPlaced,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            var model = new VolunteerDashboardViewModel
            {
                NewOrders = pendingOrders.Count,
                RecentOrders = pendingOrders
                    .Take(4)
                    .Select(order => new AdminDashboardOrderViewModel
                    {
                        OrderId = order.OrderId,
                        StudentEmail = order.Email,
                        SubmittedAt = order.OrderDate,
                        Status = OrderFulfilment.NormalizeStatus(order.OrderFulfilment?.OrderStatus),
                        ItemCount = order.OrderItems.Sum(item => item.OrderQuantity),
                        PointsUsed = order.Total
                    })
                    .ToList()
            };

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var application = await _context.VolunteerApplications
                    .AsNoTracking()
                    .Where(volunteerApplication => volunteerApplication.UserId == userId &&
                                                   volunteerApplication.ApplicationStatus == ApplicationStatuses.Approved)
                    .OrderByDescending(volunteerApplication => volunteerApplication.SubmittedDate)
                    .ThenByDescending(volunteerApplication => volunteerApplication.VolunteerApplicationId)
                    .FirstOrDefaultAsync();

                if (application != null)
                {
                    model.HasSchedule = true;
                    model.MonMorning = application.MonMorning;
                    model.MonAfternoon = application.MonAfternoon;
                    model.TueMorning = application.TueMorning;
                    model.TueAfternoon = application.TueAfternoon;
                    model.WedMorning = application.WedMorning;
                    model.WedAfternoon = application.WedAfternoon;
                    model.ThuMorning = application.ThuMorning;
                    model.ThuAfternoon = application.ThuAfternoon;
                    model.FriMorning = application.FriMorning;
                    model.FriAfternoon = application.FriAfternoon;
                    model.SatMorning = application.SatMorning;
                    model.SatAfternoon = application.SatAfternoon;
                    model.SunMorning = application.SunMorning;
                    model.SunAfternoon = application.SunAfternoon;
                }
            }

            return View(model);
        }
        private IActionResult? EnsureApprovedStudentAccess()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var status = _context.UserApplications
                .AsNoTracking()
                .Where(application => application.UserId == userId)
                .OrderByDescending(application => application.RegistrationDate)
                .ThenByDescending(application => application.ApplicationId)
                .Select(application => application.ApplicationStatus)
                .FirstOrDefault();

            if (status == ApplicationStatuses.Approved)
            {
                return null;
            }

            TempData["ApplicationAccessMessage"] = "Your student application still needs approval before volunteer access is unlocked.";
            return RedirectToAction("Status", "Student");
        }
        private static string BuildFullName(string firstName, string lastName)
        {
            return string.Join(" ", new[] { firstName, lastName }
                .Where(namePart => !string.IsNullOrWhiteSpace(namePart)));
        }
    }
}
