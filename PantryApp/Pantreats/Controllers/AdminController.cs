using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Pantreats.Models;
using Microsoft.EntityFrameworkCore;
using Pantreats.Data;
using System.Security.Claims;

namespace Pantreats.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private static readonly Dictionary<string, string> AllowedRoles = new()
        {
            ["Admin"] = "Admin",
            ["Students"] = "Student",
            ["Volunteers"] = "Volunteer",
            ["Donors"] = "Donor",
            ["Kiosk"] = "Kiosk"
        };

        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Include(order => order.OrderItems)
                .Include(order => order.OrderFulfilment)
                .Where(order => string.IsNullOrWhiteSpace(order.OrderSource) || order.OrderSource != Order.SourceKiosk)
                .OrderByDescending(order => order.OrderDate)
                .ToListAsync();

            var applications = await _context.UserApplications
                .AsNoTracking()
                .OrderByDescending(application => application.RegistrationDate)
                .ThenByDescending(application => application.ApplicationId)
                .ToListAsync();

            var latestApplications = applications
                .GroupBy(application => application.UserId)
                .Select(group => group.First())
                .ToList();

            var volunteerApplications = await _context.VolunteerApplications
                .AsNoTracking()
                .OrderByDescending(application => application.SubmittedDate)
                .ThenByDescending(application => application.VolunteerApplicationId)
                .ToListAsync();

            var latestVolunteerApplications = volunteerApplications
                .GroupBy(application => application.UserId)
                .Select(group => group.First())
                .ToList();

            var donations = await _context.Donations
                .AsNoTracking()
                .Include(donation => donation.Donor)
                .Include(donation => donation.DonationItems)
                .OrderByDescending(donation => donation.DonationDate)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                NewOrders = orders.Count(order =>
                    string.Equals(
                        OrderFulfilment.NormalizeStatus(order.OrderFulfilment?.OrderStatus),
                        OrderFulfilment.StatusOrderPlaced,
                        StringComparison.OrdinalIgnoreCase)),
                PendingApplications = latestApplications.Count(application =>
                    string.IsNullOrWhiteSpace(application.ApplicationStatus) ||
                    application.ApplicationStatus == ApplicationStatuses.Pending),
                PendingVolunteerApplications = latestVolunteerApplications.Count(application =>
                    string.IsNullOrWhiteSpace(application.ApplicationStatus) ||
                    application.ApplicationStatus == ApplicationStatuses.Pending),
                NewDonations = donations.Count(donation =>
                    string.Equals(donation.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
                RecentOrders = orders
                    .Where(order =>
                        string.Equals(
                            OrderFulfilment.NormalizeStatus(order.OrderFulfilment?.OrderStatus),
                            OrderFulfilment.StatusOrderPlaced,
                            StringComparison.OrdinalIgnoreCase))
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
                    .ToList(),
                RecentApplications = latestApplications
                    .Where(application =>
                        string.IsNullOrWhiteSpace(application.ApplicationStatus) ||
                        application.ApplicationStatus == ApplicationStatuses.Pending)
                    .OrderBy(application => application.RegistrationDate)
                    .Take(4)
                    .Select(application => new AdminDashboardApplicationViewModel
                    {
                        ApplicationId = application.ApplicationId,
                        StudentName = BuildFullName(application.FirstName, application.MiddleName, application.LastName),
                        StudentId = application.StudentId,
                        SubmittedAt = application.RegistrationDate
                    })
                    .ToList(),
                RecentDonations = donations
                    .Take(4)
                    .Select(donation => new AdminDashboardDonationViewModel
                    {
                        DonationId = donation.DonationId,
                        DonorName = donation.Donor?.Name ?? "Donor",
                        SubmittedAt = donation.DonationDate,
                        Status = donation.Status,
                        ItemCount = donation.DonationItems.Count,
                        TotalUnits = donation.DonationItems.Sum(item => item.Quantity)
                    })
                    .ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();

            var model = new List<UserManagerViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var roleValue = NormalizeRole(roles.FirstOrDefault());

                model.Add(new UserManagerViewModel
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    Role = GetRoleLabel(roleValue),
                    RoleValue = roleValue
                });
            }

            return View(model);
        }

        public async Task<IActionResult> UserEdit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var studentApplication = await _context.UserApplications
                .AsNoTracking()
                .Where(application => application.UserId == id)
                .OrderByDescending(application => application.RegistrationDate)
                .ThenByDescending(application => application.ApplicationId)
                .FirstOrDefaultAsync();

            var volunteerApplication = await _context.VolunteerApplications
                .AsNoTracking()
                .Where(application => application.UserId == id)
                .OrderByDescending(application => application.SubmittedDate)
                .ThenByDescending(application => application.VolunteerApplicationId)
                .FirstOrDefaultAsync();

            var donor = await _context.Donors
                .AsNoTracking()
                .FirstOrDefaultAsync(existingDonor => existingDonor.UserId == id);

            var model = new UserEditViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Role = NormalizeRole(roles.FirstOrDefault()),
                StudentApplicationId = studentApplication?.ApplicationId,
                StudentSubmittedAt = studentApplication?.RegistrationDate,
                StudentReviewedAt = studentApplication?.ReviewedAt,
                StudentApplicationStatus = studentApplication?.ApplicationStatus,
                StudentReviewNotes = studentApplication?.ReviewNotes,
                StudentApplicationIsActive = studentApplication?.IsActive ?? false,
                StudentApplicationIsVolunteer = studentApplication?.IsVolunteer ?? false,
                StudentNumber = studentApplication?.StudentId,
                StudentFirstName = studentApplication?.FirstName ?? string.Empty,
                StudentMiddleName = studentApplication?.MiddleName ?? string.Empty,
                StudentLastName = studentApplication?.LastName ?? string.Empty,
                StudentDateOfBirth = studentApplication?.DOB,
                StudentApplicationPhoneNumber = studentApplication?.PhoneNum ?? string.Empty,
                StudentGender = studentApplication?.Gender ?? string.Empty,
                StudentStatus = studentApplication?.StudentStatus ?? string.Empty,
                StudentCampus = studentApplication?.Campus ?? string.Empty,
                HouseholdBabiesToddlers = studentApplication?.HouseholdBabiesToddlers ?? 0,
                HouseholdChildren = studentApplication?.HouseholdBabiesChildren ?? 0,
                HouseholdTeens = studentApplication?.HouseholdTeens ?? 0,
                HouseholdAdults = studentApplication?.HouseholdAdults ?? 0,
                HasTransportation = studentApplication?.HasTransportation,
                EmploymentStatus = studentApplication?.EmploymentStatus ?? string.Empty,
                EmployedHouseMembers = studentApplication?.EmployedHouseMembers ?? 0,
                HasSnap = studentApplication?.HasSNAP ?? false,
                HasWic = studentApplication?.HasWIC ?? false,
                HasTanf = studentApplication?.HasTANF ?? false,
                InterestedInSnap = studentApplication?.IsInterestedInSNAP ?? false,
                InterestedInWic = studentApplication?.IsInterestedInWIC ?? false,
                InterestedInTanf = studentApplication?.IsInterestedInTANF ?? false,
                VolunteerApplicationId = volunteerApplication?.VolunteerApplicationId,
                VolunteerSubmittedAt = volunteerApplication?.SubmittedDate,
                VolunteerReviewedAt = volunteerApplication?.ReviewedAt,
                VolunteerApplicationStatus = volunteerApplication?.ApplicationStatus,
                VolunteerReviewNotes = volunteerApplication?.ReviewNotes,
                VolunteerFirstName = volunteerApplication?.FirstName ?? string.Empty,
                VolunteerLastName = volunteerApplication?.LastName ?? string.Empty,
                VolunteerEmail = volunteerApplication?.Email ?? string.Empty,
                VolunteerPhoneNumber = volunteerApplication?.PhoneNum ?? string.Empty,
                VolunteerYear = volunteerApplication?.Year ?? string.Empty,
                HasVolunteeredBefore = volunteerApplication?.HasVolunteeredBefore ?? false,
                PreviousCapacity = volunteerApplication?.PreviousCapacity,
                ReasonForVolunteering = volunteerApplication?.ReasonForVolunteering ?? string.Empty,
                VolunteerFrequency = volunteerApplication?.VolunteerFrequency ?? string.Empty,
                OtherFrequency = volunteerApplication?.OtherFrequency,
                MonMorning = volunteerApplication?.MonMorning ?? false,
                MonAfternoon = volunteerApplication?.MonAfternoon ?? false,
                TueMorning = volunteerApplication?.TueMorning ?? false,
                TueAfternoon = volunteerApplication?.TueAfternoon ?? false,
                WedMorning = volunteerApplication?.WedMorning ?? false,
                WedAfternoon = volunteerApplication?.WedAfternoon ?? false,
                ThuMorning = volunteerApplication?.ThuMorning ?? false,
                ThuAfternoon = volunteerApplication?.ThuAfternoon ?? false,
                FriMorning = volunteerApplication?.FriMorning ?? false,
                FriAfternoon = volunteerApplication?.FriAfternoon ?? false,
                SatMorning = volunteerApplication?.SatMorning ?? false,
                SatAfternoon = volunteerApplication?.SatAfternoon ?? false,
                SunMorning = volunteerApplication?.SunMorning ?? false,
                SunAfternoon = volunteerApplication?.SunAfternoon ?? false,
                DonorId = donor?.DonorID,
                DonorName = donor?.Name ?? string.Empty,
                DonorEmail = donor?.Email,
                DonorPhoneNumber = donor?.PhoneNumber,
                DonorAddress = donor?.Address
            };

            PopulateRoleOptions(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserEdit(UserEditViewModel model)
        {
            if (!AllowedRoles.ContainsKey(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role), "Select admin, student, volunteer, donor, or kiosk.");
            }

            if (model.StudentDateOfBirth > DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.StudentDateOfBirth), "Date of birth cannot be in the future.");
            }

            if (!ModelState.IsValid)
            {
                PopulateRoleOptions(model);
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            user.Email = model.Email;
            user.UserName = model.UserName;
            user.PhoneNumber = model.PhoneNumber;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                AddErrors(updateResult);
                PopulateRoleOptions(model);
                return View(model);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    AddErrors(removeResult);
                    PopulateRoleOptions(model);
                    return View(model);
                }
            }

            await EnsureRoleExists(model.Role);
            var addResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!addResult.Succeeded)
            {
                AddErrors(addResult);
                PopulateRoleOptions(model);
                return View(model);
            }

            if (model.StudentApplicationId.HasValue)
            {
                var studentApplication = await _context.UserApplications
                    .FirstOrDefaultAsync(application =>
                        application.ApplicationId == model.StudentApplicationId.Value &&
                        application.UserId == model.UserId);

                if (studentApplication != null)
                {
                    studentApplication.StudentId = model.StudentNumber ?? studentApplication.StudentId;
                    studentApplication.FirstName = model.StudentFirstName;
                    studentApplication.MiddleName = model.StudentMiddleName;
                    studentApplication.LastName = model.StudentLastName;
                    studentApplication.DOB = model.StudentDateOfBirth;
                    studentApplication.PhoneNum = model.StudentApplicationPhoneNumber;
                    studentApplication.Gender = model.StudentGender;
                    studentApplication.StudentStatus = model.StudentStatus;
                    studentApplication.Campus = model.StudentCampus;
                    studentApplication.HouseholdBabiesToddlers = model.HouseholdBabiesToddlers;
                    studentApplication.HouseholdBabiesChildren = model.HouseholdChildren;
                    studentApplication.HouseholdTeens = model.HouseholdTeens;
                    studentApplication.HouseholdAdults = model.HouseholdAdults;
                    studentApplication.HasTransportation = model.HasTransportation;
                    studentApplication.EmploymentStatus = model.EmploymentStatus;
                    studentApplication.EmployedHouseMembers = model.EmployedHouseMembers;
                    studentApplication.HasSNAP = model.HasSnap;
                    studentApplication.HasWIC = model.HasWic;
                    studentApplication.HasTANF = model.HasTanf;
                    studentApplication.IsInterestedInSNAP = model.InterestedInSnap;
                    studentApplication.IsInterestedInWIC = model.InterestedInWic;
                    studentApplication.IsInterestedInTANF = model.InterestedInTanf;
                    studentApplication.ApplicationStatus = string.IsNullOrWhiteSpace(model.StudentApplicationStatus)
                        ? studentApplication.ApplicationStatus
                        : model.StudentApplicationStatus.Trim();
                    studentApplication.ReviewNotes = string.IsNullOrWhiteSpace(model.StudentReviewNotes)
                        ? null
                        : model.StudentReviewNotes.Trim();
                    studentApplication.IsActive = model.StudentApplicationIsActive;
                    studentApplication.IsVolunteer = model.StudentApplicationIsVolunteer;
                }
            }

            if (model.VolunteerApplicationId.HasValue)
            {
                var volunteerApplication = await _context.VolunteerApplications
                    .FirstOrDefaultAsync(application =>
                        application.VolunteerApplicationId == model.VolunteerApplicationId.Value &&
                        application.UserId == model.UserId);

                if (volunteerApplication != null)
                {
                    volunteerApplication.FirstName = model.VolunteerFirstName;
                    volunteerApplication.LastName = model.VolunteerLastName;
                    volunteerApplication.Email = model.VolunteerEmail;
                    volunteerApplication.PhoneNum = model.VolunteerPhoneNumber;
                    volunteerApplication.Year = model.VolunteerYear;
                    volunteerApplication.HasVolunteeredBefore = model.HasVolunteeredBefore;
                    volunteerApplication.PreviousCapacity = string.IsNullOrWhiteSpace(model.PreviousCapacity)
                        ? null
                        : model.PreviousCapacity.Trim();
                    volunteerApplication.ReasonForVolunteering = model.ReasonForVolunteering;
                    volunteerApplication.VolunteerFrequency = model.VolunteerFrequency;
                    volunteerApplication.OtherFrequency = string.IsNullOrWhiteSpace(model.OtherFrequency)
                        ? null
                        : model.OtherFrequency.Trim();
                    volunteerApplication.ApplicationStatus = string.IsNullOrWhiteSpace(model.VolunteerApplicationStatus)
                        ? volunteerApplication.ApplicationStatus
                        : model.VolunteerApplicationStatus.Trim();
                    volunteerApplication.ReviewNotes = string.IsNullOrWhiteSpace(model.VolunteerReviewNotes)
                        ? null
                        : model.VolunteerReviewNotes.Trim();
                    volunteerApplication.MonMorning = model.MonMorning;
                    volunteerApplication.MonAfternoon = model.MonAfternoon;
                    volunteerApplication.TueMorning = model.TueMorning;
                    volunteerApplication.TueAfternoon = model.TueAfternoon;
                    volunteerApplication.WedMorning = model.WedMorning;
                    volunteerApplication.WedAfternoon = model.WedAfternoon;
                    volunteerApplication.ThuMorning = model.ThuMorning;
                    volunteerApplication.ThuAfternoon = model.ThuAfternoon;
                    volunteerApplication.FriMorning = model.FriMorning;
                    volunteerApplication.FriAfternoon = model.FriAfternoon;
                    volunteerApplication.SatMorning = model.SatMorning;
                    volunteerApplication.SatAfternoon = model.SatAfternoon;
                    volunteerApplication.SunMorning = model.SunMorning;
                    volunteerApplication.SunAfternoon = model.SunAfternoon;
                }
            }

            if (model.DonorId.HasValue)
            {
                var donor = await _context.Donors
                    .FirstOrDefaultAsync(existingDonor =>
                        existingDonor.DonorID == model.DonorId.Value &&
                        existingDonor.UserId == model.UserId);

                if (donor != null)
                {
                    donor.Name = model.DonorName;
                    donor.Email = model.DonorEmail;
                    donor.PhoneNumber = model.DonorPhoneNumber;
                    donor.Address = model.DonorAddress;
                }
            }

            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "User updated successfully.";
            TempData["StatusType"] = "success";
            return RedirectToAction(nameof(UserEdit), new { id = model.UserId });
        }

        // Edits the user's role from the user list.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string userID, string newRole)
        {
            if (string.IsNullOrEmpty(newRole) || !AllowedRoles.ContainsKey(newRole))
            {
                return RedirectToAction("Users");
            }

            var user = await _userManager.FindByIdAsync(userID);

            if (user != null)
            {
                var prevRoles = await _userManager.GetRolesAsync(user);

                var wasVolunteer = prevRoles.Contains("Volunteers");
                var changingToStudent = newRole == "Students";

                if (prevRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, prevRoles);
                }

                await EnsureRoleExists(newRole);
                await _userManager.AddToRoleAsync(user, newRole);

                if (wasVolunteer && changingToStudent)
                {
                    var volunteerApplications = await _context.VolunteerApplications
                        .Where(application => application.UserId == userID)
                        .ToListAsync();

                    if (volunteerApplications.Any())
                    {
                        _context.VolunteerApplications.RemoveRange(volunteerApplications);
                    }

                    var volunteerSchedules = await _context.VolunteerSchedules
                        .Where(schedule => schedule.UserId == userID)
                        .ToListAsync();

                    if (volunteerSchedules.Any())
                    {
                        _context.VolunteerSchedules.RemoveRange(volunteerSchedules);
                    }

                    var scheduleChangeRequests = await _context.ScheduleChangeRequests
                        .Where(request => request.UserId == userID)
                        .ToListAsync();

                    if (scheduleChangeRequests.Any())
                    {
                        _context.ScheduleChangeRequests.RemoveRange(scheduleChangeRequests);
                    }

                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return RedirectToAction(nameof(Users));
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.Equals(currentUserId, userId, StringComparison.Ordinal))
            {
                TempData["StatusMessage"] = "You can't delete the account you're currently signed in with.";
                TempData["StatusType"] = "error";
                return RedirectToAction(nameof(UserEdit), new { id = userId });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["StatusMessage"] = "That user no longer exists.";
                TempData["StatusType"] = "error";
                return RedirectToAction(nameof(Users));
            }

            var email = user.Email ?? string.Empty;
            var userName = user.UserName ?? string.Empty;

            var donor = await _context.Donors
                .FirstOrDefaultAsync(existingDonor => existingDonor.UserId == userId);

            if (donor != null)
            {
                var donations = await _context.Donations
                    .Where(donation => donation.DonorId == donor.DonorID)
                    .ToListAsync();

                if (donations.Any())
                {
                    var donationIds = donations.Select(donation => donation.DonationId).ToList();
                    var donationItems = await _context.DonationItems
                        .Where(donationItem => donationIds.Contains(donationItem.DonationId))
                        .ToListAsync();

                    if (donationItems.Any())
                    {
                        _context.DonationItems.RemoveRange(donationItems);
                    }

                    _context.Donations.RemoveRange(donations);
                }

                _context.Donors.Remove(donor);
            }

            var userApplications = await _context.UserApplications
                .Where(application => application.UserId == userId)
                .ToListAsync();

            if (userApplications.Any())
            {
                _context.UserApplications.RemoveRange(userApplications);
            }

            var volunteerApplications = await _context.VolunteerApplications
                .Where(application => application.UserId == userId)
                .ToListAsync();

            if (volunteerApplications.Any())
            {
                _context.VolunteerApplications.RemoveRange(volunteerApplications);
            }

            var volunteerSchedules = await _context.VolunteerSchedules
                .Where(schedule => schedule.UserId == userId)
                .ToListAsync();

            if (volunteerSchedules.Any())
            {
                _context.VolunteerSchedules.RemoveRange(volunteerSchedules);
            }

            var scheduleChangeRequests = await _context.ScheduleChangeRequests
                .Where(request => request.UserId == userId)
                .ToListAsync();

            if (scheduleChangeRequests.Any())
            {
                _context.ScheduleChangeRequests.RemoveRange(scheduleChangeRequests);
            }

            var itemRequests = await _context.ItemRequest
                .Where(request => request.UserName == userName || (!string.IsNullOrWhiteSpace(email) && request.UserName == email))
                .ToListAsync();

            if (itemRequests.Any())
            {
                _context.ItemRequest.RemoveRange(itemRequests);
            }

            var orders = await _context.Orders
                .Where(order => order.UserId == userId ||
                                order.UserId == userName ||
                                (!string.IsNullOrWhiteSpace(email) && (order.UserId == email || order.Email == email)))
                .ToListAsync();

            if (orders.Any())
            {
                var orderIds = orders.Select(order => order.OrderId).ToList();

                var orderItems = await _context.OrderItems
                    .Where(orderItem => orderIds.Contains(orderItem.OrderId))
                    .ToListAsync();

                if (orderItems.Any())
                {
                    _context.OrderItems.RemoveRange(orderItems);
                }

                var fulfilments = await _context.OrderFulfilments
                    .Where(fulfilment => orderIds.Contains(fulfilment.OrderId))
                    .ToListAsync();

                if (fulfilments.Any())
                {
                    _context.OrderFulfilments.RemoveRange(fulfilments);
                }

                _context.Orders.RemoveRange(orders);
            }

            await _context.SaveChangesAsync();

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                TempData["StatusMessage"] = deleteResult.Errors.FirstOrDefault()?.Description ?? "Unable to delete that user.";
                TempData["StatusType"] = "error";
                return RedirectToAction(nameof(UserEdit), new { id = userId });
            }

            TempData["StatusMessage"] = $"Deleted user {email}.";
            TempData["StatusType"] = "success";
            return RedirectToAction(nameof(Users));
        }

        // View Donations - Trevor
        public IActionResult Donations()
        {
            // Retrieves all donations from the database, including the associated donation items, then orders them by donation date in descending order
            var donations = _context.Donations
                .Include(d => d.DonationItems)
                .Include(d => d.Donor)
                .OrderByDescending(d => d.DonationDate)
                .ToList();

            // Passes the list of donations to the view for display
            return View(donations);
        }

        public IActionResult ProcessDonation(int id)
        {
            var donation = _context.Donations
                .Include(d => d.DonationItems)
                .Include(d => d.Donor)
                .FirstOrDefault(d => d.DonationId == id);

            if (donation == null)
            {
                return NotFound();
            }

            return View(donation);
        }

        public async Task<IActionResult> ReceivingDonations()
        {
            var model = await BuildReceivingDonationViewModelAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceivingDonations(ReceivingDonationViewModel model)
        {
            var donorMode = string.Equals(model.DonorMode, "manual", StringComparison.OrdinalIgnoreCase)
                ? "manual"
                : "existing";

            model.DonorMode = donorMode;

            if (donorMode == "existing")
            {
                if (!model.SelectedDonorId.HasValue)
                {
                    ModelState.AddModelError(nameof(model.SelectedDonorId), "Select a registered donor.");
                }
            }
            else if (string.IsNullOrWhiteSpace(model.ManualName))
            {
                ModelState.AddModelError(nameof(model.ManualName), "Enter a name for the donor.");
            }

            var selectedItems = (model.Items ?? new List<DonationItemInput>())
                .Where(item => item.Selected && item.Quantity > 0)
                .ToList();

            if (!selectedItems.Any())
            {
                ModelState.AddModelError(nameof(model.Items), "Select at least one donated item.");
            }

            if (!ModelState.IsValid)
            {
                var hydratedModel = await BuildReceivingDonationViewModelAsync(model);
                return View(hydratedModel);
            }

            Donor donor;

            if (donorMode == "existing")
            {
                donor = await _context.Donors
                    .FirstOrDefaultAsync(existingDonor =>
                        existingDonor.DonorID == model.SelectedDonorId)
                    ?? null!;

                if (donor == null)
                {
                    ModelState.AddModelError(nameof(model.SelectedDonorId), "Select a valid registered donor.");
                    var hydratedModel = await BuildReceivingDonationViewModelAsync(model);
                    return View(hydratedModel);
                }
            }
            else
            {
                donor = new Donor
                {
                    Name = model.ManualName!.Trim(),
                    PhoneNumber = string.IsNullOrWhiteSpace(model.ManualPhoneNumber) ? null : model.ManualPhoneNumber.Trim(),
                    Email = string.IsNullOrWhiteSpace(model.ManualEmail) ? null : model.ManualEmail.Trim(),
                    Address = string.IsNullOrWhiteSpace(model.ManualAddress) ? null : model.ManualAddress.Trim()
                };

                _context.Donors.Add(donor);
                await _context.SaveChangesAsync();
            }

            var donation = new Donation
            {
                DonorId = donor.DonorID,
                DonationDate = DateTime.Now,
                Status = "Received",
                Address = string.IsNullOrWhiteSpace(model.DonationAddress) ? donor.Address : model.DonationAddress.Trim(),
                Comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim(),
                DonationItems = selectedItems
                    .Select(item => new DonationItem
                    {
                        ItemName = item.Name,
                        Quantity = item.Quantity
                    })
                    .ToList()
            };

            _context.Donations.Add(donation);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = $"Received donation saved for {donor.Name}.";
            TempData["StatusType"] = "success";

            return RedirectToAction("Details", "Donor", new { id = donor.DonorID });
        }

        // Approve Donations - Trevor
        [HttpPost]
        public IActionResult ApproveDonation(int id)
        {
            // Find the donation by ID
            var donation = _context.Donations.FirstOrDefault(d => d.DonationId == id);

            // If the donation is not found, return a 404 error
            if (donation == null)
            {
                return NotFound();
            }

            // Update the donation status to "Approved"
            donation.Status = "Approved";
            _context.SaveChanges();

            // Redirect back to the Donations view
            return RedirectToAction("Donations");
        }

        // Deny Donations - Trevor
        [HttpPost]
        public IActionResult DenyDonation(int id)
        {
            // Find the donation by ID
            var donation = _context.Donations.FirstOrDefault(d => d.DonationId == id);

            // If the donation is not found, return a 404 error
            if (donation == null)
            {
                return NotFound();
            }

            // Update the donation status to "Denied"
            donation.Status = "Denied";
            _context.SaveChanges();

            // Redirects back to the Donations view
            return RedirectToAction("Donations");
        }

        private static string NormalizeRole(string? role)
        {
            return role switch
            {
                "Admin" => "Admin",
                "Student" or "Students" => "Students",
                "Volunteer" or "Volunteers" => "Volunteers",
                "Donor" or "Donors" or "Vendor" or "Vendors" => "Donors",
                "Kiosk" => "Kiosk",
                _ => string.Empty
            };
        }

        private static string GetRoleLabel(string? role)
        {
            var normalizedRole = NormalizeRole(role);

            return string.IsNullOrEmpty(normalizedRole)
                ? "No Role"
                : AllowedRoles[normalizedRole];
        }

        private static string BuildFullName(string firstName, string middleName, string lastName)
        {
            return string.Join(" ", new[] { firstName, middleName, lastName }
                .Where(namePart => !string.IsNullOrWhiteSpace(namePart)));
        }

        private async Task<ReceivingDonationViewModel> BuildReceivingDonationViewModelAsync(ReceivingDonationViewModel? model = null)
        {
            model ??= new ReceivingDonationViewModel();

            if (model.Items == null || model.Items.Count == 0)
            {
                model.Items = BuildDonationItemInputs();
            }

            model.RegisteredDonors = await _context.Donors
                .AsNoTracking()
                .OrderBy(donor => donor.Name)
                .Select(donor => new AdminDonorOptionViewModel
                {
                    DonorId = donor.DonorID,
                    Name = donor.Name,
                    Email = donor.Email,
                    PhoneNumber = donor.PhoneNumber
                })
                .ToListAsync();

            return model;
        }

        private static List<DonationItemInput> BuildDonationItemInputs()
        {
            var itemNames = new[]
            {
                "Canned Goods",
                "Produce",
                "Meat",
                "Bread & Bakery",
                "Rice & Pasta",
                "Dairy Products",
                "Frozen Foods",
                "Snacks",
                "Beverages",
                "Hygiene Products",
                "Baby Supplies",
                "Pet Food"
            };

            return itemNames
                .Select(itemName => new DonationItemInput
                {
                    Name = itemName,
                    Quantity = 1
                })
                .ToList();
        }

        private static void PopulateRoleOptions(UserEditViewModel model)
        {
            model.AvailableRoles = AllowedRoles
                .Select(role => new SelectListItem
                {
                    Value = role.Key,
                    Text = role.Value,
                    Selected = role.Key == model.Role
                })
                .ToList();
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private async Task EnsureRoleExists(string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
