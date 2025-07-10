using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoCGAPYS.Models
{
    public class Estimaciones
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(15, 2)")]
        public decimal Monto { get; set; }

        [Required]
        public DateTime FechaEstimacion { get; set; }

        // Esta propiedad es opcional (nullable), por eso el 'string?'.
        public string? Descripcion { get; set; }

        // --- Relación con la tabla Proyectos ---

        [Required]
        public string IdProyectoFk { get; set; } // La columna que guarda la llave foránea.

        // La 'propiedad de navegación' que le permite a EF Core entender la relación.
        [ForeignKey("IdProyectoFk")]
        public virtual Proyectos Proyecto { get; set; }
    }
}