using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Pantreats.Data;
using Pantreats.Models;

namespace Pantreats.Controllers
{
    public class InventoryController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;

        public InventoryController(HttpClient httpClient, ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _context = context;
        }

        public IActionResult Index()
        {
            var inventory = _context.Inventory
                .AsNoTracking()
                .OrderBy(i => i.ItemName)
                .Select(i => new Inventory
                {
                    ItemId = i.ItemId,
                    UPC = i.UPC,
                    ItemName = i.ItemName,
                    Quantity = i.Quantity,
                    BrandName = i.BrandName,
                    Subcategory = i.Subcategory,
                    UnitSize = i.UnitSize,
                    Points = i.Points
                })
                .ToList();

            ViewBag.Count = inventory.Count;
            return View(inventory);
        }

        [HttpGet]
        public async Task<IActionResult> Create(string? upc)
        {
            var model = new Inventory
            {
                UPC = upc?.Trim() ?? string.Empty,
                ItemName = string.Empty,
                BrandName = string.Empty,
                Category = string.Empty,
                Subcategory = string.Empty,
                GenderUse = string.Empty,
                UnitSize = string.Empty,
                Quantity = 1
            };

            if (!string.IsNullOrWhiteSpace(upc))
            {
                var lookupResult = await TryLookupProductAsync(upc.Trim());

                if (lookupResult.Success && lookupResult.Product != null)
                {
                    ApplyLookupToInventory(model, lookupResult.Product, upc.Trim());
                    ViewBag.LookupImageUrl = lookupResult.Product.images;
                    ViewBag.LookupSucceeded = true;
                }
                else
                {
                    ViewBag.LookupError = lookupResult.ErrorMessage ?? "Product not found";
                }
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult LookupUPC(string upc)
        {
            if (string.IsNullOrWhiteSpace(upc))
            {
                return RedirectToAction(nameof(Create));
            }

            return RedirectToAction(nameof(Create), new { upc = upc.Trim() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Inventory item, IFormFile? imageFile, string? imageUrl)
        {
            NormalizeInventory(item);

            if (string.IsNullOrWhiteSpace(item.UPC))
            {
                ModelState.AddModelError(nameof(item.UPC), "UPC is required.");
            }

            if (await _context.Inventory.AnyAsync(i => i.UPC == item.UPC))
            {
                ModelState.AddModelError(nameof(item.UPC), "An inventory item with this UPC already exists.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.LookupImageUrl = imageUrl;
                return View(item);
            }

            _context.Inventory.Add(item);
            await _context.SaveChangesAsync();

            await SaveInventoryImageAsync(item.ItemId, imageFile, imageUrl);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> AddInventoryItem(Inventory item, IFormFile? imageFile, string? imageUrl)
        {
            return Create(item, imageFile, imageUrl);
        }

        public IActionResult Edit(int id)
        {
            var item = _context.Inventory.Include(i => i.InventoryImage).FirstOrDefault(i => i.ItemId == id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Inventory item, IFormFile? imageFile, string? imageUrl)
        {
            NormalizeInventory(item);

            if (string.IsNullOrWhiteSpace(item.UPC))
            {
                ModelState.AddModelError(nameof(item.UPC), "UPC is required.");
            }

            if (await _context.Inventory.AnyAsync(i => i.UPC == item.UPC && i.ItemId != item.ItemId))
            {
                ModelState.AddModelError(nameof(item.UPC), "An inventory item with this UPC already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(item);
            }

            var dbItem = await _context.Inventory.FirstOrDefaultAsync(i => i.ItemId == item.ItemId);

            if (dbItem == null)
            {
                return NotFound();
            }

            dbItem.UPC = item.UPC;
            dbItem.ItemName = item.ItemName;
            dbItem.BrandName = item.BrandName;
            dbItem.Category = item.Category;
            dbItem.Subcategory = item.Subcategory;
            dbItem.GenderUse = item.GenderUse;
            dbItem.UnitSize = item.UnitSize;
            dbItem.Quantity = item.Quantity;
            dbItem.Points = item.Points;

            await _context.SaveChangesAsync();
            await SaveInventoryImageAsync(item.ItemId, imageFile, imageUrl);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var item = _context.Inventory.FirstOrDefault(i => i.ItemId == id);

            if (item != null)
            {
                var itemName = item.ItemName;
                _context.Inventory.Remove(item);
                _context.SaveChanges();
                TempData["InventoryToast"] = $"{itemName} deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> GetImage(int? itemId, string? upc)
        {
            int? resolvedItemId = itemId;

            if (!resolvedItemId.HasValue && !string.IsNullOrWhiteSpace(upc))
            {
                resolvedItemId = await _context.Inventory
                    .Where(i => i.UPC == upc)
                    .Select(i => (int?)i.ItemId)
                    .FirstOrDefaultAsync();
            }

            if (!resolvedItemId.HasValue)
            {
                return NotFound();
            }

            var image = await _context.InventoryImages.FirstOrDefaultAsync(i => i.InventoryItemId == resolvedItemId.Value);

            if (image == null)
            {
                return NotFound();
            }

            return File(image.ImageData, image.ContentType);
        }

        private async Task SaveInventoryImageAsync(int itemId, IFormFile? imageFile, string? imageUrl)
        {
            byte[]? imageBytes = null;
            string? contentType = null;

            if (imageFile != null && imageFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await imageFile.CopyToAsync(ms);
                imageBytes = ms.ToArray();
                contentType = imageFile.ContentType;
            }
            else if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                try
                {
                    var response = await _httpClient.GetAsync(imageUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        imageBytes = await response.Content.ReadAsByteArrayAsync();
                        contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Image save failed: " + ex.Message);
                }
            }

            if (imageBytes == null)
            {
                return;
            }

            var existing = await _context.InventoryImages.FirstOrDefaultAsync(i => i.InventoryItemId == itemId);

            if (existing == null)
            {
                _context.InventoryImages.Add(new InventoryImage
                {
                    InventoryItemId = itemId,
                    ImageData = imageBytes,
                    ContentType = contentType ?? "image/jpeg"
                });
            }
            else
            {
                existing.ImageData = imageBytes;
                existing.ContentType = contentType ?? existing.ContentType;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<(bool Success, LookupResult? Product, string? ErrorMessage)> TryLookupProductAsync(string upc)
        {
            if (string.IsNullOrWhiteSpace(upc))
            {
                return (false, null, "UPC is required");
            }

            var apiUrl = $"https://api.upcitemdb.com/prod/trial/lookup?upc={upc}";

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    return (false, null, "Product not found");
                }

                var json = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeObject<dynamic>(json);

                if (obj?.items == null || obj.items.Count == 0)
                {
                    return (false, null, "Product not found");
                }

                var item = obj.items[0];

                var product = new LookupResult
                {
                    upc = upc,
                    title = item.title,
                    brand = item.brand,
                    category = item.category,
                    description = item.description,
                    weight = item.weight,
                    images = item.images != null && item.images.Count > 0 ? item.images[0].ToString() : null
                };

                return (true, product, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        private static void ApplyLookupToInventory(Inventory inventory, LookupResult product, string upc)
        {
            inventory.UPC = upc;
            inventory.ItemName = product.title ?? string.Empty;
            inventory.BrandName = product.brand ?? string.Empty;
            inventory.Category = product.category ?? string.Empty;
            inventory.UnitSize = product.weight ?? string.Empty;
        }

        private static void NormalizeInventory(Inventory item)
        {
            item.UPC = item.UPC?.Trim() ?? string.Empty;
            item.ItemName ??= string.Empty;
            item.BrandName ??= string.Empty;
            item.Category ??= string.Empty;
            item.Subcategory ??= string.Empty;
            item.GenderUse ??= string.Empty;
            item.UnitSize ??= string.Empty;
        }
    }
}
