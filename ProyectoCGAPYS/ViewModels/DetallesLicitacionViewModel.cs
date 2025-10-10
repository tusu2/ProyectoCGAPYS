using System.ComponentModel.DataAnnotations;

namespace ProyectoCGAPYS.ViewModels
{
    public class DetallesLicitacionViewModel
    {
        // --- Datos para mostrar ---
        public int LicitacionId { get; set; }
        public string NumeroLicitacion { get; set; }
        public string NombreProyecto { get; set; }
        public string DescripcionProyecto { get; set; }
        public DateTime FechaFinPropuestas { get; set; }
        public List<PropuestaViewModel> PropuestasSubidas { get; set; }

        // --- Datos para el formulario de subida ---
        [Required(ErrorMessage = "Debes seleccionar un archivo.")]
        public IFormFile ArchivoPropuesta { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(500)]
        public string DescripcionPropuesta { get; set; }

        public DetallesLicitacionViewModel()
        {
            PropuestasSubidas = new List<PropuestaViewModel>();
        }
    }
}
