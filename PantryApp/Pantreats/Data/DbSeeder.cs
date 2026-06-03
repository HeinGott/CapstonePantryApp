namespace Pantreats.Data
{
    public class DbSeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

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
                adminUser = new IdentityUser
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
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            //check admin role exists -nick
            if (!await roleManager.RoleExistsAsync("Students"))
            {
                await roleManager.CreateAsync(new IdentityRole("Students"));
            }


            var studentEmail = "student@student.com";
            var studentPassword = "Student_123";

            var studentUser = await userManager.FindByEmailAsync(studentEmail);
            if (studentUser == null)
            {
                studentUser = new IdentityUser
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
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            //check admin role exists -nick
            if (!await roleManager.RoleExistsAsync("Vendors"))
            {
                await roleManager.CreateAsync(new IdentityRole("Vendors"));
            }


            var donorEmail = "donor@donor.com";
            var donorPassword = "Donor_123";

            var donorUser = await userManager.FindByEmailAsync(donorEmail);
            if (donorUser == null)
            {
                donorUser = new IdentityUser
                {
                    UserName = donorEmail,
                    Email = donorEmail,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(donorUser, donorPassword);
                await userManager.AddToRoleAsync(donorUser, "Vendors");
            }
        }

        public static async Task SeedVolunteerAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            //check admin role exists -nick
            if (!await roleManager.RoleExistsAsync("Volunteers"))
            {
                await roleManager.CreateAsync(new IdentityRole("Volunteers"));
            }


            var volunteerEmail = "volunteer@volunteer.com";
            var volunteerPassword = "Volunteer_123";

            var volunteerUser = await userManager.FindByEmailAsync(volunteerEmail);
            if (volunteerUser == null)
            {
                volunteerUser = new IdentityUser
                {
                    UserName = volunteerEmail,
                    Email = volunteerEmail,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(volunteerUser, volunteerPassword);
                await userManager.AddToRoleAsync(volunteerUser, "Volunteers");
            }
        }

    }
}
