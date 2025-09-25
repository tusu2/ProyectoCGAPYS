// En Utilities/DbSeeder.cs

using Microsoft.AspNetCore.Identity;
using ProyectoCGAPYS.Models; // Asegúrate de que apunte a tus modelos

public static class DbSeeder
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

        // --- 1. CREACIÓN DE ROLES ---
        string[] roleNames = { "Jefa", "Empleado1", "Empleado2", "Empleado3" };
        foreach (var roleName in roleNames)
        {
            // Revisa si el rol ya existe
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                // Si no existe, lo crea
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // --- 2. CREACIÓN DE UN USUARIO "JEFA" POR DEFECTO ---
        // Revisa si el usuario administrador ya existe
        var user = await userManager.FindByEmailAsync("jefa@uadec.mx");

        if (user == null)
        {
            // Si no existe, lo crea
            user = new IdentityUser()
            {
                UserName = "jefa@uadec.mx",
                Email = "jefa@uadec.mx",
            };
            // Usa una contraseña segura. El segundo parámetro es la contraseña.
            await userManager.CreateAsync(user, "Password123!");

            // Le asigna el rol de "Jefa"
            await userManager.AddToRoleAsync(user, "Jefa");
        }

        var empleado1 = await userManager.FindByEmailAsync("empleado1@uadec.mx");

        // Si el usuario existe y AÚN NO tiene el rol de "Empleado1"...
        if (empleado1 != null && !(await userManager.IsInRoleAsync(empleado1, "Empleado1")))
        {
            // ...se lo asignamos.
            await userManager.AddToRoleAsync(empleado1, "Empleado1");
        }
        var empleado2 = await userManager.FindByEmailAsync("empleado2@uadec.mx");

        // Si el usuario existe y AÚN NO tiene el rol de "Empleado2"...
        if (empleado2 != null && !(await userManager.IsInRoleAsync(empleado2, "Empleado2")))
        {
            // ...se lo asignamos.
            await userManager.AddToRoleAsync(empleado2, "Empleado2");
        }
        var empleado3= await userManager.FindByEmailAsync("empleado3@uadec.mx");

        // Si el usuario existe y AÚN NO tiene el rol de "Empleado2"...
        if (empleado3 != null && !(await userManager.IsInRoleAsync(empleado3, "Empleado3")))
        {
            // ...se lo asignamos.
            await userManager.AddToRoleAsync(empleado3, "Empleado3");
        }
    }
}