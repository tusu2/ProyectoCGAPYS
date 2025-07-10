using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Datos;
using ProyectoCGAPYS.ViewModels;
using System.Threading.Tasks;

namespace TuProyecto.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Inyectamos el contexto de la base de datos
        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Creamos una instancia del ViewModel principal que enviaremos a la vista
            var dashboardViewModel = new DashboardViewModel();

            // 1. Obtener KPIs Principales
            // Usamos 'FromSqlRaw' para llamar al procedimiento. Asumimos el año fiscal 2025.
            var kpis = await _context.Set<KPIsViewModel>()
                                     .FromSqlRaw("EXEC sp_GetDashboard_KPIsPrincipales @AnioFiscal = {0}", 2025)
                                     .ToListAsync();
            dashboardViewModel.KPIs = kpis.FirstOrDefault();

            // 2. Obtener Estado por Fondo
            dashboardViewModel.EstadoPorFondo = await _context.Set<FondoViewModel>()
                                                              .FromSqlRaw("EXEC sp_GetDashboard_EstadoPorFondo")
                                                              .ToListAsync();

            // 3. Obtener Proyectos por Fase
            dashboardViewModel.ProyectosPorFase = await _context.Set<FaseViewModel>()
                                                                .FromSqlRaw("EXEC sp_GetDashboard_ProyectosPorFase")
                                                                .ToListAsync();

            // 4. Obtener Proyectos por Vencer
            dashboardViewModel.ProyectosPorVencer = await _context.Set<ProyectoAlertaViewModel>()
                                                                  .FromSqlRaw("EXEC sp_GetDashboard_ProyectosPorVencer")
                                                                  .ToListAsync();

            // 5. Obtener Estimaciones Pendientes
            dashboardViewModel.EstimacionesPendientes = await _context.Set<ProyectoAlertaViewModel>()
                                                                       .FromSqlRaw("EXEC sp_GetDashboard_EstimacionesPendientes")
                                                                       .ToListAsync();

            // Pasamos el ViewModel completamente poblado a la vista
            return View(dashboardViewModel);
        }
    }
}