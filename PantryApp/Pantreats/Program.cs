using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pantreats.Data;
using Pantreats.Services;

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

    Console.WriteLine("using the MDF database in app_data");
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
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// add microsoft as an external login option
builder.Services.AddAuthentication()
    .AddOpenIdConnect("Microsoft", "Microsoft", options =>
    {
        var azureAdSettings = builder.Configuration.GetSection("AzureAd");

        options.Authority = azureAdSettings["Instance"] + azureAdSettings["TenantId"] + "/v2.0";
        options.ClientId = azureAdSettings["ClientId"];
        options.ClientSecret = azureAdSettings["ClientSecret"];
        options.CallbackPath = azureAdSettings["CallbackPath"];

        // use authorization code flow
        options.ResponseType = "code";

        // important: this makes microsoft work with asp.net identity external login
        options.SignInScheme = IdentityConstants.ExternalScheme;

        // basic user info from microsoft
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        options.SaveTokens = true;
    });

// add mvc views
builder.Services.AddControllersWithViews();

//the email service
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddHttpClient();

// add razor pages for identity pages
builder.Services.AddRazorPages();


var app = builder.Build();

// create roles when app starts
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    var env = services.GetRequiredService<IWebHostEnvironment>();

    // list of roles for your app
    string[] roles = { "Admin", "Donors", "Volunteers", "Students" };

    foreach (var role in roles)
    {
        // check if role already exists
        if (!await roleManager.RoleExistsAsync(role))
        {
            // create role if it doesn't exist
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    //seed logins for demo accounts -nick
    await DbSeeder.SeedAdminAsync(services);
    await DbSeeder.SeedDonorAsync(services);
    await DbSeeder.SeedStudentAsync(services);
    await DbSeeder.SeedVolunteerAsync(services);

    using (var dbScope = app.Services.CreateScope())
    {
        var dbContext = dbScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await PrepareDatabaseAsync(dbContext);
        //DbSeeder.ClearInventory(dbContext); this clears the inventory table if not commented out 
        DbSeeder.SeedInventory(dbContext, env);
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

app.MapStaticAssets();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();

static async Task PrepareDatabaseAsync(ApplicationDbContext dbContext)
{
    await dbContext.Database.MigrateAsync();
}



