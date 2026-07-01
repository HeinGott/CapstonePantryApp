using ClosedXML.Excel;
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

            //Find or create a VolunteerSchedule for this user and update it with the application data
            var schedule = await _context.VolunteerSchedules
                .FirstOrDefaultAsync(existingSchedule => existingSchedule.UserId == application.UserId);

            if (schedule == null)
            {
                schedule = new VolunteerSchedule
                {
                    UserId = application.UserId
                };
                _context.VolunteerSchedules.Add(schedule);
            }

            schedule.FirstName = application.FirstName;
            schedule.LastName = application.LastName;
            schedule.MonMorning = application.MonMorning;
            schedule.MonAfternoon = application.MonAfternoon;
            schedule.TueMorning = application.TueMorning;
            schedule.TueAfternoon = application.TueAfternoon;
            schedule.WedMorning = application.WedMorning;
            schedule.WedAfternoon = application.WedAfternoon;
            schedule.ThuMorning = application.ThuMorning;
            schedule.ThuAfternoon = application.ThuAfternoon;
            schedule.FriMorning = application.FriMorning;
            schedule.FriAfternoon = application.FriAfternoon;
            schedule.SatMorning = application.SatMorning;
            schedule.SatAfternoon = application.SatAfternoon;
            schedule.SunMorning = application.SunMorning;
            schedule.SunAfternoon = application.SunAfternoon;
            schedule.LastUpdated = DateTime.UtcNow;

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

        [HttpGet]
        [Authorize(Roles = "Students")]
        public IActionResult ApplyVolunteer()
        {
            var accessResult = EnsureApprovedStudentAccess();
            if (accessResult != null)
            {
                return accessResult;
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var latestApplication = _context.VolunteerApplications
                .AsNoTracking()
                .Where(application => application.UserId == userId)
                .OrderByDescending(application => application.SubmittedDate)
                .ThenByDescending(application => application.VolunteerApplicationId)
                .FirstOrDefault();

            if (latestApplication != null)
            {
                var normalizedStatus = string.IsNullOrWhiteSpace(latestApplication.ApplicationStatus)
                    ? ApplicationStatuses.Pending
                    : latestApplication.ApplicationStatus;

                if (normalizedStatus != ApplicationStatuses.Rejected)
                {
                    return RedirectToAction(nameof(Status));
                }
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

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var existingApplication = _context.VolunteerApplications
                .AsNoTracking()
                .Where(application => application.UserId == userId)
                .OrderByDescending(application => application.SubmittedDate)
                .ThenByDescending(application => application.VolunteerApplicationId)
                .FirstOrDefault();

            if (existingApplication != null)
            {
                var normalizedStatus = string.IsNullOrWhiteSpace(existingApplication.ApplicationStatus)
                    ? ApplicationStatuses.Pending
                    : existingApplication.ApplicationStatus;

                if (normalizedStatus != ApplicationStatuses.Rejected)
                {
                    return RedirectToAction(nameof(Status));
                }
            }

            // Re-show the form if validation fails
            if (!ModelState.IsValid)
            {
                return View("ApplyVolunteer", model);
            }

            // Map ViewModel → Entity
            var application = new VolunteerApplication
            {
                UserId = userId,

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
                var schedule = await _context.VolunteerSchedules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(existingSchedule => existingSchedule.UserId == userId);

                if (schedule != null)
                {
                    model.HasSchedule = true;
                    model.MonMorning = schedule.MonMorning;
                    model.MonAfternoon = schedule.MonAfternoon;
                    model.TueMorning = schedule.TueMorning;
                    model.TueAfternoon = schedule.TueAfternoon;
                    model.WedMorning = schedule.WedMorning;
                    model.WedAfternoon = schedule.WedAfternoon;
                    model.ThuMorning = schedule.ThuMorning;
                    model.ThuAfternoon = schedule.ThuAfternoon;
                    model.FriMorning = schedule.FriMorning;
                    model.FriAfternoon = schedule.FriAfternoon;
                    model.SatMorning = schedule.SatMorning;
                    model.SatAfternoon = schedule.SatAfternoon;
                    model.SunMorning = schedule.SunMorning;
                    model.SunAfternoon = schedule.SunAfternoon;
                }
            }

            return View(model);
        }

        [Authorize(Roles = "Volunteers")]
        public async Task<IActionResult> Schedule()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var mySchedule = await _context.VolunteerSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(schedule => schedule.UserId == userId);

            var pendingRequest = await _context.ScheduleChangeRequests
                .AsNoTracking()
                .Where(request => request.UserId == userId && request.RequestStatus == ApplicationStatuses.Pending)
                .OrderByDescending(request => request.SubmittedDate)
                .FirstOrDefaultAsync();

            var roster = await _context.VolunteerSchedules
                .AsNoTracking()
                .OrderBy(schedule => schedule.FirstName)
                .ThenBy(schedule => schedule.LastName)
                .ToListAsync();

            var model = new VolunteerSchedulePageViewModel
            {
                HasSchedule = mySchedule != null,
                MonMorning = mySchedule?.MonMorning ?? false,
                MonAfternoon = mySchedule?.MonAfternoon ?? false,
                TueMorning = mySchedule?.TueMorning ?? false,
                TueAfternoon = mySchedule?.TueAfternoon ?? false,
                WedMorning = mySchedule?.WedMorning ?? false,
                WedAfternoon = mySchedule?.WedAfternoon ?? false,
                ThuMorning = mySchedule?.ThuMorning ?? false,
                ThuAfternoon = mySchedule?.ThuAfternoon ?? false,
                FriMorning = mySchedule?.FriMorning ?? false,
                FriAfternoon = mySchedule?.FriAfternoon ?? false,
                SatMorning = mySchedule?.SatMorning ?? false,
                SatAfternoon = mySchedule?.SatAfternoon ?? false,
                SunMorning = mySchedule?.SunMorning ?? false,
                SunAfternoon = mySchedule?.SunAfternoon ?? false,

                HasPendingRequest = pendingRequest != null,
                PendingRequest = pendingRequest == null ? null : new ScheduleChangeRequestSummaryViewModel
                {
                    ScheduleChangeRequestId = pendingRequest.ScheduleChangeRequestId,
                    Reason = pendingRequest.Reason,
                    RequestStatus = pendingRequest.RequestStatus,
                    SubmittedAt = pendingRequest.SubmittedDate
                },

                Roster = roster.Select(schedule => new VolunteerScheduleRosterRowViewModel
                {
                    FullName = BuildFullName(schedule.FirstName, schedule.LastName),
                    MonMorning = schedule.MonMorning,
                    MonAfternoon = schedule.MonAfternoon,
                    TueMorning = schedule.TueMorning,
                    TueAfternoon = schedule.TueAfternoon,
                    WedMorning = schedule.WedMorning,
                    WedAfternoon = schedule.WedAfternoon,
                    ThuMorning = schedule.ThuMorning,
                    ThuAfternoon = schedule.ThuAfternoon,
                    FriMorning = schedule.FriMorning,
                    FriAfternoon = schedule.FriAfternoon,
                    SatMorning = schedule.SatMorning,
                    SatAfternoon = schedule.SatAfternoon,
                    SunMorning = schedule.SunMorning,
                    SunAfternoon = schedule.SunAfternoon
                }).ToList()
            };

            return View(model);
        }

        [Authorize(Roles = "Volunteers")]
        public async Task<IActionResult> RequestScheduleChange()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var hasPending = await _context.ScheduleChangeRequests
                .AsNoTracking()
                .AnyAsync(request => request.UserId == userId && request.RequestStatus == ApplicationStatuses.Pending);

            if (hasPending)
            {
                TempData["ScheduleAccessMessage"] = "You already have a pending schedule change request.";
                return RedirectToAction(nameof(Schedule));
            }

            var mySchedule = await _context.VolunteerSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(schedule => schedule.UserId == userId);

            var model = new RequestScheduleChangeViewModel
            {
                MonMorning = mySchedule?.MonMorning ?? false,
                MonAfternoon = mySchedule?.MonAfternoon ?? false,
                TueMorning = mySchedule?.TueMorning ?? false,
                TueAfternoon = mySchedule?.TueAfternoon ?? false,
                WedMorning = mySchedule?.WedMorning ?? false,
                WedAfternoon = mySchedule?.WedAfternoon ?? false,
                ThuMorning = mySchedule?.ThuMorning ?? false,
                ThuAfternoon = mySchedule?.ThuAfternoon ?? false,
                FriMorning = mySchedule?.FriMorning ?? false,
                FriAfternoon = mySchedule?.FriAfternoon ?? false,
                SatMorning = mySchedule?.SatMorning ?? false,
                SatAfternoon = mySchedule?.SatAfternoon ?? false,
                SunMorning = mySchedule?.SunMorning ?? false,
                SunAfternoon = mySchedule?.SunAfternoon ?? false
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Volunteers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitScheduleChangeRequest(RequestScheduleChangeViewModel model)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                return View("RequestScheduleChange", model);
            }

            // Re-check
            var hasPending = await _context.ScheduleChangeRequests
                .AsNoTracking()
                .AnyAsync(request => request.UserId == userId && request.RequestStatus == ApplicationStatuses.Pending);

            if (hasPending)
            {
                TempData["ScheduleAccessMessage"] = "You already have a pending schedule change request.";
                return RedirectToAction(nameof(Schedule));
            }

            var currentSchedule = await _context.VolunteerSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(schedule => schedule.UserId == userId);

            var request = new ScheduleChangeRequest
            {
                UserId = userId,
                FirstName = currentSchedule?.FirstName ?? string.Empty,
                LastName = currentSchedule?.LastName ?? string.Empty,

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
                SunAfternoon = model.SunAfternoon,

                Reason = model.Reason.Trim()
            };

            _context.ScheduleChangeRequests.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Schedule));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ScheduleRequests()
        {
            var requests = await _context.ScheduleChangeRequests
                .AsNoTracking()
                .OrderByDescending(request => request.SubmittedDate)
                .ToListAsync();

            var summaries = requests
                .Select(request => new ScheduleRequestSummaryViewModel
                {
                    ScheduleChangeRequestId = request.ScheduleChangeRequestId,
                    FullName = BuildFullName(request.FirstName, request.LastName),
                    Reason = request.Reason,
                    RequestStatus = request.RequestStatus,
                    SubmittedAt = request.SubmittedDate
                })
                .ToList();

            var model = new ScheduleRequestAdminIndexViewModel
            {
                PendingRequests = summaries
                    .Where(summary => summary.RequestStatus == ApplicationStatuses.Pending)
                    .OrderBy(summary => summary.SubmittedAt)
                    .ToList(),
                ReviewedRequests = summaries
                    .Where(summary => summary.RequestStatus != ApplicationStatuses.Pending)
                    .OrderByDescending(summary => summary.SubmittedAt)
                    .ToList()
            };

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ScheduleRequestDetails(int id)
        {
            var request = await _context.ScheduleChangeRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(foundRequest => foundRequest.ScheduleChangeRequestId == id);

            if (request == null)
            {
                return NotFound();
            }

            var currentSchedule = await _context.VolunteerSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(schedule => schedule.UserId == request.UserId);

            var model = new ScheduleRequestDetailsViewModel
            {
                ScheduleChangeRequestId = request.ScheduleChangeRequestId,
                FullName = BuildFullName(request.FirstName, request.LastName),
                Reason = request.Reason,
                RequestStatus = request.RequestStatus,
                SubmittedAt = request.SubmittedDate,
                ReviewedAt = request.ReviewedAt,
                ReviewNotes = request.ReviewNotes,

                HasCurrentSchedule = currentSchedule != null,
                CurrentMonMorning = currentSchedule?.MonMorning ?? false,
                CurrentMonAfternoon = currentSchedule?.MonAfternoon ?? false,
                CurrentTueMorning = currentSchedule?.TueMorning ?? false,
                CurrentTueAfternoon = currentSchedule?.TueAfternoon ?? false,
                CurrentWedMorning = currentSchedule?.WedMorning ?? false,
                CurrentWedAfternoon = currentSchedule?.WedAfternoon ?? false,
                CurrentThuMorning = currentSchedule?.ThuMorning ?? false,
                CurrentThuAfternoon = currentSchedule?.ThuAfternoon ?? false,
                CurrentFriMorning = currentSchedule?.FriMorning ?? false,
                CurrentFriAfternoon = currentSchedule?.FriAfternoon ?? false,
                CurrentSatMorning = currentSchedule?.SatMorning ?? false,
                CurrentSatAfternoon = currentSchedule?.SatAfternoon ?? false,
                CurrentSunMorning = currentSchedule?.SunMorning ?? false,
                CurrentSunAfternoon = currentSchedule?.SunAfternoon ?? false,

                MonMorning = request.MonMorning,
                MonAfternoon = request.MonAfternoon,
                TueMorning = request.TueMorning,
                TueAfternoon = request.TueAfternoon,
                WedMorning = request.WedMorning,
                WedAfternoon = request.WedAfternoon,
                ThuMorning = request.ThuMorning,
                ThuAfternoon = request.ThuAfternoon,
                FriMorning = request.FriMorning,
                FriAfternoon = request.FriAfternoon,
                SatMorning = request.SatMorning,
                SatAfternoon = request.SatAfternoon,
                SunMorning = request.SunMorning,
                SunAfternoon = request.SunAfternoon
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveScheduleRequest(int id, string? reviewNotes)
        {
            var request = await _context.ScheduleChangeRequests
                .FirstOrDefaultAsync(foundRequest => foundRequest.ScheduleChangeRequestId == id);

            if (request == null)
            {
                return NotFound();
            }

            request.RequestStatus = ApplicationStatuses.Approved;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            request.ReviewNotes = string.IsNullOrWhiteSpace(reviewNotes) ? null : reviewNotes.Trim();

            // Copy the requested schedule onto the volunteer's real schedule row
            var schedule = await _context.VolunteerSchedules
                .FirstOrDefaultAsync(existingSchedule => existingSchedule.UserId == request.UserId);

            if (schedule == null)
            {
                schedule = new VolunteerSchedule
                {
                    UserId = request.UserId,
                    FirstName = request.FirstName,
                    LastName = request.LastName
                };
                _context.VolunteerSchedules.Add(schedule);
            }

            schedule.MonMorning = request.MonMorning;
            schedule.MonAfternoon = request.MonAfternoon;
            schedule.TueMorning = request.TueMorning;
            schedule.TueAfternoon = request.TueAfternoon;
            schedule.WedMorning = request.WedMorning;
            schedule.WedAfternoon = request.WedAfternoon;
            schedule.ThuMorning = request.ThuMorning;
            schedule.ThuAfternoon = request.ThuAfternoon;
            schedule.FriMorning = request.FriMorning;
            schedule.FriAfternoon = request.FriAfternoon;
            schedule.SatMorning = request.SatMorning;
            schedule.SatAfternoon = request.SatAfternoon;
            schedule.SunMorning = request.SunMorning;
            schedule.SunAfternoon = request.SunAfternoon;
            schedule.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ScheduleRequestDetails), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectScheduleRequest(int id, string? reviewNotes)
        {
            var request = await _context.ScheduleChangeRequests
                .FirstOrDefaultAsync(foundRequest => foundRequest.ScheduleChangeRequestId == id);

            if (request == null)
            {
                return NotFound();
            }

            request.RequestStatus = ApplicationStatuses.Rejected;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            request.ReviewNotes = string.IsNullOrWhiteSpace(reviewNotes) ? null : reviewNotes.Trim();

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ScheduleRequestDetails), new { id });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportSchedule()
        {
            var roster = await _context.VolunteerSchedules
                .AsNoTracking()
                .OrderBy(schedule => schedule.FirstName)
                .ThenBy(schedule => schedule.LastName)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Volunteer Schedule");

            string[] headers =
            {
                "Name",
                "Mon AM", "Mon PM",
                "Tue AM", "Tue PM",
                "Wed AM", "Wed PM",
                "Thu AM", "Thu PM",
                "Fri AM", "Fri PM",
                "Sat AM", "Sat PM",
                "Sun AM", "Sun PM"
            };

            for (var column = 0; column < headers.Length; column++)
            {
                worksheet.Cell(1, column + 1).Value = headers[column];
            }

            var headerRange = worksheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#102f61");
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var row = 2;
            foreach (var schedule in roster)
            {
                worksheet.Cell(row, 1).Value = BuildFullName(schedule.FirstName, schedule.LastName);

                bool[] slots =
                {
                    schedule.MonMorning, schedule.MonAfternoon,
                    schedule.TueMorning, schedule.TueAfternoon,
                    schedule.WedMorning, schedule.WedAfternoon,
                    schedule.ThuMorning, schedule.ThuAfternoon,
                    schedule.FriMorning, schedule.FriAfternoon,
                    schedule.SatMorning, schedule.SatAfternoon,
                    schedule.SunMorning, schedule.SunAfternoon
                };

                for (var slotIndex = 0; slotIndex < slots.Length; slotIndex++)
                {
                    var cell = worksheet.Cell(row, slotIndex + 2);
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    if (slots[slotIndex])
                    {
                        cell.Value = "✓";
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontColor = XLColor.White;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#102f61");
                    }
                    else
                    {
                        cell.Value = "";
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f9fafb");
                    }
                }

                row++;
            }

            worksheet.SheetView.FreezeRows(1);
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"VolunteerSchedule_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
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
