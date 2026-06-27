using Pantreats.Data;
using Pantreats.Models;

namespace Pantreats.Services
{
    public class CheckoutService
    {
        private readonly ApplicationDbContext _context;

        public CheckoutService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CheckoutResult> CreateOrderAsync(CheckoutRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserId))
            {
                return CheckoutResult.Failure("A valid student account is required.");
            }

            var selectedUPCs = (request.UPCs ?? new List<string>())
                .Where(upc => !string.IsNullOrWhiteSpace(upc))
                .Select(upc => upc.Trim())
                .ToList();

            if (selectedUPCs.Count == 0)
            {
                return CheckoutResult.Failure("Add at least one item first.");
            }

            if (request.OrderSource != Order.SourceKiosk)
            {
                var lastOrder = await _context.Orders
                    .Where(o => o.UserId == request.UserId &&
                                o.OrderSource != Order.SourceKiosk)
                    .OrderByDescending(o => o.OrderDate)
                    .FirstOrDefaultAsync();

                if (lastOrder != null && lastOrder.OrderDate > DateTime.Now.AddDays(-2))
                {
                    return CheckoutResult.Failure("You can only place one online order every 2 days. Please try again later.");
                }
            }

            var requestedItems = selectedUPCs
                .GroupBy(upc => upc, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

            var distinctUPCs = requestedItems.Keys.ToList();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var inventoryItems = await _context.Inventory
                .Where(item => distinctUPCs.Contains(item.UPC))
                .ToListAsync();

            if (inventoryItems.Count != distinctUPCs.Count)
            {
                return CheckoutResult.Failure("One of those items is no longer available.");
            }

            var unavailableItem = inventoryItems.FirstOrDefault(item => item.Quantity < requestedItems[item.UPC]);
            if (unavailableItem != null)
            {
                return CheckoutResult.Failure(
                    $"Only {unavailableItem.Quantity} {unavailableItem.ItemName} {(unavailableItem.Quantity == 1 ? "is" : "are")} available right now.");
            }

            var order = new Order
            {
                UserId = request.UserId,
                Email = string.IsNullOrWhiteSpace(request.Email) ? "Not Provided" : request.Email.Trim(),
                PhoneNum = string.IsNullOrWhiteSpace(request.PhoneNumber) ? "Not Provided" : request.PhoneNumber.Trim(),
                OrderDate = DateTime.Now,
                Total = 0,
                OrderSource = string.IsNullOrWhiteSpace(request.OrderSource) ? Order.SourceOnline : request.OrderSource.Trim()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var totalPoints = 0;

            foreach (var inventoryItem in inventoryItems)
            {
                var quantity = requestedItems[inventoryItem.UPC];
                inventoryItem.Quantity -= quantity;

                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.OrderId,
                    InventoryItemId = inventoryItem.ItemId,
                    InventoryUPC = inventoryItem.UPC,
                    ItemName = inventoryItem.ItemName,
                    Category = inventoryItem.Category,
                    OrderQuantity = quantity,
                    Points = inventoryItem.Points
                });

                totalPoints += inventoryItem.Points * quantity;
            }

            order.Total = totalPoints;

            var fulfilmentDate = order.OrderDate;
            var fulfilmentStatus = request.CompleteImmediately
                ? OrderFulfilment.StatusCompleted
                : OrderFulfilment.StatusOrderPlaced;

            _context.OrderFulfilments.Add(new OrderFulfilment
            {
                OrderId = order.OrderId,
                FulfilmentDate = fulfilmentDate,
                DateReceived = request.CompleteImmediately ? fulfilmentDate : null,
                OrderStatus = fulfilmentStatus
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CheckoutResult.Success(order.OrderId, totalPoints, order.OrderDate);
        }
    }

    public class CheckoutRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string OrderSource { get; set; } = Order.SourceOnline;
        public bool CompleteImmediately { get; set; }
        public List<string> UPCs { get; set; } = new();
    }

    public class CheckoutResult
    {
        public bool Succeeded { get; private set; }
        public int? OrderId { get; private set; }
        public int TotalPoints { get; private set; }
        public DateTime? OrderDate { get; private set; }
        public string Message { get; private set; } = string.Empty;

        public static CheckoutResult Success(int orderId, int totalPoints, DateTime orderDate)
        {
            return new CheckoutResult
            {
                Succeeded = true,
                OrderId = orderId,
                TotalPoints = totalPoints,
                OrderDate = orderDate
            };
        }

        public static CheckoutResult Failure(string message)
        {
            return new CheckoutResult
            {
                Succeeded = false,
                Message = message
            };
        }
    }
}
