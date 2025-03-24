using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupportHub.DataAccess.Data;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.DataAccess.Repository;
using SupportHub.Utility;
using Microsoft.AspNetCore.Identity.UI.Services;
using SupportHub.Models;
using SupportHub.Utility.EmailServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ExternalUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();  // Required for email confirmation, password reset, etc.

builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.RequireUniqueEmail = true; // Ensure email is unique

    // Extend token lifespan
    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultProvider;

    // Extend token expiry time (Default is 1 day, here we set it to 3 days)
    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
});

// Override the default token lifespan (e.g., set password reset token expiry to 3 days)
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromDays(3); // Extend token expiry to 3 days
});


builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddSingleton<EmailService>();



var app = builder.Build();

// Configure middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Seed the admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ExternalUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    await SeedAdminUser(userManager, roleManager);
}

// Configure endpoints
app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages(); // Required for Razor Pages to work

    endpoints.MapControllerRoute(
        name: "Public",
        pattern: "{area=Public}/{controller=Home}/{action=Index}/{id?}");

    // Redirect unauthenticated users to Login page
    endpoints.MapGet("/", async context =>
    {
        if (!context.User.Identity.IsAuthenticated)
        {
            context.Response.Redirect("/Identity/Account/Login");
        }
        else
        {
            context.Response.Redirect("/Public/Home/Index");
        }
    });
});

app.Run();


// ======================== Seed Admin User ========================
async Task SeedAdminUser(UserManager<ExternalUser> userManager, RoleManager<IdentityRole> roleManager)
{
    string adminEmail = "admin123@cormsquare.com";
    string adminPassword = "Admin@CormSquare123456";

    // Ensure Admin Role Exists
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // Check if the admin user already exists
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        var user = new ExternalUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true, // Skip email confirmation
            FirstName = "Admin",
            LastName = "User",
            PhoneNumber = "1234567890",
            CompanyName = "CormSquare",
            EmployeeID = "ADM001",
            Country = "India"
        };

        var result = await userManager.CreateAsync(user, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Admin"); // Assign role
            Console.WriteLine("Admin user created successfully!");
        }
        else
        {
            Console.WriteLine("Error creating admin user:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine(error.Description);
            }
        }
    }
}