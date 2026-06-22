using System.Text.RegularExpressions;

namespace Pantreats.Data
{
    public class DbSeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            //check admin role exists -nick
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

           
            var adminEmail = "admin@admin.com";
            var adminPassword = "Admin_123";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(adminUser, adminPassword);
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        public static async Task SeedStudentAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            //check Students role exists -nick
            if (!await roleManager.RoleExistsAsync("Students"))
            {
                await roleManager.CreateAsync(new IdentityRole("Students"));
            }


            var studentEmail = "student@student.com";
            var studentPassword = "Student_123";

            var studentUser = await userManager.FindByEmailAsync(studentEmail);
            if (studentUser == null)
            {
                studentUser = new ApplicationUser
                {
                    UserName = studentEmail,
                    Email = studentEmail,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(studentUser, studentPassword);
                await userManager.AddToRoleAsync(studentUser, "Students");
            }
        }


        public static async Task SeedDonorAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            //check Donors role exists -nick
            if (!await roleManager.RoleExistsAsync("Donors"))
            {
                await roleManager.CreateAsync(new IdentityRole("Donors"));
            }


            var donorEmail = "donor@donor.com";
            var donorPassword = "Donor_123";

            var donorUser = await userManager.FindByEmailAsync(donorEmail);
            if (donorUser == null)
            {
                donorUser = new ApplicationUser
                {
                    UserName = donorEmail,
                    Email = donorEmail,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(donorUser, donorPassword);
                await userManager.AddToRoleAsync(donorUser, "Donors");
            }
            else if (await userManager.IsInRoleAsync(donorUser, "Vendors"))
            {
                await userManager.RemoveFromRoleAsync(donorUser, "Vendors");

                if (!await userManager.IsInRoleAsync(donorUser, "Donors"))
                {
                    await userManager.AddToRoleAsync(donorUser, "Donors");
                }
            }
        }

        public static async Task SeedVolunteerAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            //check Volunteers role exists -nick
            if (!await roleManager.RoleExistsAsync("Volunteers"))
            {
                await roleManager.CreateAsync(new IdentityRole("Volunteers"));
            }


            var volunteerEmail = "volunteer@volunteer.com";
            var volunteerPassword = "Volunteer_123";

            var volunteerUser = await userManager.FindByEmailAsync(volunteerEmail);
            if (volunteerUser == null)
            {
                volunteerUser = new ApplicationUser
                {
                    UserName = volunteerEmail,
                    Email = volunteerEmail,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(volunteerUser, volunteerPassword);
                await userManager.AddToRoleAsync(volunteerUser, "Volunteers");
            }
        }

        
        public static void SeedInventory(ApplicationDbContext context, IWebHostEnvironment env)
        {
            if (context.Inventory.AsNoTracking().Any()) //check if inventory table is empty -nick
                return;

            var filePath = Path.Combine(env.ContentRootPath, "App_Data", "InventorySeed.csv");

            if (!File.Exists(filePath)) //check if file exists -nick
                return;

            var lines = File.ReadAllLines(filePath);

            var items = new List<Inventory>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;



                var columns = line.Split(',');
                if (columns.Length < 2)
                    continue;

                var name = columns[0].Trim();
                var upc = columns[1].Trim();


                items.Add(new Inventory
                {
                    UPC = upc,
                    ItemName = name,
                    BrandName = GetBrand(name),
                    Category = GetCategory(name),
                    Subcategory = GetSubcategory(name),
                    GenderUse = "All",
                    UnitSize = GetUnitSize(name),
                    Quantity = 25
                });
            }

            context.Inventory.AddRange(items);
            context.SaveChanges();

        }

        //helper methods that search for keywords in the names of each item-nick
        private static string GetCategory(string name)
        {
            if (name.Contains("Toothpaste")) return "Personal Care";
            if (name.Contains("Deodorant")) return "Personal Care";
            return "Food";
        }

        private static string GetBrand(string name)
        {
            var brands = new[]
            {
            "Great Value", "Food Lion", "Publix", "Del Monte",
            "Campbell", "Lidl", "Laura Lynn", "Swanson",
            "Bush's", "Kirkland", "Crest", "Colgate", "Founders", 
            "Lakeside", "Happy Harvest", "Cheeze-It", "Nature Valley",
            "Clover Valley", "Armor", "Green Giant", "Progresso", "Swanson", 
            "Crider", "Maruchan", "Marie Callender's", "American Beauty",
            "Goya", "Dakota's Pride", "Barilla", "Northern Catch",
            "IGA", "Mothers Maid", "Nutter Butter", "Teddy Grahams",
            "Chicken of the Sea", "Stokely's", "Nissin", "Lance",
            "Iberia", "Mission Pride", "Early Garden","Garden", "Quaker", "Cheetos",
            "Lay's", "Velveeta", "Garolalo", "Pirate's Booty", "Member's Mark",
            "Mueller's", "La Moderna", "Chef's Cupboard", "Combino"
            };

            return brands.FirstOrDefault(b => name.StartsWith(b)) ?? "Unknown";
        }


        private static string GetSubcategory(string name)
        {
            name = name.ToLower();

            //vegetables
            if (name.Contains("green beans")) return "Vegetables";
            if (name.Contains("corn")) return "Vegetables";
            if (name.Contains("peas")) return "Vegetables";
            if (name.Contains("carrot")) return "Vegetables";
            if (name.Contains("beet")) return "Vegetables";
            if (name.Contains("spinach")) return "Vegetables";

            //beans
            if (name.Contains("black bean")) return "Beans";
            if (name.Contains("pinto")) return "Beans";
            if (name.Contains("garbanzo") || name.Contains("chickpea")) return "Beans";
            if (name.Contains("blackeye") || name.Contains("black eyed")) return "Beans";
            if (name.Contains("pork and beans")) return "Beans";
            if (name.Contains("lentil")) return "Beans";
            if (name.Contains("pigeon peas")) return "Beans";

            //tomatoes
            if (name.Contains("tomato paste")) return "Tomatoes";
            if (name.Contains("tomato sauce")) return "Tomatoes";
            if (name.Contains("diced tomato")) return "Tomatoes";
            if (name.Contains("stewed tomato")) return "Tomatoes";

            //soups
            if (name.Contains("soup")) return "Soups";
            if (name.Contains("chicken noodle")) return "Soups";
            if (name.Contains("vegetable soup")) return "Soups";
            if (name.Contains("beef soup")) return "Soups";
            if (name.Contains("dumplings")) return "Soups";
            if (name.Contains("cream of chicken")) return "Soups";
            if (name.Contains("cream of mushroom")) return "Soups";
            if (name.Contains("tomato soup")) return "Soups";
            if (name.Contains("condensed soup")) return "Soups";

            //meat
            if (name.Contains("tuna")) return "Meat";
            if (name.Contains("salmon")) return "Meat";
            if (name.Contains("chicken")) return "Meat";
            if (name.Contains("chicken breast")) return "Meat";
            if (name.Contains("vienna sausage")) return "Meat";

            //pasta
            if (name.Contains("spaghetti")) return "Pasta";
            if (name.Contains("macaroni")) return "Pasta";
            if (name.Contains("ramen")) return "Pasta";
            if (name.Contains("cup noodles")) return "Pasta";

            //snacks
            if (name.Contains("cheetos")) return "Snacks";
            if (name.Contains("chips")) return "Snacks";
            if (name.Contains("veggie straws")) return "Snacks";
            if (name.Contains("cheeze-it")) return "Snacks";
            if (name.Contains("cracker")) return "Snacks";
            if (name.Contains("teddy grahams") || name.Contains("nutter butter"))
                return "Snacks";

            if (name.Contains("granola")) return "Snacks";

            //personal Care
            if (name.Contains("toothpaste")) return "Oral Care";
            if (name.Contains("deodorant")) return "Hygiene";
            if (name.Contains("soap")) return "Hygiene";

            return "Pantry - Other";
        }


        private static string GetUnitSize(string name) //looks for a number and oz in item name. defaults to standard if no oz in name -nick
        {
            var match = Regex.Match(name, @"\d+(\.\d+)?\s?oz"); 
            return match.Success ? match.Value : "Standard";
        }


        public static void ClearInventory(ApplicationDbContext context) //method to delete all inventory in db table, used for testing
        {
            var allItems = context.Inventory.ToList();

            if (!allItems.Any())
                return;

            context.Inventory.RemoveRange(allItems);
            context.SaveChanges();
        }

    }
}
