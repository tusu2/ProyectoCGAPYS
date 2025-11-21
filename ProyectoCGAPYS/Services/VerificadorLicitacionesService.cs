using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ProyectoCGAPYS.Data; // Tu DbContext
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProyectoCGAPYS.Datos;

namespace ProyectoCGAPYS.Services
{
    public class VerificadorLicitacionesService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public VerificadorLicitacionesService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Se ejecuta cada minuto para verificar las licitaciones.
            while (!stoppingToken.IsCancellationRequested)
            {
                await VerificarLicitaciones();
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task VerificarLicitaciones()
        {
            // Creamos un "scope" para poder usar servicios como el DbContext
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Buscamos licitaciones activas cuya fecha límite ya pasó.
                var licitacionesParaCerrar = context.Licitaciones
                    .Where(l => l.Estado == "Activo" &&
                                l.FechaFinPropuestas.HasValue &&
                                l.FechaFinPropuestas.Value <= DateTime.Now)
                    .ToList();

                if (licitacionesParaCerrar.Any())
                {
                    foreach (var licitacion in licitacionesParaCerrar)
                    {
                        licitacion.Estado = "Cerrado";
                        // --- CÓDIGO AÑADIDO PARA NOTIFICAR ---
                        if (!string.IsNullOrEmpty(licitacion.UsuarioIdActivacion))
                        {
                            var notificacion = new Notificacion
                            {
                                UsuarioId = licitacion.UsuarioIdActivacion,
                                Mensaje = $"La licitación '{licitacion.NumeroLicitacion}' ha finalizado y se cerró automáticamente.",
                                Url = "/Licitaciones/Detalles/" + licitacion.Id,
                                FechaCreacion = DateTime.Now,
                                Leida = false
                            };
                            context.Notificaciones.Add(notificacion);
                        }
                    }

                    await context.SaveChangesAsync();
                }
            }
        }
    }
}