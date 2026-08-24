using System.Text;
using AGC.Server.Configuration;
using AGC.Server.Data;
using AGC.Server.Entities;
using AGC.Server.Hubs;
using AGC.Server.Middleware;
using AGC.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

EnvFile.Load();

var builder = WebApplication.CreateBuilder(args);

var appOptions = AppOptions.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(appOptions);
Stripe.StripeConfiguration.ApiKey = appOptions.StripeSecretKey;

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(appOptions.DatabaseConnectionString));

builder.Services.AddSingleton<PasswordHasher<User>>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<OwnerCodeAttemptLimiter>();

// Game builds/thumbnails live in Supabase Storage, not local disk — this app has no
// persistent filesystem of its own on a stateless host. The service_role key is a
// server-side secret; it's never sent to the desktop client.
builder.Services.AddHttpClient<GameFileStorage>(client =>
{
    client.BaseAddress = new Uri(appOptions.SupabaseUrl);
    client.DefaultRequestHeaders.Add("apikey", appOptions.SupabaseServiceRoleKey);
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", appOptions.SupabaseServiceRoleKey);
});

builder.Services.AddSingleton<MaintenanceState>();
builder.Services.AddHostedService<MaintenanceReopenService>();

if (string.IsNullOrEmpty(appOptions.ResendApiKey))
{
    builder.Services.AddSingleton<IEmailSender, DevConsoleEmailSender>();
}
else
{
    builder.Services.AddHttpClient<IEmailSender, ResendEmailSender>();
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(appOptions.JwtSigningKey)),
        };
        // SignalR can't set an Authorization header on WebSocket/SSE transports, so the
        // client sends the token as a query param instead — only honor that for the hub path.
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs/maintenance"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Owner", policy => policy.RequireClaim(TokenService.IsOwnerClaimType, "true"));
});

builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Behind a real deployment's reverse proxy (Koyeb, Fly, Railway, etc.), the app only
// ever sees plain HTTP from the proxy — trust its X-Forwarded-* headers so
// Request.Scheme and Request.Host (used to build the Stripe Checkout success/cancel
// URLs) correctly reflect the public https:// URL instead of an internal http:// one.
// Every proxy in front of this app is the platform's own edge, so trusting all
// forwarders here is safe.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseAuthentication();
app.UseMiddleware<MaintenanceMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MaintenanceHub>("/hubs/maintenance");

app.MapGet("/checkout/success", () => Results.Content(CheckoutPage.Render(
    "PAYMENT RECEIVED", "You're all set — head back to AGC Launcher. Your library will update in a few seconds."),
    "text/html"));
app.MapGet("/checkout/cancel", () => Results.Content(CheckoutPage.Render(
    "CHECKOUT CANCELLED", "No charge was made. You can return to AGC Launcher and try again any time."),
    "text/html"));

app.Run();

static class CheckoutPage
{
    private const string Template = """
        <!doctype html>
        <html><head><meta charset="utf-8"><title>AGC Launcher</title>
        <style>
            body { background:#05070A; color:#E8EEF3; font-family:sans-serif; display:flex;
                    align-items:center; justify-content:center; height:100vh; margin:0; }
            .card { text-align:center; max-width:420px; padding:32px; }
            h1 { font-size:20px; letter-spacing:2px; color:#2BE0FF; }
            p { color:#8592A0; line-height:1.5; }
        </style></head>
        <body><div class="card"><h1>__HEADING__</h1><p>__MESSAGE__</p></div></body></html>
        """;

    public static string Render(string heading, string message) =>
        Template.Replace("__HEADING__", heading).Replace("__MESSAGE__", message);
}
