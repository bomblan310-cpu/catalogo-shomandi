using Microsoft.EntityFrameworkCore;
using MobileCatalogAPI.Data;
using MobileCatalogAPI.Services;
using Npgsql;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var databaseConnection = NormalizeConnectionString(builder.Configuration.GetConnectionString("CatalogDatabase"));
var useLocalDatabase = string.IsNullOrWhiteSpace(databaseConnection) || databaseConnection.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase);

builder.Services.AddDbContext<MobileCatalogDbContext>(options =>
{
	if (useLocalDatabase)
		options.UseSqlite("Data Source=mobilecatalog.local.db");
	else
		options.UseNpgsql(databaseConnection);
});
builder.Services.AddControllers();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "shomandi.admin.v2";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = false;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("admin-login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddSingleton<WhatsAppLinkService>();
builder.Services.AddScoped<AdminApiKeyFilter>();
builder.Services.AddSingleton<CloudinaryImageService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var database = scope.ServiceProvider.GetRequiredService<MobileCatalogDbContext>();
	await CatalogSeeder.InitializeAsync(database);
}

app.UseDefaultFiles();
app.UseAuthentication();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    var isAdminPage = string.Equals(context.Request.Path.Value, "/admin.html", StringComparison.OrdinalIgnoreCase);
    if (isAdminPage || context.Request.Path.StartsWithSegments("/api/admin"))
    {
        context.Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
        context.Response.Headers.Pragma = "no-cache";
    }
    // Opening or refreshing the panel always requires a fresh login.
    if (isAdminPage)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        context.Response.Cookies.Delete("shomandi.admin");
    }
    await next();
});
if (app.Environment.IsDevelopment())
	app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
	{
		OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "no-cache, no-store"
	});
else
	app.UseStaticFiles();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/ping", () => Results.Ok("pong"));
app.MapControllers();

app.Run();

static string? NormalizeConnectionString(string? connectionString)
{
	if (string.IsNullOrWhiteSpace(connectionString) ||
		(!connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) &&
		 !connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)))
		return connectionString;

	var uri = new Uri(connectionString);
	var credentials = uri.UserInfo.Split(':', 2);
	var npgsql = new NpgsqlConnectionStringBuilder
	{
		Host = uri.Host,
		Port = uri.Port > 0 ? uri.Port : 5432,
		Database = uri.AbsolutePath.Trim('/'),
		Username = Uri.UnescapeDataString(credentials[0]),
		Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty,
		SslMode = uri.Query.Contains("sslmode=require", StringComparison.OrdinalIgnoreCase)
			? SslMode.Require : SslMode.Prefer
	};
	return npgsql.ConnectionString;
}
