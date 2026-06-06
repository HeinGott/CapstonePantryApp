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
            var inventory = _context.Inventory.OrderBy(i => i.ItemName).ToList();

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

                //if imageurl exists save to db for item upc
                if (!string.IsNullOrEmpty(product.images))
                {
                    try
                    {
                        var imageResponse = await _httpClient.GetAsync(product.images);

                        if (imageResponse.IsSuccessStatusCode)
                        {
                            byte[] imageBytes = await imageResponse.Content.ReadAsByteArrayAsync(); //turn image into byte array so it can be stored as varbinary(max) in db

                            string contentType = imageResponse.Content.Headers.ContentType?.MediaType;

                            var existingImage = await _context.InventoryImages.FindAsync(upc);

                            //if image exists for upc then we do nothing, but if none we save to table

                            if(existingImage == null)
                            {
                                _context.InventoryImages.Add(new InventoryImage
                                {
                                    UPC = upc,
                                    ImageData = imageBytes,
                                    ContentType = contentType
                                });

                            }

                            await _context.SaveChangesAsync(); 
                        }
                    }
                    catch
                    {

                    }
                }


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
        public IActionResult AddInventoryItem(Inventory item)
        {
            if (!ModelState.IsValid)
            {
                return View(item);
            }

            _context.Inventory.Add(item);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }


        public IActionResult Edit(string upc)
        {
            var item = _context.Inventory.FirstOrDefault(i => i.UPC == upc);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        public IActionResult Edit(Inventory item)
        {
            if (ModelState.IsValid)
            {
                _context.Inventory.Update(item);
                _context.SaveChanges();

                return RedirectToAction(nameof(Index));
            }

            return View(item);
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
