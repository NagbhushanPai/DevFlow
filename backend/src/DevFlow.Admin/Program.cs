using DevFlow.Application.Common.Authorization;
using DevFlow.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "devflow_admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Every page requires a signed-in user with the global Admin role unless marked [AllowAnonymous].
var adminOnly = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole(Roles.Admin).Build();
builder.Services.AddAuthorization(options => options.FallbackPolicy = adminOnly);
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter(adminOnly));
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
{
    var forwarded = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto };
    forwarded.KnownIPNetworks.Clear();
    forwarded.KnownProxies.Clear();
    app.UseForwardedHeaders(forwarded);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Admin/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Admin}/{action=Index}/{id?}");
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
