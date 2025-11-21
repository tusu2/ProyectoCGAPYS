using ProyectoCGAPYS.Models;
using System.ComponentModel.DataAnnotations;

namespace ProyectoCGAPYS.ViewModels
{
  
    public class DetallesLicitacionViewModel
    {
        public List<DocumentoViewModel> Documentos { get; set; }
        public int LicitacionId { get; set; }
        public string NumeroLicitacion { get; set; }
        public string NombreProyecto { get; set; }
        public string DescripcionProyecto { get; set; }
        public DateTime FechaFinPropuestas { get; set; }

        // --- NUEVOS DATOS AÑADIDOS ---
        public string Latitud { get; set; }
        public string Longitud { get; set; }
        public List<DocumentoViewModel> DocumentosProyecto { get; set; }
        // ------------------------------------

        public List<PropuestaViewModel> PropuestasSubidas { get; set; }

        public PropuestaInputModel PropuestaInput { get; set; }
        public string EstadoParticipacion { get; set; }

        // --- DATOS PARA EL FORMULARIO DE SUBIDA (se mantienen igual) ---
        [Required(ErrorMessage = "Debes seleccionar un archivo.")]
        public IFormFile ArchivoPropuesta { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(500)]
        public string DescripcionPropuesta { get; set; }
        public string ProyectoId { get; set; }

        // Para el formulario de "Crear Nueva Estimación"
        public EstimacionCrearViewModel NuevaEstimacion { get; set; }

        // Para mostrar la lista de estimaciones ya enviadas
        public List<Estimaciones> EstimacionesAnteriores { get; set; }
        public Dictionary<string, List<Estimaciones>> EstimacionesAgrupadas { get; set; }
        public DetallesLicitacionViewModel()
        {
            PropuestasSubidas = new List<PropuestaViewModel>();
            DocumentosProyecto = new List<DocumentoViewModel>(); // Inicializamos la nueva lista
        }
    }
}
