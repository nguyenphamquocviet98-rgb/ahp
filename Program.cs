using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using code.Data;

// Enforce invariant culture for numbers and dates
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options => options.Filters.Add<code.Services.DatabaseConflictFilter>());
builder.Services.AddScoped<code.Services.PasswordService>();
builder.Services.AddHttpContextAccessor();

// Register DbContext with SQL Server connection string
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register AHP Service
builder.Services.AddScoped<code.Services.AHPService>();
builder.Services.AddScoped<code.Services.AHPB2BService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<code.Services.AiAdvisorService>();

// Register Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            int.TryParse(context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id);
            var user = await db.NguoiDungs.AsNoTracking().Include(u => u.DoanhNghiep).FirstOrDefaultAsync(u => u.MaNguoiDung == id);
            if (user?.TrangThai != true || (user.MaDoanhNghiep.HasValue && user.DoanhNghiep?.TrangThai != true) ||
                context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value != user.VaiTro ||
                context.Principal?.FindFirst("MaDoanhNghiep")?.Value != (user.MaDoanhNghiep ?? 0).ToString())
                context.RejectPrincipal();
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Tự động nạp dữ liệu mẫu nếu chưa có
try
{
    if (app.Environment.IsDevelopment())
        await DataSeeder.SeedDataAsync(app.Services);
}
catch (Exception ex)
{
    Console.WriteLine($"[DataSeeder Warning] Không thể nạp dữ liệu mẫu tự động: {ex.Message}");
}

app.Run();
