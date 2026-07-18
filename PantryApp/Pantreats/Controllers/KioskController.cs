using Pantreats.Services;

namespace Pantreats.Controllers
{
    [Authorize(Roles = "Admin,Kiosk")]
    [Route("kiosk")]
    public class KioskController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CheckoutService _checkoutService;

        public KioskController(ApplicationDbContext context, CheckoutService checkoutService)
        {
            _context = context;
            _checkoutService = checkoutService;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Self Checkout Kiosk";
            ViewData["HideNavbar"] = true;
            ViewData["HideFooter"] = true;
            ViewData["FullWidth"] = true;
            return View();
        }

        [HttpPost("student/lookup")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LookupStudent([FromBody] KioskStudentLookupRequest request)
        {
            if (!int.TryParse(request?.StudentId?.Trim(), out var studentId))
            {
                return BadRequest(new { success = false, message = "Enter a valid student ID number." });
            }

            var student = await GetLatestStudentApplicationAsync(studentId);
            if (student == null || student.ApplicationStatus != ApplicationStatuses.Approved)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    message = "We couldn't find an approved pantry account for that student ID. Please see pantry staff."
                });
            }

            if (!student.CurrentPointBalance.HasValue)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    message = "That student does not have an active pantry point balance yet. Please see pantry staff."
                });
            }

            return Ok(new
            {
                success = true,
                studentId = student.StudentId,
                fullName = BuildFullName(student.FirstName, student.MiddleName, student.LastName),
                monthlyPointBalance = student.MonthlyPointBalance,
                currentPointBalance = student.CurrentPointBalance
            });
        }

        [HttpPost("item/lookup")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LookupItem([FromBody] KioskItemLookupRequest request)
        {
            var upc = request?.UPC?.Trim();
            if (string.IsNullOrWhiteSpace(upc))
            {
                return BadRequest(new { success = false, message = "Scan an item barcode to continue." });
            }

            var item = await _context.Inventory
                .AsNoTracking()
                .Where(currentItem => currentItem.UPC == upc)
                .Select(currentItem => new
                {
                    currentItem.UPC,
                    currentItem.ItemName,
                    currentItem.BrandName,
                    currentItem.Points,
                    currentItem.Quantity
                })
                .FirstOrDefaultAsync();

            if (item == null)
            {
                return NotFound(new { success = false, message = "That item barcode was not recognized." });
            }

            if (item.Quantity <= 0)
            {
                return BadRequest(new { success = false, message = $"{item.ItemName} is out of stock right now." });
            }

            return Ok(new { success = true, item });
        }

        [HttpPost("checkout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout([FromBody] KioskCheckoutRequest request)
        {
            if (!int.TryParse(request?.StudentId?.Trim(), out var studentId))
            {
                return BadRequest(new { success = false, message = "Enter a valid student ID number." });
            }

            var student = await GetLatestStudentApplicationAsync(studentId);
            if (student == null || student.ApplicationStatus != ApplicationStatuses.Approved)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    message = "We couldn't find an approved pantry account for that student ID. Please see pantry staff."
                });
            }

            var studentUser = await _context.Users
                .AsNoTracking()
                .Where(user => user.Id == student.UserId)
                .Select(user => new
                {
                    user.Id,
                    user.Email,
                    user.PhoneNumber
                })
                .FirstOrDefaultAsync();

            if (studentUser == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "That student account is unavailable right now. Please see pantry staff."
                });
            }

            var result = await _checkoutService.CreateOrderAsync(new CheckoutRequest
            {
                UserId = studentUser.Id,
                Email = studentUser.Email ?? string.Empty,
                PhoneNumber = studentUser.PhoneNumber,
                OrderSource = Order.SourceKiosk,
                CompleteImmediately = true,
                UPCs = request?.UPCs ?? new List<string>()
            });

            if (!result.Succeeded)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new
            {
                success = true,
                orderId = result.OrderId,
                orderDate = result.OrderDate,
                totalPoints = result.TotalPoints,
                remainingPoints = result.RemainingPoints,
                message = $"Checkout complete for {BuildFullName(student.FirstName, student.MiddleName, student.LastName)}."
            });
        }

        private async Task<UserApplication?> GetLatestStudentApplicationAsync(int studentId)
        {
            return await _context.UserApplications
                .AsNoTracking()
                .Where(application => application.StudentId == studentId)
                .OrderByDescending(application => application.RegistrationDate)
                .ThenByDescending(application => application.ApplicationId)
                .FirstOrDefaultAsync();
        }

        private static string BuildFullName(string? firstName, string? middleName, string? lastName)
        {
            return string.Join(" ", new[] { firstName, middleName, lastName }
                .Where(namePart => !string.IsNullOrWhiteSpace(namePart)));
        }
    }
}
