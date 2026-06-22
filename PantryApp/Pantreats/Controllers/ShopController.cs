using System.Text.RegularExpressions;
using System.Security.Claims;

namespace Pantreats.Controllers
{
    [Authorize(Roles = "Admin,Students")]
    [Route("shop")]
    public class ShopController : Controller
    {
        private const int PageSize = 20;
        private readonly ApplicationDbContext _context;

        public ShopController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int page = 1)
        {
            var accessResult = await EnsureApprovedStudentAccessAsync();
            if (accessResult != null)
            {
                return accessResult;
            }

            page = Math.Max(page, 1);

            var query = _context.Inventory
                .AsNoTracking()
                .OrderBy(i => i.ItemName)
                .ThenBy(i => i.BrandName);

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));
            page = Math.Min(page, totalPages);

            var items = await query
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
                    ImageName = BuildImageName(i.ItemName)
                })
                .ToListAsync();

            return View(new ShopViewModel
            {
                Items = items,
                PageNumber = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = PageSize
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
                    ImageName = BuildImageName(i.ItemName)
                })
                .FirstOrDefaultAsync();

            if (item == null)
            {
                return NotFound(new { success = false, message = "That item could not be found." });
            }

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
            var requestedItems = selectedUPCs
                .GroupBy(upc => upc)
                .ToDictionary(group => group.Key, group => group.Count());
            var distinctUPCs = requestedItems.Keys.ToList();

            if (!selectedUPCs.Any())
            {
                return BadRequest(new { success = false, message = "Add at least one item first." });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var inventoryItems = await _context.Inventory
                .Where(i => distinctUPCs.Contains(i.UPC))
                .ToListAsync();

            if (inventoryItems.Count != distinctUPCs.Count)
            {
                return BadRequest(new { success = false, message = "One of those items is no longer available." });
            }

            var unavailableItem = inventoryItems.FirstOrDefault(i => i.Quantity < requestedItems[i.UPC]);
            if (unavailableItem != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Only {unavailableItem.Quantity} {unavailableItem.ItemName} {(unavailableItem.Quantity == 1 ? "is" : "are")} available right now."
                });
            }

            Order? order = null;

            if (inventoryItems.Any())
            {
                order = new Order
                {
                    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "Anonymous",
                    Email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "Anonymous",
                    PhoneNum = "Not Provided",
                    OrderDate = DateTime.Now,
                    Total = selectedUPCs.Count
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var inventoryItem in inventoryItems)
                {
                    var orderQuantity = requestedItems[inventoryItem.UPC];
                    inventoryItem.Quantity -= orderQuantity;

                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.OrderId,
                        InventoryItemId = inventoryItem.ItemId,
                        InventoryUPC = inventoryItem.UPC,
                        ItemName = inventoryItem.ItemName,
                        Category = inventoryItem.Category,
                        OrderQuantity = orderQuantity,
                        Points = orderQuantity
                    });
                }

                _context.OrderFulfilments.Add(new OrderFulfilment
                {
                    OrderId = order.OrderId,
                    FulfilmentDate = DateTime.Now,
                    OrderStatus = "Waiting Pickup"
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                success = true,
                orderId = order?.OrderId,
                message = $"Order #{order?.OrderId} was placed. We'll get it ready for pickup."
            });
        }

        private static string BuildImageName(string itemName)
        {
            var normalized = itemName
                .Replace("&", "and", StringComparison.OrdinalIgnoreCase)
                .Replace("'", string.Empty)
                .ToLowerInvariant();

            return Regex.Replace(normalized, "[^a-z0-9]", string.Empty);
        }

        private async Task<IActionResult?> EnsureApprovedStudentAccessAsync()
        {
            if (User.IsInRole("Admin"))
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
