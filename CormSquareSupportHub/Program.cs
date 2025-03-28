using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupportHub.DataAccess.Data;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.DataAccess.Repository;
using SupportHub.Models;
using SupportHub.Utility.EmailServices;
using Microsoft.AspNetCore.Identity.UI.Services;
using SupportHub.Utility;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 7295;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax; // Use Lax unless cross-site is required
});


builder.Services.Configure<AttachmentSettings>(builder.Configuration.GetSection("AttachmentSettings"));
builder.Services.AddIdentity<ExternalUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
// Update the existing ConfigureApplicationCookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax; // Change from None to Lax
    options.Cookie.HttpOnly = true;
});


builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddSingleton<EmailService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseHsts();  // Ensure HSTS is enabled
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ExternalUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    await SeedRolesAsync(roleManager);
    await SeedAdminUserAsync(userManager, roleManager);
}

app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages();
    endpoints.MapControllerRoute(
        name: "Public",
        pattern: "{area=Public}/{controller=Home}/{action=Index}/{id?}");
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

async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
{
    string[] roleNames = { "Internal User", "KM Creator", "KM Champion", "ExternalUser" };

    foreach (var role in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

async Task SeedAdminUserAsync(UserManager<ExternalUser> userManager, RoleManager<IdentityRole> roleManager)
{
    string adminEmail = "admin123@cormsquare.com";
    string adminPassword = "AdminPassword123!";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        var user = new ExternalUser
        {
            Email = adminEmail,
            UserName = adminEmail,
            NormalizedEmail = adminEmail.ToUpper(),
            NormalizedUserName = adminEmail.ToUpper(),
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "User",
            PhoneNumber = "1234567890",
            CompanyName = "CormSquare",
            EmployeeID = "ADM001",
            Country = "India",
            IsApproved = true
        };

var result = await userManager.CreateAsync(user, adminPassword);
if (result.Succeeded)
{
    await userManager.AddToRoleAsync(user, "Admin");
}
    }
}

public class AttachmentSettings
{
    public string UploadPath { get; set; }
}