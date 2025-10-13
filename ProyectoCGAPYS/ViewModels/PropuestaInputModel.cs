using System.ComponentModel.DataAnnotations;

namespace ProyectoCGAPYS.ViewModels
{
    public class PropuestaInputModel
    {
        // ID de la licitación a la que pertenece la propuesta.
        // Lo necesitaremos para saber dónde asociar la propuesta.
        public int LicitacionId { get; set; }

        [Required(ErrorMessage = "La descripción no puede estar vacía.")]
        [Display(Name = "Descripción o Anotaciones")]
        public string DescripcionPropuesta { get; set; }

        [Required(ErrorMessage = "Debes seleccionar un archivo.")]
        [Display(Name = "Seleccionar Archivo")]
        public IFormFile ArchivoPropuesta { get; set; }
    }
}
