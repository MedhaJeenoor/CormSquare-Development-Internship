using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupportHub.DataAccess.Data;
using SupportHub.DataAccess.Repository.IRepository;
using SupportHub.DataAccess.Repository;
using SupportHub.Models;
using SupportHub.Utility.EmailServices;
using SupportHub.Utility;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ExternalUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    await SeedAdminUser(userManager, roleManager);
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

async Task SeedAdminUser(UserManager<ExternalUser> userManager, RoleManager<IdentityRole> roleManager)
{
    string adminEmail = "admin123@cormsquare.com";
    string adminPassword = "AdminPassword123!";

    Console.WriteLine("Starting admin user seeding...");

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        var roleResult = await roleManager.CreateAsync(new IdentityRole("Admin"));
        Console.WriteLine($"Role 'Admin' creation: {(roleResult.Succeeded ? "Succeeded" : "Failed")}");
    }

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        Console.WriteLine($"No user found with email: {adminEmail}. Creating new user...");
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

        Console.WriteLine($"User object before creation: Email='{user.Email}', UserName='{user.UserName}', NormalizedEmail='{user.NormalizedEmail}'");
        try
        {
            var result = await userManager.CreateAsync(user, adminPassword);
            if (result.Succeeded)
            {
                Console.WriteLine("Admin user created successfully!");
                await userManager.AddToRoleAsync(user, "Admin");
                Console.WriteLine("Admin role assigned successfully!");
            }
            else
            {
                var errorMessage = "Error creating admin user: " + string.Join(", ", result.Errors.Select(e => e.Description));
                Console.WriteLine(errorMessage);
                throw new Exception(errorMessage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during user creation: {ex.Message}");
            throw;
        }
    }
    else
    {
        Console.WriteLine($"User with email {adminEmail} already exists. Skipping creation.");
    }
}