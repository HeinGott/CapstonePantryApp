using DocumentFormat.OpenXml.InkML;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
    options.UseSqlServer(connectionString)
        // This app does some schema preparation outside normal EF migrations,
        // so we don't want startup to fail solely because the runtime model
        // doesn't exactly match the last snapshot.
        .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// turn on identity and roles
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
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
builder.Services.AddScoped<CheckoutService>();

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
    string[] roles = { "Admin", "Donors", "Volunteers", "Students", "Kiosk" };

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
    await DbSeeder.SeedKioskAsync(services);

    using (var dbScope = app.Services.CreateScope())
    {
        var dbContext = dbScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await PrepareDatabaseAsync(dbContext);
        //DbSeeder.ClearInventory(dbContext); this clears the inventory table if not commented out 
        DbSeeder.SeedInventory(dbContext, env);

        if (app.Environment.IsDevelopment())
        {
            ImageOptimizerService.OptimizeItemImages(env);
        }

        DbSeeder.SeedInventoryImages(dbContext, env);
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
    await EnsureStudentApplicationWorkflowSchemaAsync(dbContext);
    await EnsureVolunteerSchedulingSchemaAsync(dbContext);

    try
    {
        await dbContext.Database.MigrateAsync();
        await EnsureStudentApplicationWorkflowSchemaAsync(dbContext);
        await EnsureVolunteerSchedulingSchemaAsync(dbContext);
    }
    catch (SqlException ex) when (ex.Message.Contains("AspNetRoles", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Skipping duplicate identity schema migration because the local database already contains ASP.NET Identity tables.");
    }
}

static async Task EnsureStudentApplicationWorkflowSchemaAsync(ApplicationDbContext dbContext)
{
    var connectionString = dbContext.Database.GetConnectionString();
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return;
    }

    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    if (!await TableExistsAsync(connection, "UserApplications"))
    {
        return;
    }

    if (!await ColumnExistsAsync(connection, "UserApplications", "ApplicationStatus"))
    {
        await ExecuteNonQueryAsync(connection, "ALTER TABLE [UserApplications] ADD [ApplicationStatus] nvarchar(32) NULL;");
    }

    if (!await ColumnExistsAsync(connection, "UserApplications", "ReviewedAt"))
    {
        await ExecuteNonQueryAsync(connection, "ALTER TABLE [UserApplications] ADD [ReviewedAt] datetime2 NULL;");
    }

    if (!await ColumnExistsAsync(connection, "UserApplications", "ReviewedByUserId"))
    {
        await ExecuteNonQueryAsync(connection, "ALTER TABLE [UserApplications] ADD [ReviewedByUserId] nvarchar(450) NULL;");
    }

    if (!await ColumnExistsAsync(connection, "UserApplications", "ReviewNotes"))
    {
        await ExecuteNonQueryAsync(connection, "ALTER TABLE [UserApplications] ADD [ReviewNotes] nvarchar(max) NULL;");
    }

    await ExecuteNonQueryAsync(connection, """
        UPDATE [UserApplications]
        SET [ApplicationStatus] = CASE WHEN [IsActive] = 1 THEN 'Approved' ELSE 'Pending' END
        WHERE [ApplicationStatus] IS NULL OR LTRIM(RTRIM([ApplicationStatus])) = '';
        """);
}

static async Task EnsureVolunteerSchedulingSchemaAsync(ApplicationDbContext dbContext)
{
    var connectionString = dbContext.Database.GetConnectionString();
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return;
    }

    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    if (!await TableExistsAsync(connection, "VolunteerShifts"))
    {
        await ExecuteNonQueryAsync(connection, """
            CREATE TABLE [VolunteerShifts] (
                [VolunteerShiftId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [UserId] nvarchar(450) NOT NULL,
                [ShiftDate] date NOT NULL,
                [StartTime] time NOT NULL,
                [EndTime] time NOT NULL,
                [Status] nvarchar(32) NOT NULL CONSTRAINT [DF_VolunteerShifts_Status] DEFAULT N'Scheduled',
                [Notes] nvarchar(max) NULL,
                [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_VolunteerShifts_CreatedAt] DEFAULT SYSUTCDATETIME(),
                [UpdatedAt] datetime2 NOT NULL CONSTRAINT [DF_VolunteerShifts_UpdatedAt] DEFAULT SYSUTCDATETIME(),
                [CreatedByUserId] nvarchar(450) NULL,
                [UpdatedByUserId] nvarchar(450) NULL
            );
            """);
    }

    await EnsureIndexExistsAsync(connection, "VolunteerShifts", "IX_VolunteerShifts_ShiftDate_StartTime", """
        CREATE INDEX [IX_VolunteerShifts_ShiftDate_StartTime]
        ON [VolunteerShifts] ([ShiftDate], [StartTime]);
        """);

    await EnsureIndexExistsAsync(connection, "VolunteerShifts", "IX_VolunteerShifts_UserId_ShiftDate", """
        CREATE INDEX [IX_VolunteerShifts_UserId_ShiftDate]
        ON [VolunteerShifts] ([UserId], [ShiftDate]);
        """);
}

static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName)
{
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName;";
    command.Parameters.AddWithValue("@tableName", tableName);
    return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
}

static async Task<bool> ColumnExistsAsync(SqlConnection connection, string tableName, string columnName)
{
    await using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT COUNT(*)
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName;
        """;
    command.Parameters.AddWithValue("@tableName", tableName);
    command.Parameters.AddWithValue("@columnName", columnName);
    return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
}

static async Task EnsureIndexExistsAsync(SqlConnection connection, string tableName, string indexName, string createSql)
{
    await using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT COUNT(*)
        FROM sys.indexes
        WHERE name = @indexName
          AND object_id = OBJECT_ID(@tableName);
        """;
    command.Parameters.AddWithValue("@indexName", indexName);
    command.Parameters.AddWithValue("@tableName", tableName);

    if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
    {
        await ExecuteNonQueryAsync(connection, createSql);
    }
}

static async Task ExecuteNonQueryAsync(SqlConnection connection, string sql)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    await command.ExecuteNonQueryAsync();
}
