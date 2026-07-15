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
                    UserId = application.UserId,
                    FullName = BuildFullName(application.FirstName, application.LastName),
                    Email = application.Email ?? string.Empty,
                    
                    ApplicationStatus = string.IsNullOrWhiteSpace(application.ApplicationStatus)
                        ? ApplicationStatuses.Pending
                        : application.ApplicationStatus,
                    SubmittedAt = application.SubmittedDate
                })
                .OrderBy(summary => summary.FullName)
                .ToList();

            var weekStart = StartOfWeek(DateTime.Today);
            var weekEnd = weekStart.AddDays(7);
            var upcomingShifts = await _context.VolunteerShifts
                .AsNoTracking()
                .Where(shift =>
                    shift.Status != VolunteerShiftStatuses.Cancelled &&
                    shift.ShiftDate >= DateTime.Today &&
                    shift.ShiftDate < weekEnd)
                .OrderBy(shift => shift.ShiftDate)
                .ThenBy(shift => shift.StartTime)
                .ToListAsync();

            var latestApplicationByUserId = latestApplications.ToDictionary(application => application.UserId, application => application);
            var pendingScheduleRequests = await _context.ScheduleChangeRequests
                .AsNoTracking()
                .CountAsync(request => request.RequestStatus == ApplicationStatuses.Pending);

            var model = new VolunteerAdminIndexViewModel
            {
                TotalVolunteers = summaries.Count(summary => summary.ApplicationStatus == ApplicationStatuses.Approved),
                PendingScheduleRequests = pendingScheduleRequests,
                UpcomingShiftsThisWeek = upcomingShifts.Count,
                TodaysShifts = upcomingShifts.Count(shift => shift.ShiftDate.Date == DateTime.Today),
                PendingApplications = summaries
                    .Where(summary => summary.ApplicationStatus == ApplicationStatuses.Pending)
                    .OrderBy(summary => summary.SubmittedAt)
                    .ToList(),
                ApprovedVolunteers = summaries
                    .Where(summary => summary.ApplicationStatus == ApplicationStatuses.Approved)
                    .OrderBy(summary => summary.FullName)
                    .ToList(),
                UpcomingShifts = upcomingShifts
                    .Take(8)
                    .Select(shift =>
                    {
                        latestApplicationByUserId.TryGetValue(shift.UserId, out var latestApplication);
                        return BuildShiftSummaryViewModel(shift, latestApplication);
                    })
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

            var currentAvailability = await _context.VolunteerSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(schedule => schedule.UserId == application.UserId);

            var upcomingShifts = await _context.VolunteerShifts
                .AsNoTracking()
                .Where(shift =>
                    shift.UserId == application.UserId &&
                    shift.Status != VolunteerShiftStatuses.Cancelled &&
                    shift.ShiftDate >= DateTime.Today)
                .OrderBy(shift => shift.ShiftDate)
                .ThenBy(shift => shift.StartTime)
                .Take(10)
                .ToListAsync();

            var model = new VolunteerDetailsViewModel
            {
                VolunteerApplicationId = application.VolunteerApplicationId,
                UserId = application.UserId,
                FullName = BuildFullName(application.FirstName, application.LastName),
                FirstName = application.FirstName,
                LastName = application.LastName,
                Email = application.Email ?? string.Empty,
                PhoneNumber = application.PhoneNum,
                
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
                ReviewNotes = application.ReviewNotes,
                AvailabilityLastUpdated = currentAvailability?.LastUpdated,
                UpcomingShifts = upcomingShifts
                    .Select(shift => BuildShiftSummaryViewModel(shift, application))
                    .ToList()
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Scheduler(DateTime? weekStart, string? userId, DateTime? shiftDate, int? editShiftId)
        {
            var resolvedWeekStart = StartOfWeek(weekStart?.Date ?? shiftDate?.Date ?? DateTime.Today);
            var model = await BuildSchedulerModelAsync(resolvedWeekStart, userId, shiftDate, editShiftId);
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveShift(VolunteerShiftFormViewModel model)
        {
            var resolvedWeekStart = StartOfWeek(model.WeekStart == default ? model.ShiftDate : model.WeekStart);
            var isAjaxRequest = IsAjaxRequest();

            if (!TryParseShiftFormTimes(model, out var startTime, out var endTime))
            {
                ModelState.AddModelError(string.Empty, "Please enter a valid start and end time.");
            }
            else if (endTime <= startTime)
            {
                ModelState.AddModelError(string.Empty, "End time must be after the start time.");
            }

            var latestApplications = await GetLatestVolunteerApplicationsAsync();
            var latestApplication = latestApplications.FirstOrDefault(application =>
                application.UserId == model.UserId &&
                NormalizeApplicationStatus(application.ApplicationStatus) == ApplicationStatuses.Approved);

            if (latestApplication == null)
            {
                ModelState.AddModelError(nameof(model.UserId), "Please choose an approved volunteer.");
            }

            if (!ModelState.IsValid)
            {
                if (isAjaxRequest)
                {
                    return BadRequest(new { message = "Please enter a valid shift." });
                }

                var invalidModel = await BuildSchedulerModelAsync(resolvedWeekStart, model.UserId, model.ShiftDate, model.VolunteerShiftId == 0 ? null : model.VolunteerShiftId, model);
                return View("Scheduler", invalidModel);
            }

            var overlappingShiftExists = await _context.VolunteerShifts
                .AsNoTracking()
                .AnyAsync(shift =>
                    shift.VolunteerShiftId != model.VolunteerShiftId &&
                    shift.UserId == model.UserId &&
                    shift.Status != VolunteerShiftStatuses.Cancelled &&
                    shift.ShiftDate == model.ShiftDate.Date &&
                    shift.StartTime < endTime &&
                    startTime < shift.EndTime);

            if (overlappingShiftExists)
            {
                if (isAjaxRequest)
                {
                    return BadRequest(new { message = "That volunteer already has an overlapping shift on this date." });
                }

                ModelState.AddModelError(string.Empty, "That volunteer already has an overlapping shift on this date.");
                var overlappingModel = await BuildSchedulerModelAsync(resolvedWeekStart, model.UserId, model.ShiftDate, model.VolunteerShiftId == 0 ? null : model.VolunteerShiftId, model);
                return View("Scheduler", overlappingModel);
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var shift = model.VolunteerShiftId == 0
                ? null
                : await _context.VolunteerShifts.FirstOrDefaultAsync(existingShift => existingShift.VolunteerShiftId == model.VolunteerShiftId);

            if (shift == null)
            {
                shift = new VolunteerShift
                {
                    UserId = model.UserId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = currentUserId
                };
                _context.VolunteerShifts.Add(shift);
            }

            shift.UserId = model.UserId;
            shift.ShiftDate = model.ShiftDate.Date;
            shift.StartTime = startTime;
            shift.EndTime = endTime;
            shift.Status = VolunteerShiftStatuses.Scheduled;
            shift.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
            shift.UpdatedAt = DateTime.UtcNow;
            shift.UpdatedByUserId = currentUserId;

            await _context.SaveChangesAsync();

            if (isAjaxRequest)
            {
                var availability = await _context.VolunteerSchedules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(schedule => schedule.UserId == shift.UserId);

                var responseModel = BuildSchedulerShiftResponse(
                    shift,
                    latestApplication,
                    availability);

                return Json(responseModel);
            }

            return RedirectToAction(nameof(Scheduler), new { weekStart = resolvedWeekStart.ToString("yyyy-MM-dd") });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteShift(int id, DateTime weekStart)
        {
            var shift = await _context.VolunteerShifts
                .FirstOrDefaultAsync(existingShift => existingShift.VolunteerShiftId == id);

            if (shift != null)
            {
                _context.VolunteerShifts.Remove(shift);
                await _context.SaveChangesAsync();
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = true, id });
            }

            return RedirectToAction(nameof(Scheduler), new { weekStart = StartOfWeek(weekStart).ToString("yyyy-MM-dd") });
        }

        [HttpGet]
        [Authorize(Roles = "Students")]
        public async Task<IActionResult> ApplyVolunteer()
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
            var studentApplication = _context.UserApplications
            .AsNoTracking()
            .Where(application => application.UserId == userId)
            .OrderByDescending(application => application.RegistrationDate)
            .ThenByDescending(application => application.ApplicationId)
            .FirstOrDefault();

            if (studentApplication != null)
            {
                model.FirstName = studentApplication.FirstName;
                model.LastName = studentApplication.LastName;
                model.PhoneNum = studentApplication.PhoneNum;               
            }

            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                model.Email = user.Email;
            }

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

            var latestApplication = await _context.VolunteerApplications
                .AsNoTracking()
                .Where(application => application.UserId == userId)
                .OrderByDescending(application => application.SubmittedDate)
                .ThenByDescending(application => application.VolunteerApplicationId)
                .FirstOrDefaultAsync();

            var upcomingShifts = await _context.VolunteerShifts
                .AsNoTracking()
                .Where(shift =>
                    shift.UserId == userId &&
                    shift.Status != VolunteerShiftStatuses.Cancelled &&
                    shift.ShiftDate >= DateTime.Today)
                .OrderBy(shift => shift.ShiftDate)
                .ThenBy(shift => shift.StartTime)
                .Take(8)
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
                UpcomingShifts = upcomingShifts
                    .Select(shift => new VolunteerAssignedShiftViewModel
                    {
                        VolunteerShiftId = shift.VolunteerShiftId,
                        ShiftDate = shift.ShiftDate,
                        TimeLabel = FormatShiftTimeLabel(shift.StartTime, shift.EndTime),
                        Status = shift.Status,
                        Notes = shift.Notes,
                        OutsideAvailability = !IsShiftWithinAvailability(shift, mySchedule, latestApplication)
                    })
                    .ToList(),

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

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteApplication(int id)
        {
            var application = await _context.VolunteerApplications
                .FirstOrDefaultAsync(foundApplication => foundApplication.VolunteerApplicationId == id);

            if (application == null)
            {
                TempData["StatusMessage"] = "That volunteer application no longer exists.";
                TempData["StatusType"] = "error";
                return RedirectToAction(nameof(Index));
            }

            var userId = application.UserId;

            _context.VolunteerApplications.Remove(application);

            var remainingApplications = await _context.VolunteerApplications
                .AsNoTracking()
                .Where(existingApplication =>
                    existingApplication.UserId == userId &&
                    existingApplication.VolunteerApplicationId != id)
                .ToListAsync();

            if (!remainingApplications.Any())
            {
                var volunteerSchedule = await _context.VolunteerSchedules
                    .FirstOrDefaultAsync(schedule => schedule.UserId == userId);

                if (volunteerSchedule != null)
                {
                    _context.VolunteerSchedules.Remove(volunteerSchedule);
                }

                var scheduleChangeRequests = await _context.ScheduleChangeRequests
                    .Where(request => request.UserId == userId)
                    .ToListAsync();

                if (scheduleChangeRequests.Any())
                {
                    _context.ScheduleChangeRequests.RemoveRange(scheduleChangeRequests);
                }

                var shifts = await _context.VolunteerShifts
                    .Where(shift => shift.UserId == userId)
                    .ToListAsync();

                if (shifts.Any())
                {
                    _context.VolunteerShifts.RemoveRange(shifts);
                }
            }

            var hasApprovedApplications = remainingApplications.Any(existingApplication =>
                NormalizeApplicationStatus(existingApplication.ApplicationStatus) == ApplicationStatuses.Approved);

            if (!hasApprovedApplications)
            {
                var userApplication = await _context.UserApplications
                    .Where(studentApplication => studentApplication.UserId == userId)
                    .OrderByDescending(studentApplication => studentApplication.RegistrationDate)
                    .ThenByDescending(studentApplication => studentApplication.ApplicationId)
                    .FirstOrDefaultAsync();

                if (userApplication != null)
                {
                    userApplication.IsVolunteer = false;
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && await _userManager.IsInRoleAsync(user, "Volunteers"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Volunteers");
                }
            }

            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Volunteer application deleted.";
            TempData["StatusType"] = "success";

            return RedirectToAction(nameof(Index));
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

        private async Task<VolunteerSchedulerPageViewModel> BuildSchedulerModelAsync(
            DateTime weekStart,
            string? userId,
            DateTime? shiftDate,
            int? editShiftId,
            VolunteerShiftFormViewModel? formOverride = null)
        {
            var resolvedWeekStart = StartOfWeek(weekStart);
            var resolvedShiftDate = shiftDate?.Date ?? resolvedWeekStart;
            var weekEnd = resolvedWeekStart.AddDays(5);

            var latestApplications = await GetLatestVolunteerApplicationsAsync();
            var approvedApplications = latestApplications
                .Where(application => NormalizeApplicationStatus(application.ApplicationStatus) == ApplicationStatuses.Approved)
                .OrderBy(application => BuildFullName(application.FirstName, application.LastName))
                .ToList();

            var approvedUserIds = approvedApplications.Select(application => application.UserId).Distinct().ToList();
            var schedules = await _context.VolunteerSchedules
                .AsNoTracking()
                .Where(schedule => approvedUserIds.Contains(schedule.UserId))
                .ToListAsync();

            var shifts = await _context.VolunteerShifts
                .AsNoTracking()
                .Where(shift =>
                    shift.Status != VolunteerShiftStatuses.Cancelled &&
                    shift.ShiftDate >= resolvedWeekStart &&
                    shift.ShiftDate < weekEnd)
                .OrderBy(shift => shift.ShiftDate)
                .ThenBy(shift => shift.StartTime)
                .ToListAsync();

            var nextShiftByUserId = shifts
                .GroupBy(shift => shift.UserId)
                .ToDictionary(group => group.Key, group => group.First());

            var selectedShift = editShiftId == null
                ? null
                : await _context.VolunteerShifts.AsNoTracking()
                    .FirstOrDefaultAsync(shift => shift.VolunteerShiftId == editShiftId.Value);

            var selectedUserId = formOverride?.UserId
                ?? selectedShift?.UserId
                ?? userId
                ?? approvedApplications.FirstOrDefault()?.UserId
                ?? string.Empty;

            var selectedDate = formOverride?.ShiftDate.Date
                ?? selectedShift?.ShiftDate.Date
                ?? resolvedShiftDate;

            var shiftForm = formOverride ?? new VolunteerShiftFormViewModel
            {
                VolunteerShiftId = selectedShift?.VolunteerShiftId ?? 0,
                UserId = selectedUserId,
                ShiftDate = selectedDate,
                StartTime = selectedShift == null ? "09:00" : selectedShift.StartTime.ToString(@"hh\:mm"),
                EndTime = selectedShift == null ? "11:00" : selectedShift.EndTime.ToString(@"hh\:mm"),
                Notes = selectedShift?.Notes,
                WeekStart = resolvedWeekStart
            };

            var applicationByUserId = approvedApplications.ToDictionary(application => application.UserId, application => application);
            var scheduleByUserId = schedules.ToDictionary(schedule => schedule.UserId, schedule => schedule);

            return new VolunteerSchedulerPageViewModel
            {
                WeekStart = resolvedWeekStart,
                WeekEnd = resolvedWeekStart.AddDays(4),
                IsEditingShift = selectedShift != null || shiftForm.VolunteerShiftId != 0,
                ShiftForm = shiftForm,
                Volunteers = approvedApplications.Select(application =>
                {
                    scheduleByUserId.TryGetValue(application.UserId, out var availability);
                    nextShiftByUserId.TryGetValue(application.UserId, out var nextShift);

                    return new VolunteerSchedulerVolunteerViewModel
                    {
                        VolunteerApplicationId = application.VolunteerApplicationId,
                        UserId = application.UserId,
                        FullName = BuildFullName(application.FirstName, application.LastName),
                        Email = application.Email ?? string.Empty,
                        AvailabilitySummary = availability != null
                            ? BuildAvailabilitySummary(availability)
                            : BuildAvailabilitySummary(application),
                        AvailabilityRows = BuildAvailabilityRows(availability, application),
                        HasAvailabilityOnSelectedDay = HasAvailabilityOnDate(application, availability, shiftForm.ShiftDate),
                        NextShiftDate = nextShift?.ShiftDate,
                        NextShiftTimeLabel = nextShift == null ? null : FormatShiftTimeLabel(nextShift.StartTime, nextShift.EndTime)
                    };
                }).ToList(),
                Days = Enumerable.Range(0, 5)
                    .Select(offset =>
                    {
                        var date = resolvedWeekStart.AddDays(offset);
                        return new VolunteerSchedulerDayViewModel
                        {
                            Date = date,
                            Label = date.ToString("ddd, MMM d"),
                            Shifts = shifts
                                .Where(shift => shift.ShiftDate.Date == date.Date)
                                .Select(shift =>
                                {
                                    applicationByUserId.TryGetValue(shift.UserId, out var application);
                                    scheduleByUserId.TryGetValue(shift.UserId, out var availability);

                                    return new VolunteerSchedulerShiftCardViewModel
                                    {
                                        VolunteerShiftId = shift.VolunteerShiftId,
                                        VolunteerApplicationId = application?.VolunteerApplicationId ?? 0,
                                        UserId = shift.UserId,
                                        FullName = application == null
                                            ? "Volunteer"
                                            : BuildFullName(application.FirstName, application.LastName),
                                        StartTime = shift.StartTime.ToString(@"hh\:mm"),
                                        EndTime = shift.EndTime.ToString(@"hh\:mm"),
                                        TimeLabel = FormatShiftTimeLabel(shift.StartTime, shift.EndTime),
                                        Status = shift.Status,
                                        OutsideAvailability = !IsShiftWithinAvailability(shift, availability, application),
                                        Notes = shift.Notes,
                                        StartMinutes = Math.Max(0, (int)(shift.StartTime - TimeSpan.FromHours(7)).TotalMinutes),
                                        DurationMinutes = Math.Max(30, (int)(shift.EndTime - shift.StartTime).TotalMinutes)
                                    };
                                })
                                .ToList()
                        };
                    })
                    .ToList()
            };
        }

        private async Task<List<VolunteerApplication>> GetLatestVolunteerApplicationsAsync()
        {
            var applications = await _context.VolunteerApplications
                .AsNoTracking()
                .OrderByDescending(application => application.SubmittedDate)
                .ThenByDescending(application => application.VolunteerApplicationId)
                .ToListAsync();

            return applications
                .GroupBy(application => application.UserId)
                .Select(group => group.First())
                .ToList();
        }

        private static VolunteerShiftSummaryViewModel BuildShiftSummaryViewModel(
            VolunteerShift shift,
            VolunteerApplication? application)
        {
            return new VolunteerShiftSummaryViewModel
            {
                VolunteerShiftId = shift.VolunteerShiftId,
                VolunteerApplicationId = application?.VolunteerApplicationId ?? 0,
                UserId = shift.UserId,
                FullName = application == null
                    ? "Volunteer"
                    : BuildFullName(application.FirstName, application.LastName),
                ShiftDate = shift.ShiftDate,
                TimeLabel = FormatShiftTimeLabel(shift.StartTime, shift.EndTime),
                Status = shift.Status,
                Notes = shift.Notes
            };
        }

        private static VolunteerSchedulerShiftResponseViewModel BuildSchedulerShiftResponse(
            VolunteerShift shift,
            VolunteerApplication? application,
            VolunteerSchedule? availability)
        {
            return new VolunteerSchedulerShiftResponseViewModel
            {
                VolunteerShiftId = shift.VolunteerShiftId,
                UserId = shift.UserId,
                FullName = application == null
                    ? "Volunteer"
                    : BuildFullName(application.FirstName, application.LastName),
                ShiftDate = shift.ShiftDate.ToString("yyyy-MM-dd"),
                StartTime = shift.StartTime.ToString(@"hh\:mm"),
                EndTime = shift.EndTime.ToString(@"hh\:mm"),
                TimeLabel = FormatShiftTimeLabel(shift.StartTime, shift.EndTime),
                Notes = shift.Notes,
                OutsideAvailability = !IsShiftWithinAvailability(shift, availability, application),
                StartMinutes = Math.Max(0, (int)(shift.StartTime - TimeSpan.FromHours(7)).TotalMinutes),
                DurationMinutes = Math.Max(30, (int)(shift.EndTime - shift.StartTime).TotalMinutes)
            };
        }

        private static string NormalizeApplicationStatus(string? status)
        {
            return string.IsNullOrWhiteSpace(status) ? ApplicationStatuses.Pending : status;
        }

        private bool IsAjaxRequest()
        {
            return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime StartOfWeek(DateTime value)
        {
            var date = value.Date;
            var difference = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return date.AddDays(-difference);
        }

        private static bool TryParseShiftFormTimes(
            VolunteerShiftFormViewModel model,
            out TimeSpan startTime,
            out TimeSpan endTime)
        {
            startTime = default;
            endTime = default;

            return TimeSpan.TryParse(model.StartTime, out startTime)
                && TimeSpan.TryParse(model.EndTime, out endTime);
        }

        private static string FormatShiftTimeLabel(TimeSpan startTime, TimeSpan endTime)
        {
            return $"{DateTime.Today.Add(startTime):h:mm tt} - {DateTime.Today.Add(endTime):h:mm tt}";
        }

        private static string BuildAvailabilitySummary(VolunteerApplication application)
        {
            return BuildAvailabilitySummaryFromObject(application);
        }

        private static string BuildAvailabilitySummary(VolunteerSchedule? availability, VolunteerApplication? fallbackApplication = null)
        {
            return availability != null
                ? BuildAvailabilitySummaryFromObject(availability)
                : fallbackApplication == null
                    ? "No availability on file"
                    : BuildAvailabilitySummaryFromObject(fallbackApplication);
        }

        private static string BuildAvailabilitySummaryFromObject(object source)
        {
            var slots = new List<string>();

            AddAvailabilitySlot(slots, GetAvailabilityValue(source, nameof(VolunteerSchedule.MonMorning)), "Mon AM");
            AddAvailabilitySlot(slots, GetAvailabilityValue(source, nameof(VolunteerSchedule.MonAfternoon)), "Mon PM");
            AddAvailabilitySlot(slots, GetAvailabilityValue(source, nameof(VolunteerSchedule.TueMorning)), "Tue AM");
            AddAvailabilitySlot(slots, GetAvailabilityValue(source, nameof(VolunteerSchedule.TueAfternoon)), "Tue PM");
            AddAvailabilitySlot(slots, GetAvailabilityValue(source, nameof(VolunteerSchedule.WedMorning)), "Wed AM");
            AddAvailabilitySlot(slots, GetAvailabilityValue(source, nameof(VolunteerSchedule.WedAfternoon)), "Wed PM");
            AddAvailabilitySlot(slots, GetAvailabilityValue(source, nameof(VolunteerSchedule.ThuMorning)), "Thu AM");
            AddAvailabilitySlot(slots, GetAvailabilityValue(source, nameof(VolunteerSchedule.ThuAfternoon)), "Thu PM");
            AddAvailabilitySlot(slots, GetAvailabilityValue(source, nameof(VolunteerSchedule.FriMorning)), "Fri AM");
            AddAvailabilitySlot(slots, GetAvailabilityValue(source, nameof(VolunteerSchedule.FriAfternoon)), "Fri PM");
            AddAvailabilitySlot(slots, GetAvailabilityValue(source, nameof(VolunteerSchedule.SatMorning)), "Sat AM");
            AddAvailabilitySlot(slots, GetAvailabilityValue(source, nameof(VolunteerSchedule.SatAfternoon)), "Sat PM");
            AddAvailabilitySlot(slots, GetAvailabilityValue(source, nameof(VolunteerSchedule.SunMorning)), "Sun AM");
            AddAvailabilitySlot(slots, GetAvailabilityValue(source, nameof(VolunteerSchedule.SunAfternoon)), "Sun PM");

            return slots.Count == 0 ? "No availability on file" : string.Join(", ", slots);
        }

        private static List<VolunteerSchedulerAvailabilityRowViewModel> BuildAvailabilityRows(
            VolunteerSchedule? availability,
            VolunteerApplication application)
        {
            var source = availability as object ?? application;

            return new List<VolunteerSchedulerAvailabilityRowViewModel>
            {
                BuildAvailabilityRow(source, "Mon", nameof(VolunteerSchedule.MonMorning), nameof(VolunteerSchedule.MonAfternoon)),
                BuildAvailabilityRow(source, "Tue", nameof(VolunteerSchedule.TueMorning), nameof(VolunteerSchedule.TueAfternoon)),
                BuildAvailabilityRow(source, "Wed", nameof(VolunteerSchedule.WedMorning), nameof(VolunteerSchedule.WedAfternoon)),
                BuildAvailabilityRow(source, "Thu", nameof(VolunteerSchedule.ThuMorning), nameof(VolunteerSchedule.ThuAfternoon)),
                BuildAvailabilityRow(source, "Fri", nameof(VolunteerSchedule.FriMorning), nameof(VolunteerSchedule.FriAfternoon)),
                BuildAvailabilityRow(source, "Sat", nameof(VolunteerSchedule.SatMorning), nameof(VolunteerSchedule.SatAfternoon)),
                BuildAvailabilityRow(source, "Sun", nameof(VolunteerSchedule.SunMorning), nameof(VolunteerSchedule.SunAfternoon))
            };
        }

        private static VolunteerSchedulerAvailabilityRowViewModel BuildAvailabilityRow(
            object source,
            string dayLabel,
            string morningPropertyName,
            string afternoonPropertyName)
        {
            return new VolunteerSchedulerAvailabilityRowViewModel
            {
                DayLabel = dayLabel,
                Morning = GetAvailabilityValue(source, morningPropertyName),
                Afternoon = GetAvailabilityValue(source, afternoonPropertyName)
            };
        }

        private static void AddAvailabilitySlot(List<string> slots, bool enabled, string label)
        {
            if (enabled)
            {
                slots.Add(label);
            }
        }

        private static bool GetAvailabilityValue(object source, string propertyName)
        {
            var property = source.GetType().GetProperty(propertyName);
            return property?.PropertyType == typeof(bool) && (bool)(property.GetValue(source) ?? false);
        }

        private static bool HasAvailabilityOnDate(
            VolunteerApplication application,
            VolunteerSchedule? availability,
            DateTime shiftDate)
        {
            var source = availability as object ?? application;

            return shiftDate.DayOfWeek switch
            {
                DayOfWeek.Monday => GetAvailabilityValue(source, nameof(VolunteerSchedule.MonMorning))
                    || GetAvailabilityValue(source, nameof(VolunteerSchedule.MonAfternoon)),
                DayOfWeek.Tuesday => GetAvailabilityValue(source, nameof(VolunteerSchedule.TueMorning))
                    || GetAvailabilityValue(source, nameof(VolunteerSchedule.TueAfternoon)),
                DayOfWeek.Wednesday => GetAvailabilityValue(source, nameof(VolunteerSchedule.WedMorning))
                    || GetAvailabilityValue(source, nameof(VolunteerSchedule.WedAfternoon)),
                DayOfWeek.Thursday => GetAvailabilityValue(source, nameof(VolunteerSchedule.ThuMorning))
                    || GetAvailabilityValue(source, nameof(VolunteerSchedule.ThuAfternoon)),
                DayOfWeek.Friday => GetAvailabilityValue(source, nameof(VolunteerSchedule.FriMorning))
                    || GetAvailabilityValue(source, nameof(VolunteerSchedule.FriAfternoon)),
                DayOfWeek.Saturday => GetAvailabilityValue(source, nameof(VolunteerSchedule.SatMorning))
                    || GetAvailabilityValue(source, nameof(VolunteerSchedule.SatAfternoon)),
                DayOfWeek.Sunday => GetAvailabilityValue(source, nameof(VolunteerSchedule.SunMorning))
                    || GetAvailabilityValue(source, nameof(VolunteerSchedule.SunAfternoon)),
                _ => false
            };
        }

        private static bool IsShiftWithinAvailability(
            VolunteerShift shift,
            VolunteerSchedule? availability,
            VolunteerApplication? fallbackApplication)
        {
            if (availability == null && fallbackApplication == null)
            {
                return false;
            }

            var noon = TimeSpan.FromHours(12);
            var needsMorning = shift.StartTime < noon;
            var needsAfternoon = shift.EndTime > noon;

            if (!needsMorning && !needsAfternoon)
            {
                return false;
            }

            bool HasAvailability(string propertyName)
            {
                return (availability != null && GetAvailabilityValue(availability, propertyName))
                    || (fallbackApplication != null && GetAvailabilityValue(fallbackApplication, propertyName));
            }

            bool HasRequiredSlots(string morningPropertyName, string afternoonPropertyName)
            {
                return (!needsMorning || HasAvailability(morningPropertyName))
                    && (!needsAfternoon || HasAvailability(afternoonPropertyName));
            }

            return shift.ShiftDate.DayOfWeek switch
            {
                DayOfWeek.Monday => HasRequiredSlots(nameof(VolunteerSchedule.MonMorning), nameof(VolunteerSchedule.MonAfternoon)),
                DayOfWeek.Tuesday => HasRequiredSlots(nameof(VolunteerSchedule.TueMorning), nameof(VolunteerSchedule.TueAfternoon)),
                DayOfWeek.Wednesday => HasRequiredSlots(nameof(VolunteerSchedule.WedMorning), nameof(VolunteerSchedule.WedAfternoon)),
                DayOfWeek.Thursday => HasRequiredSlots(nameof(VolunteerSchedule.ThuMorning), nameof(VolunteerSchedule.ThuAfternoon)),
                DayOfWeek.Friday => HasRequiredSlots(nameof(VolunteerSchedule.FriMorning), nameof(VolunteerSchedule.FriAfternoon)),
                DayOfWeek.Saturday => HasRequiredSlots(nameof(VolunteerSchedule.SatMorning), nameof(VolunteerSchedule.SatAfternoon)),
                DayOfWeek.Sunday => HasRequiredSlots(nameof(VolunteerSchedule.SunMorning), nameof(VolunteerSchedule.SunAfternoon)),
                _ => false
            };
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
