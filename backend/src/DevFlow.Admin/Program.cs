using DevFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllersWithViews();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Admin/Error");
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=Index}/{id?}");
app.Run();
