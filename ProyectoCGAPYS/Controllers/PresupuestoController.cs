using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Data;
using ProyectoCGAPYS.Datos;
using ProyectoCGAPYS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoCGAPYS.Controllers
{
    public class PresupuestoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PresupuestoController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Debes traer TiposFondo
            var fondos = await _context.TiposFondo.OrderBy(f => f.Nombre).ToListAsync();
            return View(fondos);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarAsignacion(List<TiposFondo> fondos)
        {
            // 1. SOLUCIÓN CLAVE:
            // Removemos la validación del campo "Id" y "Nombre" del ModelState.
            // ¿Por qué? Porque los nuevos items vienen con Id nulo (y eso causaba el error).
            // Nosotros generamos el ID manualmente abajo, así que no es necesario validarlo aquí.
            foreach (var key in ModelState.Keys)
            {
                if (key.Contains(".Id"))
                {
                    ModelState.Remove(key);
                }
            }

            // Filtramos filas vacías por si acaso se envió basura
            var fondosValidos = fondos.Where(f => !string.IsNullOrWhiteSpace(f.Nombre) || f.Monto > 0).ToList();

            if (ModelState.IsValid)
            {
                try
                {
                    foreach (var fondo in fondosValidos)
                    {
                        // CASO 1: Actualizar Existente
                        if (!string.IsNullOrEmpty(fondo.Id))
                        {
                            var fondoExistente = await _context.TiposFondo.FindAsync(fondo.Id);
                            if (fondoExistente != null)
                            {
                                fondoExistente.Monto = fondo.Monto;
                                // Si deseas permitir cambiar nombres, descomenta esto:
                                // fondoExistente.Nombre = fondo.Nombre; 
                                _context.Update(fondoExistente);
                            }
                        }
                        // CASO 2: Crear Nuevo
                        else
                        {
                            fondo.Id = Guid.NewGuid().ToString(); // Generamos el ID aquí
                            _context.Add(fondo);
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["Mensaje"] = "Presupuesto guardado correctamente en la Base de Datos.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Si hay error de SQL, lo mostramos en pantalla para saber qué pasó
                    ModelState.AddModelError("", "Error al guardar en BD: " + ex.Message);
                }
            }
            else
            {
                // Esto te servirá para depurar: Si no guarda, te dirá por qué arriba del formulario
                var errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Mensaje"] = "Error de validación: " + string.Join(", ", errores);
            }

            // Si falló, regresamos la lista para no perder lo que escribiste
            return View("Index", fondos);
        }
    }
}