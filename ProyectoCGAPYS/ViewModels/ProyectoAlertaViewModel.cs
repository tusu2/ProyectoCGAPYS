using System;

namespace ProyectoCGAPYS.ViewModels
{
    public class ProyectoAlertaViewModel
    {
        public string Id { get; set; }
        public string NombreProyecto { get; set; }
        public string NombreResponsable { get; set; }
        public DateTime? FechaVencimiento { get; set; }

        // En este contexto, DiasTranscurridos representa los "Días Restantes"
        public int? DiasTranscurridos { get; set; }

        // Nueva propiedad para la alerta de conflicto SQL
        public int EsConflicto { get; set; }

        // Propiedad calculada para el color del semáforo (Solo lectura)
        public string ClaseSemaforo
        {
            get
            {
                if (!DiasTranscurridos.HasValue) return "bg-secondary"; // Gris si no hay datos

                if (DiasTranscurridos > 60) return "bg-success"; // Verde (+60 días)
                if (DiasTranscurridos > 45) return "bg-warning text-dark"; // Amarillo (46-60 días)
                return "bg-danger"; // Rojo (45 días o menos)
            }
        }
    }
}