using DZGNCatering.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();

builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(20); // 20 dakika aktif kalır
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; 
        options.LogoutPath = "/Account/Logout";
    });
builder.Services.AddAuthorization();


builder.Services.AddDbContext<AppDbContext>(options =>
{
    var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
    if (isWindows)
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
    else
    {
        var sqliteConn = builder.Configuration.GetConnectionString("SqliteConnection");
        if (string.IsNullOrEmpty(sqliteConn))
        {
            sqliteConn = "Data Source=dzgncatering.db";
        }
        options.UseSqlite(sqliteConn);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

var supportedCultures = new[] { new System.Globalization.CultureInfo("tr-TR") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("tr-TR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DZGNCatering.Data.AppDbContext>();
    // For SQLite use EnsureCreated to avoid pending-migration issues; use Migrate for SQL Server
    if (context.Database.IsSqlite())
    {
        context.Database.EnsureCreated();
    }
    else
    {
        context.Database.Migrate();
    }
    if (!context.Users.Any(u => u.Role == DZGNCatering.Models.UserRole.Admin))
    {
        
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes("123456");
        var hash = Convert.ToBase64String(sha256.ComputeHash(bytes));

        var adminUser = new DZGNCatering.Models.User
        {
            FullName = "Sistem Yöneticisi (Admin)",
            Email = "admin@dzgn.com",
            PasswordHash = hash,
            Role = DZGNCatering.Models.UserRole.Admin
        };
        context.Users.Add(adminUser);
        context.SaveChanges();
    }
}
app.Run();
