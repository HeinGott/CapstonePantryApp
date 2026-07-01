using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pantreats.Data;
using Pantreats.Models;

namespace Pantreats.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Inventory()
        {
            var inventoryItems = await _context.Inventory
                .OrderBy(i => i.Category)
                .ThenBy(i => i.ItemName)
                .ToListAsync();

            var model = new InventoryReportViewModel
            {
                TotalInventoryItems = inventoryItems.Count,
                TotalInventoryQuantity = inventoryItems.Sum(i => i.Quantity),
                LowStockCount = inventoryItems.Count(i => i.Quantity <= 5),

                CategoryTotals = inventoryItems
                    .GroupBy(i => i.Category)
                    .Select(g => new InventoryCategoryReportItem
                    {
                        Category = g.Key,
                        ItemTypes = g.Count(),
                        TotalQuantity = g.Sum(i => i.Quantity)
                    })
                    .OrderByDescending(g => g.TotalQuantity)
                    .ToList(),

                LowStockItems = inventoryItems
                    .Where(i => i.Quantity <= 5)
                    .OrderBy(i => i.Quantity)
                    .ThenBy(i => i.ItemName)
                    .Select(i => new LowStockReportItem
                    {
                        ItemId = i.ItemId,
                        UPC = i.UPC,
                        ItemName = i.ItemName,
                        BrandName = i.BrandName,
                        Category = i.Category,
                        Quantity = i.Quantity,
                        Points = i.Points
                    })
                    .ToList(),

                FullInventory = inventoryItems
                    .Select(i => new InventoryFullReportItem
                    {
                        ItemId = i.ItemId,
                        UPC = i.UPC,
                        ItemName = i.ItemName,
                        BrandName = i.BrandName,
                        Category = i.Category,
                        Subcategory = i.Subcategory,
                        UnitSize = i.UnitSize,
                        Quantity = i.Quantity,
                        Points = i.Points
                    })
                    .ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> ExportInventory()
        {
            var inventoryItems = await _context.Inventory
                .OrderBy(i => i.Category)
                .ThenBy(i => i.ItemName)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Inventory Report");

            worksheet.Cell(1, 1).Value = "Pantry Inventory Report";
            worksheet.Range(1, 1, 1, 8).Merge();
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 18;

            worksheet.Cell(3, 1).Value = "Total Item Types";
            worksheet.Cell(3, 2).Value = inventoryItems.Count;

            worksheet.Cell(4, 1).Value = "Total Inventory Quantity";
            worksheet.Cell(4, 2).Value = inventoryItems.Sum(i => i.Quantity);

            worksheet.Cell(5, 1).Value = "Low Stock Items";
            worksheet.Cell(5, 2).Value = inventoryItems.Count(i => i.Quantity <= 5);

            var headerRow = 7;

            worksheet.Cell(headerRow, 1).Value = "UPC";
            worksheet.Cell(headerRow, 2).Value = "Item Name";
            worksheet.Cell(headerRow, 3).Value = "Brand";
            worksheet.Cell(headerRow, 4).Value = "Category";
            worksheet.Cell(headerRow, 5).Value = "Subcategory";
            worksheet.Cell(headerRow, 6).Value = "Unit Size";
            worksheet.Cell(headerRow, 7).Value = "Quantity";
            worksheet.Cell(headerRow, 8).Value = "Points";

            var currentRow = headerRow + 1;

            foreach (var item in inventoryItems)
            {
                worksheet.Cell(currentRow, 1).Value = item.UPC;
                worksheet.Cell(currentRow, 2).Value = item.ItemName;
                worksheet.Cell(currentRow, 3).Value = item.BrandName;
                worksheet.Cell(currentRow, 4).Value = item.Category;
                worksheet.Cell(currentRow, 5).Value = item.Subcategory;
                worksheet.Cell(currentRow, 6).Value = item.UnitSize;
                worksheet.Cell(currentRow, 7).Value = item.Quantity;
                worksheet.Cell(currentRow, 8).Value = item.Points;

                currentRow++;
            }

            var headerRange = worksheet.Range(headerRow, 1, headerRow, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"PantryInventoryReport-{DateTime.Now:yyyy-MM-dd}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        public async Task<IActionResult> Students()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var model = new StudentReportViewModel
            {
                TotalStudentsWithOrders = orders
                    .Select(o => o.Email)
                    .Distinct()
                    .Count(),

                TotalOrders = orders.Count,

                TotalPointsUsed = orders.Sum(o => o.Total),

                TotalItemsOrdered = orders
                    .SelectMany(o => o.OrderItems)
                    .Sum(oi => oi.OrderQuantity),

                StudentOrders = orders
                    .GroupBy(o => new { o.Email, o.UserId })
                    .Select(g => new StudentOrderReportItem
                    {
                        Email = g.Key.Email,
                        UserId = g.Key.UserId,
                        OrderCount = g.Count(),
                        TotalPointsUsed = g.Sum(o => o.Total),
                        TotalItemsOrdered = g.SelectMany(o => o.OrderItems).Sum(oi => oi.OrderQuantity),
                        MostRecentOrder = g.Max(o => o.OrderDate)
                    })
                    .OrderByDescending(g => g.OrderCount)
                    .ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> ExportStudents()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var studentOrders = orders
                .GroupBy(o => new { o.Email, o.UserId })
                .Select(g => new StudentOrderReportItem
                {
                    Email = g.Key.Email,
                    UserId = g.Key.UserId,
                    OrderCount = g.Count(),
                    TotalPointsUsed = g.Sum(o => o.Total),
                    TotalItemsOrdered = g.SelectMany(o => o.OrderItems).Sum(oi => oi.OrderQuantity),
                    MostRecentOrder = g.Max(o => o.OrderDate)
                })
                .OrderByDescending(g => g.OrderCount)
                .ToList();

            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Student Order Report");

            worksheet.Cell(1, 1).Value = "Student Order Report";
            worksheet.Range(1, 1, 1, 5).Merge();
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 18;

            worksheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:MM/dd/yyyy h:mm tt}";
            worksheet.Range(2, 1, 2, 5).Merge();

            worksheet.Cell(4, 1).Value = "Students With Orders";
            worksheet.Cell(4, 2).Value = studentOrders.Count;

            worksheet.Cell(5, 1).Value = "Total Orders";
            worksheet.Cell(5, 2).Value = orders.Count;

            worksheet.Cell(6, 1).Value = "Total Points Used";
            worksheet.Cell(6, 2).Value = orders.Sum(o => o.Total);

            worksheet.Cell(7, 1).Value = "Total Items Ordered";
            worksheet.Cell(7, 2).Value = orders
                .SelectMany(o => o.OrderItems)
                .Sum(oi => oi.OrderQuantity);

            var headerRow = 9;

            worksheet.Cell(headerRow, 1).Value = "Email";
            worksheet.Cell(headerRow, 2).Value = "Orders";
            worksheet.Cell(headerRow, 3).Value = "Total Points Used";
            worksheet.Cell(headerRow, 4).Value = "Total Items Ordered";
            worksheet.Cell(headerRow, 5).Value = "Most Recent Order";

            var currentRow = headerRow + 1;

            foreach (var student in studentOrders)
            {
                worksheet.Cell(currentRow, 1).Value = student.Email;
                worksheet.Cell(currentRow, 2).Value = student.OrderCount;
                worksheet.Cell(currentRow, 3).Value = student.TotalPointsUsed;
                worksheet.Cell(currentRow, 4).Value = student.TotalItemsOrdered;
                worksheet.Cell(currentRow, 5).Value = student.MostRecentOrder;

                worksheet.Cell(currentRow, 5).Style.DateFormat.Format = "mm/dd/yyyy";

                currentRow++;
            }

            var headerRange = worksheet.Range(headerRow, 1, headerRow, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"StudentOrderReport-{DateTime.Now:yyyy-MM-dd}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        public async Task<IActionResult> Usage()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderFulfilment)
                .ToListAsync();

            var orderItems = await _context.OrderItems.ToListAsync();

            var model = new UsageReportViewModel
            {
                TotalOrders = orders.Count,
                OnlineOrders = orders.Count(o => o.OrderSource == Order.SourceOnline),
                KioskOrders = orders.Count(o => o.OrderSource == Order.SourceKiosk),
                FulfilledOrders = orders.Count(o => o.OrderFulfilment != null),
                PendingOrders = orders.Count(o => o.OrderFulfilment == null),
                TotalPointsUsed = orders.Sum(o => o.Total),
                TotalItemsOrdered = orderItems.Sum(oi => oi.OrderQuantity),

                MostRequestedItems = orderItems
                    .GroupBy(oi => new { oi.ItemName, oi.Category })
                    .Select(g => new PopularItemReportItem
                    {
                        ItemName = g.Key.ItemName,
                        Category = g.Key.Category,
                        TotalQuantityOrdered = g.Sum(oi => oi.OrderQuantity),
                        TotalPointsUsed = g.Sum(oi => oi.Points * oi.OrderQuantity)
                    })
                    .OrderByDescending(g => g.TotalQuantityOrdered)
                    .Take(25)
                    .ToList(),

                OrdersByDate = orders
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new OrderDateReportItem
                    {
                        OrderDate = g.Key,
                        OrderCount = g.Count(),
                        TotalPoints = g.Sum(o => o.Total)
                    })
                    .OrderByDescending(g => g.OrderDate)
                    .ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> ExportUsage()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderFulfilment)
                .ToListAsync();

            var orderItems = await _context.OrderItems.ToListAsync();

            var mostRequestedItems = orderItems
                .GroupBy(oi => new { oi.ItemName, oi.Category })
                .Select(g => new PopularItemReportItem
                {
                    ItemName = g.Key.ItemName,
                    Category = g.Key.Category,
                    TotalQuantityOrdered = g.Sum(oi => oi.OrderQuantity),
                    TotalPointsUsed = g.Sum(oi => oi.Points * oi.OrderQuantity)
                })
                .OrderByDescending(g => g.TotalQuantityOrdered)
                .Take(25)
                .ToList();

            var ordersByDate = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new OrderDateReportItem
                {
                    OrderDate = g.Key,
                    OrderCount = g.Count(),
                    TotalPoints = g.Sum(o => o.Total)
                })
                .OrderByDescending(g => g.OrderDate)
                .ToList();

            using var workbook = new XLWorkbook();

            var summarySheet = workbook.Worksheets.Add("Usage Summary");

            summarySheet.Cell(1, 1).Value = "Pantry Usage Report";
            summarySheet.Range(1, 1, 1, 4).Merge();
            summarySheet.Cell(1, 1).Style.Font.Bold = true;
            summarySheet.Cell(1, 1).Style.Font.FontSize = 18;

            summarySheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:MM/dd/yyyy h:mm tt}";
            summarySheet.Range(2, 1, 2, 4).Merge();

            summarySheet.Cell(4, 1).Value = "Total Orders";
            summarySheet.Cell(4, 2).Value = orders.Count;

            summarySheet.Cell(5, 1).Value = "Total Items Ordered";
            summarySheet.Cell(5, 2).Value = orderItems.Sum(oi => oi.OrderQuantity);

            summarySheet.Cell(6, 1).Value = "Total Points Used";
            summarySheet.Cell(6, 2).Value = orders.Sum(o => o.Total);

            summarySheet.Cell(7, 1).Value = "Online Orders";
            summarySheet.Cell(7, 2).Value = orders.Count(o => o.OrderSource == Order.SourceOnline);

            summarySheet.Cell(8, 1).Value = "Kiosk Orders";
            summarySheet.Cell(8, 2).Value = orders.Count(o => o.OrderSource == Order.SourceKiosk);

            summarySheet.Cell(9, 1).Value = "Fulfilled Orders";
            summarySheet.Cell(9, 2).Value = orders.Count(o => o.OrderFulfilment != null);

            summarySheet.Cell(10, 1).Value = "Pending Orders";
            summarySheet.Cell(10, 2).Value = orders.Count(o => o.OrderFulfilment == null);

            summarySheet.Range(4, 1, 10, 1).Style.Font.Bold = true;
            summarySheet.Columns().AdjustToContents();

            var itemsSheet = workbook.Worksheets.Add("Most Requested Items");

            itemsSheet.Cell(1, 1).Value = "Most Requested Items";
            itemsSheet.Range(1, 1, 1, 4).Merge();
            itemsSheet.Cell(1, 1).Style.Font.Bold = true;
            itemsSheet.Cell(1, 1).Style.Font.FontSize = 18;

            var itemHeaderRow = 3;

            itemsSheet.Cell(itemHeaderRow, 1).Value = "Item Name";
            itemsSheet.Cell(itemHeaderRow, 2).Value = "Category";
            itemsSheet.Cell(itemHeaderRow, 3).Value = "Total Quantity Ordered";
            itemsSheet.Cell(itemHeaderRow, 4).Value = "Total Points Used";

            var currentItemRow = itemHeaderRow + 1;

            foreach (var item in mostRequestedItems)
            {
                itemsSheet.Cell(currentItemRow, 1).Value = item.ItemName;
                itemsSheet.Cell(currentItemRow, 2).Value = item.Category;
                itemsSheet.Cell(currentItemRow, 3).Value = item.TotalQuantityOrdered;
                itemsSheet.Cell(currentItemRow, 4).Value = item.TotalPointsUsed;

                currentItemRow++;
            }

            var itemHeaderRange = itemsSheet.Range(itemHeaderRow, 1, itemHeaderRow, 4);
            itemHeaderRange.Style.Font.Bold = true;
            itemHeaderRange.Style.Fill.BackgroundColor = XLColor.LightGreen;

            itemsSheet.Columns().AdjustToContents();

            var dateSheet = workbook.Worksheets.Add("Orders By Date");

            dateSheet.Cell(1, 1).Value = "Orders By Date";
            dateSheet.Range(1, 1, 1, 3).Merge();
            dateSheet.Cell(1, 1).Style.Font.Bold = true;
            dateSheet.Cell(1, 1).Style.Font.FontSize = 18;

            var dateHeaderRow = 3;

            dateSheet.Cell(dateHeaderRow, 1).Value = "Date";
            dateSheet.Cell(dateHeaderRow, 2).Value = "Order Count";
            dateSheet.Cell(dateHeaderRow, 3).Value = "Total Points Used";

            var currentDateRow = dateHeaderRow + 1;

            foreach (var date in ordersByDate)
            {
                dateSheet.Cell(currentDateRow, 1).Value = date.OrderDate;
                dateSheet.Cell(currentDateRow, 1).Style.DateFormat.Format = "mm/dd/yyyy";
                dateSheet.Cell(currentDateRow, 2).Value = date.OrderCount;
                dateSheet.Cell(currentDateRow, 3).Value = date.TotalPoints;

                currentDateRow++;
            }

            var dateHeaderRange = dateSheet.Range(dateHeaderRow, 1, dateHeaderRow, 3);
            dateHeaderRange.Style.Font.Bold = true;
            dateHeaderRange.Style.Fill.BackgroundColor = XLColor.LightGreen;

            dateSheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"PantryUsageReport-{DateTime.Now:yyyy-MM-dd}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

    }//end of authorize admin role
}//end of namespace