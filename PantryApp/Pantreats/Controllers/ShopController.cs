using System.Text.RegularExpressions;
using System.Security.Claims;
using Pantreats.Services;

namespace Pantreats.Controllers
{
    [Authorize(Roles = "Admin,Students,Volunteers")]
    [Route("shop")]
    public class ShopController : Controller
    {
        private const int PageSize = 12;
        private readonly ApplicationDbContext _context;
        private readonly CheckoutService _checkoutService;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;

        public ShopController(ApplicationDbContext context, CheckoutService checkoutService, IEmailService emailService, IWebHostEnvironment environment)
        {
            _context = context;
            _checkoutService = checkoutService;
            _emailService = emailService;
            _environment = environment;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int page = 1, string? search = null, string? subcategory = null)
        {
            var accessResult = await EnsureApprovedStudentAccessAsync();
            if (accessResult != null)
            {
                return accessResult;
            }

            var studentApplication = await GetLatestStudentApplicationAsync();

            page = Math.Max(page, 1);
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            subcategory = string.IsNullOrWhiteSpace(subcategory) || subcategory.Equals("all", StringComparison.OrdinalIgnoreCase)
                ? null
                : subcategory.Trim();

            var baseQuery = _context.Inventory.AsNoTracking();
            var totalInventoryItems = await baseQuery.CountAsync();
            var subcategories = await baseQuery
                .Where(i => i.Subcategory != null && i.Subcategory != string.Empty)
                .Select(i => i.Subcategory)
                .Distinct()
                .OrderBy(currentSubcategory => currentSubcategory)
                .ToListAsync();

            var query = baseQuery;

            if (!string.IsNullOrWhiteSpace(subcategory))
            {
                query = query.Where(i => i.Subcategory == subcategory);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i =>
                    i.ItemName.Contains(search) ||
                    i.BrandName.Contains(search) ||
                    i.Subcategory.Contains(search) ||
                    i.UnitSize.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));
            page = Math.Min(page, totalPages);

            var items = await query
                .OrderBy(i => i.ItemName)
                .ThenBy(i => i.BrandName)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(i => new ShopItemViewModel
                {
                    ItemId = i.ItemId,
                    UPC = i.UPC,
                    ItemName = i.ItemName,
                    BrandName = i.BrandName,
                    Category = i.Category,
                    Subcategory = i.Subcategory,
                    UnitSize = i.UnitSize,
                    Quantity = i.Quantity,
                    Points = i.Points,
                    ImageName = BuildImageName(i.ItemName)
                })
                .ToListAsync();

            foreach (var item in items)
            {
                item.ImageUrl = ResolveShopImageUrl(item) ?? Url.Action("GetImage", "Inventory", new { itemId = item.ItemId }) ?? "/images/pantreatsLogo.png";
            }

            return View(new ShopViewModel
            {
                Items = items,
                Subcategories = subcategories,
                PageNumber = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                TotalInventoryItems = totalInventoryItems,
                SearchTerm = search,
                SelectedSubcategory = subcategory,
                PageSize = PageSize,
                MonthlyPointBalance = studentApplication?.MonthlyPointBalance,
                CurrentPointBalance = studentApplication?.CurrentPointBalance
            });
        }

        [HttpPost("cart/add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart([FromBody] ShopAddCartRequest request)
        {
            var accessResult = await EnsureApprovedStudentJsonAccessAsync();
            if (accessResult != null)
            {
                return accessResult;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.UPC))
            {
                return BadRequest(new { success = false, message = "Choose an item first." });
            }

            var requestedQuantity = Math.Max(1, request.RequestedQuantity);

            var item = await _context.Inventory
                .AsNoTracking()
                .Where(i => i.UPC == request.UPC)
                .Select(i => new ShopItemViewModel
                {
                    ItemId = i.ItemId,
                    UPC = i.UPC,
                    ItemName = i.ItemName,
                    BrandName = i.BrandName,
                    Category = i.Category,
                    Subcategory = i.Subcategory,
                    UnitSize = i.UnitSize,
                    Quantity = i.Quantity,
                    Points = i.Points,
                    ImageName = BuildImageName(i.ItemName)
                })
                .FirstOrDefaultAsync();

            if (item == null)
            {
                return NotFound(new { success = false, message = "That item could not be found." });
            }

            item.ImageUrl = ResolveShopImageUrl(item) ?? Url.Action("GetImage", "Inventory", new { itemId = item.ItemId }) ?? "/images/pantreatsLogo.png";

            if (item.Quantity <= 0)
            {
                return BadRequest(new { success = false, message = $"{item.ItemName} is out of stock right now." });
            }

            if (item.Quantity < requestedQuantity)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Only {item.Quantity} {item.ItemName} {(item.Quantity == 1 ? "is" : "are")} available right now."
                });
            }

            return Ok(new { success = true, item });
        }

        [HttpPost("checkout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout([FromBody] ShopCheckoutRequest request)
        {
            var accessResult = await EnsureApprovedStudentJsonAccessAsync();
            if (accessResult != null)
            {
                return accessResult;
            }

            var selectedUPCs = request?.UPCs?
                .Where(upc => !string.IsNullOrWhiteSpace(upc))
                .ToList() ?? new List<string>();

            if (!selectedUPCs.Any())
            {
                return BadRequest(new { success = false, message = "Add at least one item first." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? string.Empty;

            if (!User.IsInRole("Admin") && !User.IsInRole("Kiosk"))
            {
                var lastOnlineOrder = await _context.Orders
                    .Where(o => o.UserId == userId && o.OrderSource == Order.SourceOnline)
                    .OrderByDescending(o => o.OrderDate)
                    .FirstOrDefaultAsync();

                if (lastOnlineOrder != null && lastOnlineOrder.OrderDate > DateTime.Now.AddDays(-2))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "You can only place one online order every 2 days. Please try again later."
                    });
                }
            }

            var result = await _checkoutService.CreateOrderAsync(new CheckoutRequest
            {
                UserId = userId,
                Email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? string.Empty,
                PhoneNumber = "Not Provided",
                OrderSource = Order.SourceOnline,
                CompleteImmediately = false,
                UPCs = selectedUPCs
            });

            if (!result.Succeeded)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            var confirmationEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(confirmationEmail) && result.OrderId.HasValue)
            {
                var orderUrl = Url.Action("Details", "Order", new { id = result.OrderId.Value }, Request.Scheme) ?? string.Empty;
                await _emailService.SendOrderConfirmationAsync(confirmationEmail, result.OrderId.Value, orderUrl);
            }

            return Ok(new
            {
                success = true,
                orderId = result.OrderId,
                remainingPoints = result.RemainingPoints,
                message = $"Order #{result.OrderId} was placed. We'll get it ready for pickup."
            });
        }

        private async Task<UserApplication?> GetLatestStudentApplicationAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await _context.UserApplications
                .AsNoTracking()
                .Where(application => application.UserId == userId)
                .OrderByDescending(application => application.RegistrationDate)
                .ThenByDescending(application => application.ApplicationId)
                .FirstOrDefaultAsync();
        }

        private static string BuildImageName(string itemName)
        {
            var normalized = itemName
                .Replace("&", "and", StringComparison.OrdinalIgnoreCase)
                .Replace("'", string.Empty)
                .ToLowerInvariant();

            return Regex.Replace(normalized, "[^a-z0-9]", string.Empty);
        }

        private string? ResolveShopImageUrl(ShopItemViewModel item)
        {
            var folderPath = Path.Combine(_environment.WebRootPath, "images", "items");
            if (!Directory.Exists(folderPath))
            {
                return null;
            }

            var baseNames = new[]
            {
                item.UPC,
                item.ImageName,
                item.ItemName
            }
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase);

            var extensions = new[] { ".webp", ".png", ".jpg", ".jpeg" };

            foreach (var baseName in baseNames)
            {
                foreach (var extension in extensions)
                {
                    var fileName = $"{baseName}{extension}";
                    var filePath = Path.Combine(folderPath, fileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        return $"/images/items/{Uri.EscapeDataString(fileName)}";
                    }
                }
            }

            return null;
        }

        private async Task<IActionResult?> EnsureApprovedStudentAccessAsync()
        {
            if (User.IsInRole("Admin"))
            {
                return null;
            }
            if (User.IsInRole("Volunteers"))
            {
                return null;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var status = await _context.UserApplications
                .AsNoTracking()
                .Where(application => application.UserId == userId)
                .OrderByDescending(application => application.RegistrationDate)
                .ThenByDescending(application => application.ApplicationId)
                .Select(application => application.ApplicationStatus)
                .FirstOrDefaultAsync();

            if (status == ApplicationStatuses.Approved)
            {
                return null;
            }

            TempData["ApplicationAccessMessage"] = "Your student application still needs approval before shop access is unlocked.";
            return RedirectToAction("Status", "Student");
        }

        private async Task<IActionResult?> EnsureApprovedStudentJsonAccessAsync()
        {
            var accessResult = await EnsureApprovedStudentAccessAsync();
            if (accessResult == null || User.IsInRole("Admin"))
            {
                return null;
            }

            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                success = false,
                message = "Your student application still needs approval before shop access is unlocked."
            });
        }
    }
}
