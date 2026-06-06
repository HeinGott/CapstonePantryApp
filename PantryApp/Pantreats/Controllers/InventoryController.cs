using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

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
            //query list of inventory items from db
            var inventory = _context.Inventory //.AsNoTracking() is important to keep page load fast -nick
                .AsNoTracking()
                .OrderBy(i => i.ItemName)
                .Select(i => new Inventory
                {
                    UPC = i.UPC,
                    ItemName = i.ItemName,
                    Quantity = i.Quantity,
                    BrandName = i.BrandName,
                    Category = i.Category
                })
                .ToList();
            // Store total count in ViewBag
            ViewBag.Count = inventory.Count;
            // Send list to the view
            return View(inventory);
        }



        [HttpPost]
        public async Task<IActionResult> LookupUPC(string upc)
        {
            if (string.IsNullOrEmpty(upc))
            {
                ViewBag.Error = "UPC is required";
                return View("UPCResult");
            }

            //string userKey = "YOUR_UPCITEMDB_KEY"; this project will use free api which does not need userkey, only update if switching to paid -Nick
            string apiUrl = $"https://api.upcitemdb.com/prod/trial/lookup?upc={upc}";

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                /*
                 * only use this if using paid version of api -Nick
                request.Headers.Add("user_key", userKey);
                request.Headers.Add("key_type", "3scale");
                */
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Product not found";
                    return View("UPCResult");
                }

                var json = await response.Content.ReadAsStringAsync();

                //UPCitemDB returns JSON object with items array -Nick
                var obj = JsonConvert.DeserializeObject<dynamic>(json);

                if (obj.items == null || obj.items.Count == 0)
                {
                    ViewBag.Error = "Product not found";
                    return View("UPCResult");
                }

                var item = obj.items[0];

                var product = new LookupResult
                {
                    title = item.title,
                    brand = item.brand,
                    category = item.category,
                    description = item.description,
                    weight = item.weight,
                    images = item.images != null && item.images.Count > 0 ? item.images[0].ToString() : null
                };

               


                ViewBag.UPC = upc;

                return View("UPCResult", product); 
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("UPCResult");
            }
        }


        [HttpPost]
        public async Task<IActionResult> AddInventoryItem(Inventory item, string imageUrl)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Invalid Model";
                return RedirectToAction(nameof(Index));
            }

            _context.Inventory.Add(item);
            _context.SaveChanges();

            //if imageurl exists save to db for item upc
            if (!string.IsNullOrEmpty(imageUrl))
            {
                try
                {
                    var response = await _httpClient.GetAsync(imageUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var bytes = await response.Content.ReadAsByteArrayAsync();

                        _context.InventoryImages.Add(new InventoryImage
                        {
                            UPC = item.UPC,
                            ImageData = bytes,
                            ContentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg"
                        });

                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Image save failed: " + ex.Message);
                }
            }


            return RedirectToAction(nameof(Index));
        }


        public IActionResult Edit(string upc)
        {
            var item = _context.Inventory.Include(i=> i.InventoryImage).FirstOrDefault(i => i.UPC == upc);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Inventory item)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                return BadRequest(errors);
            }
            var imageUrl = Request.Form["imageUrl"].ToString();
            var imageFile = Request.Form.Files["imageFile"];

            var dbItem = await _context.Inventory
                .FirstOrDefaultAsync(i => i.UPC == item.UPC); //find item in db because it is no longer tracked

            if (dbItem == null)
                return NotFound();

            
            dbItem.ItemName = item.ItemName;
            dbItem.BrandName = item.BrandName;
            dbItem.Category = item.Category;
            dbItem.Subcategory = item.Subcategory;
            dbItem.GenderUse = item.GenderUse;
            dbItem.UnitSize = item.UnitSize;
            dbItem.Quantity = item.Quantity;

            await _context.SaveChangesAsync();

            //check if user gave image
            if ((imageFile == null || imageFile.Length == 0) && string.IsNullOrWhiteSpace(imageUrl))
            {
                return RedirectToAction(nameof(Index));
            }

            byte[] imageBytes = null;
            string contentType = null;

            //user file upload
            if(imageFile != null && imageFile.Length > 0)
            {
                using var ms = new MemoryStream();
                
                await imageFile.CopyToAsync(ms);

                imageBytes = ms.ToArray();
                contentType = imageFile.ContentType;
            }

            //user image url
            else if(!string.IsNullOrWhiteSpace(imageUrl))
            {
                var response = await _httpClient.GetAsync(imageUrl);

                if (response.IsSuccessStatusCode)
                {
                    imageBytes = await response.Content.ReadAsByteArrayAsync();
                    contentType = response.Content.Headers.ContentType?.MediaType;
                }
            }

            if(imageBytes != null)
            {
                var existing = await _context.InventoryImages.FindAsync(item.UPC);

                //make new inventoryimage entry if not existing replace if existing in table
                if(existing == null)
                {
                    _context.InventoryImages.Add(new InventoryImage
                    {
                        UPC = item.UPC,
                        ImageData = imageBytes,
                        ContentType = contentType
                    });
                }
                else
                {
                    existing.ImageData = imageBytes;
                    existing.ContentType = contentType;
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(string upc)
        {
            var item = _context.Inventory.FirstOrDefault(i => i.UPC == upc);

            if (item != null)
            {
                _context.Inventory.Remove(item);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> GetImage(string upc)
        {
            var image = await _context.InventoryImages.FindAsync(upc);

            if(image == null)
            {
                return NotFound();
            }

            return File(image.ImageData, image.ContentType);
        }

    }
}
