using System.ComponentModel.DataAnnotations;

namespace ProyectoCGAPYS.ViewModels // Reemplaza "TuProyecto" con el namespace real de tu proyecto
{
    public class CrearLicitacionViewModel
    {
        // El ID del proyecto al que se asocia la licitación. Esencial.
        public string ProyectoId { get; set; }

        // Lo usamos para mostrar el nombre del proyecto en la vista.
        [Display(Name = "Nombre del Proyecto")]
        public string ProyectoNombre { get; set; }

        [Required(ErrorMessage = "El número de licitación es obligatorio.")]
        [Display(Name = "Número de Licitación")]
        public string NumeroLicitacion { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        [Display(Name = "Fecha de Inicio")]
        [DataType(DataType.Date)]
        public DateTime FechaInicio { get; set; }

        [Required(ErrorMessage = "La fecha límite para propuestas es obligatoria.")]
        [Display(Name = "Fecha Límite para Propuestas")]
        [DataType(DataType.Date)]
        public DateTime FechaFinPropuestas { get; set; }
    }
}