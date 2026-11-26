using Gcmac.Registration.Data;
using Gcmac.Registration.Models;
using Gcmac.Registration.Options;
using Gcmac.Registration.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.Configure<MemberApiOptions>(builder.Configuration.GetSection(MemberApiOptions.SectionName));
builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection(PaymentOptions.SectionName));
builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection(NotificationOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("SqlServer");
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(string.IsNullOrWhiteSpace(connectionString)
        ? "Server=(localdb)\\mssqllocaldb;Database=GcmacRegistration;Trusted_Connection=True;TrustServerCertificate=True;"
        : connectionString));

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IMemberDirectory, MemberDirectory>(client =>
{
    var settings = builder.Configuration.GetSection(MemberApiOptions.SectionName).Get<MemberApiOptions>() ?? new();
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(settings.TimeoutSeconds, 2, 60));
});

var paymentProvider = builder.Configuration[$"{PaymentOptions.SectionName}:Provider"] ?? "PNB";
var usePnb = paymentProvider.Equals("PNB", StringComparison.OrdinalIgnoreCase);
if (usePnb)
{
    var pnb = builder.Configuration.GetSection($"{PaymentOptions.SectionName}:Pnb").Get<PnbPaymentOptions>() ?? new();
    if (string.IsNullOrWhiteSpace(pnb.RequestUrl) ||
        string.IsNullOrWhiteSpace(pnb.VerifyUrl) ||
        string.IsNullOrWhiteSpace(pnb.MerchantCode) ||
        string.IsNullOrWhiteSpace(pnb.Key) ||
        string.IsNullOrWhiteSpace(pnb.Iv))
    {
        Console.Error.WriteLine("PNB payment gateway is selected but not configured. Set Payment__Pnb__RequestUrl, Payment__Pnb__VerifyUrl, Payment__Pnb__MerchantCode, Payment__Pnb__Key and Payment__Pnb__Iv.");
    }

    builder.Services.AddSingleton<IPaymentGateway, PnbPaymentGateway>();
}
else
{
    builder.Services.AddSingleton<IPaymentGateway, DemoPaymentGateway>();
}

builder.Services.AddSingleton<RegistrationStore>();
builder.Services.AddSingleton<FeeCatalog>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddScoped<RegistrationService>();
builder.Services.AddScoped<IMemberAccountService, MemberAccountService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<INotificationSender, HttpEmailSender>();
builder.Services.AddSingleton<INotificationSender, HttpSmsSender>();
builder.Services.AddHostedService<NotificationWorker>();

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(connectionString))
{
    await using var scope = app.Services.CreateAsyncScope();
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
