using System.ComponentModel.DataAnnotations;

namespace ProyectoCGAPYS.ViewModels
{
    public class AgregarCostoViewModel
    {
        [Required]
        public string IdProyectoFk { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un concepto.")]
        public string IdConceptoFk { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "El precio unitario es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a cero.")]
        [DataType(DataType.Currency)]
        public decimal PrecioUnitario { get; set; }
    }
}