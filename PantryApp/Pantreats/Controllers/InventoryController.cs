using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Pantreats.Controllers
{
    public class InventoryController : Controller
    {

        private readonly HttpClient _httpClient;

        public InventoryController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public IActionResult Index()
        {
            // Create a list to store inventory items
            var inventory = new List<Inventory>();

            // Get path to the CSV file
            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "App_Data",
                "FakePanTreatInventory.csv"
            );

            // Check if file exists
            if (System.IO.File.Exists(filePath))
            {
                // Read all lines and skip the header (first row)
                var lines = System.IO.File.ReadAllLines(filePath).Skip(1);

                // Loop through each line in the file
                foreach (var line in lines)
                {
                    // Split line by commas
                    var parts = line.Split(',');

                    // Add item to list
                    inventory.Add(new Inventory
                    {
                        UPC = parts[0],
                        ItemName = parts[1],
                        BrandName = parts[2],
                        Category = parts[3],
                        Subcategory = parts[4],     // Column D
                        GenderUse = parts[5],    // Column E
                        UnitSize = parts[6],     // Column F
                        Quantity = int.Parse(parts[7].Trim()), // Column G
                    });
                }
            }

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

            //string userKey = "YOUR_UPCITEMDB_KEY"; this project will use free api which does not need userkey, only update if switching to paid
            string apiUrl = $"https://api.upcitemdb.com/prod/trial/lookup?upc={upc}";

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                /*
                 * only use this if using paid version of api
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

                //UPCitemDB returns JSON object with items array
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

                return View("UPCResult", product);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("UPCResult");
            }
        }


    }
}
