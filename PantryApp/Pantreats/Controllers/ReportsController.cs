using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Pantreats.Models;

namespace Pantreats.Controllers
{
    public class ReportsController : Controller
    {
        public IActionResult Inventory()
        {
            var inventory = GetInventoryFromCsv();

            return View(inventory);
        }

        public IActionResult Students()
        {
            return View();
        }

        public IActionResult Usage()
        {
            return View();
        }

        public IActionResult ExportInventory()
        {
            var inventory = GetInventoryFromCsv();

            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Inventory Report");

            worksheet.Cell(1, 1).Value = "UPC";
            worksheet.Cell(1, 2).Value = "Item Name";
            worksheet.Cell(1, 3).Value = "Brand Name";
            worksheet.Cell(1, 4).Value = "Category";
            worksheet.Cell(1, 5).Value = "Subcategory";
            worksheet.Cell(1, 6).Value = "Gender Use";
            worksheet.Cell(1, 7).Value = "Unit Size";
            worksheet.Cell(1, 8).Value = "Quantity";

            var headerRange = worksheet.Range("A1:H1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            var row = 2;

            foreach (var item in inventory)
            {
                worksheet.Cell(row, 1).Value = item.UPC;
                worksheet.Cell(row, 2).Value = item.ItemName;
                worksheet.Cell(row, 3).Value = item.BrandName;
                worksheet.Cell(row, 4).Value = item.Category;
                worksheet.Cell(row, 5).Value = item.Subcategory;
                worksheet.Cell(row, 6).Value = item.GenderUse;
                worksheet.Cell(row, 7).Value = item.UnitSize;
                worksheet.Cell(row, 8).Value = item.Quantity;

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = "InventoryReport.xlsx";

            //designates download type
            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        private List<Inventory> GetInventoryFromCsv()
        {
            var inventory = new List<Inventory>();

            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "App_Data",
                "FakePanTreatInventory.csv"
            );

            if (System.IO.File.Exists(filePath))
            {
                var lines = System.IO.File.ReadAllLines(filePath).Skip(1);

                foreach (var line in lines)
                {
                    var parts = line.Split(',');

                    inventory.Add(new Inventory
                    {
                        UPC = parts[0],
                        ItemName = parts[1],
                        BrandName = parts[2],
                        Category = parts[3],
                        Subcategory = parts[4],
                        GenderUse = parts[5],
                        UnitSize = parts[6],
                        Quantity = int.Parse(parts[7].Trim())
                    });
                }
            }

            return inventory;
        }
    }
}