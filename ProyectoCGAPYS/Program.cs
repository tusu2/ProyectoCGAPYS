using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Datos;
using System.Runtime.InteropServices;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Identity;
using ProyectoCGAPYS.Data;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
QuestPDF.Settings.License = LicenseType.Community;
//Configuracion la conexion a sql ser local dbMSSQLLLOCAL

builder.Services.AddDbContext<ApplicationDbContext>(opciones =>
               opciones.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSql")));


builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>() // <-- �Esta l�nea activa la gesti�n de Roles!
    .AddEntityFrameworkStores<ApplicationDbContext>();
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
           .RequireAuthenticatedUser()
           .RequireRole("Jefa")
           .Build();
});

builder.Services.AddHostedService<ProyectoCGAPYS.Services.VerificadorLicitacionesService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=PanelDeFases}/{action=Index}/{id?}");
app.MapRazorPages();
// En Program.cs, justo antes de app.Run()

//=========== C�DIGO PARA SEMBRAR LA BASE DE DATOS ===========
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Llama a nuestro m�todo de inicializaci�n
        await DbSeeder.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurri� un error al sembrar la base de datos.");
    }
}
//==============================================================

app.Run();