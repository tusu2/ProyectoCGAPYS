using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ProyectoCGAPYS.Models
{
    public class Proyectos_Costos
    {
        [Key]
        public string Id { get; set; }

        public int Cantidad { get; set; }

        [Column(TypeName = "decimal(15, 2)")]
        public decimal PrecioUnitario { get; set; }

        [Column(TypeName = "decimal(15, 2)")]
        public decimal ImporteTotal { get; set; }

        // --- Llaves Foráneas Obligatorias ---

        [Required(ErrorMessage = "La línea de costo debe estar asociada a un proyecto.")]
        public string IdProyectoFk { get; set; }
        [ForeignKey("IdProyectoFk")]
        public virtual Proyectos Proyecto { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un concepto para la línea de costo.")]
        public string IdConceptoFk { get; set; }
        [ForeignKey("IdConceptoFk")]
        public virtual Conceptos Concepto { get; set; }
    }
}