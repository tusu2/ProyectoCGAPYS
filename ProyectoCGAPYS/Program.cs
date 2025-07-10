using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Datos;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);
//Configuracion la conexion a sql ser local dbMSSQLLLOCAL

builder.Services.AddDbContext<ApplicationDbContext>(opciones =>
               opciones.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSql")));


// Add services to the container.
builder.Services.AddControllersWithViews();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
