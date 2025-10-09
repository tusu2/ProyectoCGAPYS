using ProyectoCGAPYS.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ProyectoCGAPYS.Models
{
    public class Licitacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ProyectoId { get; set; } // FK a Proyecto

        [ForeignKey("ProyectoId")]
        public virtual Proyectos Proyecto { get; set; }

        [Required]
        [StringLength(100)]
        public string NumeroLicitacion { get; set; }

        [Required]
        public string Descripcion { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFinPropuestas { get; set; }

        [Required]
        [StringLength(50)]
        public string Estado { get; set; }

        // Relaciones
        public virtual ICollection<LicitacionContratista> ContratistasParticipantes { get; set; }
    }
}