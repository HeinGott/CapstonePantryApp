using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pantreats.Data;

var builder = WebApplication.CreateBuilder(args);

var appDataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");

if (!Directory.Exists(appDataPath))
{
    Directory.CreateDirectory(appDataPath);
}

AppDomain.CurrentDomain.SetData("DataDirectory", appDataPath);

// mdf path
var mdfPath = Path.Combine(appDataPath, "Pantreats.mdf");

// get both connection strings
var localMdfConnection = builder.Configuration.GetConnectionString("LocalMdf");
var localDbDatabaseConnection = builder.Configuration.GetConnectionString("LocalDbDatabase");

string connectionString;

if (File.Exists(mdfPath))
{
    connectionString = localMdfConnection
        ?? throw new InvalidOperationException("connection string 'LocalMdf' not found.");

    Console.WriteLine("using app_data mdf database");
}
else
{
    connectionString = localDbDatabaseConnection
        ?? throw new InvalidOperationException("connection string 'LocalDbDatabase' not found.");

    Console.WriteLine("using localdb named database");
}

// add services to the container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// turn on identity and roles
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient();

var app = builder.Build();

// create roles when app starts
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // list of roles for your app
    string[] roles = { "Admin", "Vendors", "Volunteers", "Students" };

    foreach (var role in roles)
    {
        // check if role already exists
        if (!await roleManager.RoleExistsAsync(role))
        {
            // create role if it doesn't exist
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

// configure the http request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();