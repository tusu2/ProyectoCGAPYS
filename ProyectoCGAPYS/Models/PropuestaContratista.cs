using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ProyectoCGAPYS.Models
{
    public class PropuestaContratista
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LicitacionId { get; set; }

        [Required]
        public int ContratistaId { get; set; }

        [ForeignKey("LicitacionId, ContratistaId")]
        public virtual LicitacionContratista Participacion { get; set; }

        [Required]
        public string NombreArchivo { get; set; }

        [Required]
        public string RutaArchivo { get; set; }

        public string? Descripcion { get; set; }

        public DateTime FechaSubida { get; set; }
    }
}