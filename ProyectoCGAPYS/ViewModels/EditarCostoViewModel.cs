using System.ComponentModel.DataAnnotations;

namespace ProyectoCGAPYS.ViewModels
{
    public class EditarCostoViewModel
    {
        [Required]
        public string Id { get; set; } // Para saber qué registro actualizar

        [Required]
        public string IdProyectoFk { get; set; } // Para saber a dónde redirigir

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "El precio unitario es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a cero.")]
        public decimal PrecioUnitario { get; set; }
    }
}