using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;
using WebBanCameraGiamSat.Models;
using WebBanCameraGiamSat.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useSqlite = builder.Configuration.GetValue<bool>("UseSqlite", false)
                || (!OperatingSystem.IsWindows() && (connectionString?.Contains("(localdb)", StringComparison.OrdinalIgnoreCase) ?? false))
                || (connectionString?.Contains(".db", StringComparison.OrdinalIgnoreCase) ?? false);

if (useSqlite)
{
    var sqliteConn = builder.Configuration.GetConnectionString("SqliteConnection")
                     ?? "Data Source=WebBanCameraGiamSat.db";
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(sqliteConn));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

// MVC + Areas
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// App services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAiChatService, AiChatService>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<IMoMoService, MoMoService>();

// Cho phep gui CSRF token qua header (dung cho cac loi goi AJAX fetch trong site.js)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

var app = builder.Build();

// Seed database (auto migrate + seed sample data on first run)
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(app.Services);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Route than thien (friendly URLs) cho trang chu tam site
app.MapControllerRoute(
    name: "product-details",
    pattern: "san-pham/{slug}",
    defaults: new { controller = "Product", action = "Details" });

app.MapControllerRoute(
    name: "product-list",
    pattern: "san-pham",
    defaults: new { controller = "Product", action = "Index" });

app.MapControllerRoute(
    name: "product-by-category",
    pattern: "danh-muc/{slug}",
    defaults: new { controller = "Product", action = "Category" });

app.MapControllerRoute(
    name: "news-details",
    pattern: "tin-tuc/{slug}",
    defaults: new { controller = "News", action = "Details" });

app.MapControllerRoute(
    name: "news-list",
    pattern: "tin-tuc",
    defaults: new { controller = "News", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
