// --- Models/LicitacionDocumento.cs ---

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoCGAPYS.Models
{
    public class LicitacionDocumento
    {
        [Key]
        public int Id { get; set; }

        public int LicitacionId { get; set; }

        [Required]
        public string TipoDocumento { get; set; }

        [Required]
        public string NombreArchivo { get; set; }

        [Required]
        public string RutaArchivo { get; set; }

        public DateTime FechaSubida { get; set; }

        // --- Relaciones ---
        [ForeignKey("LicitacionId")]
        public virtual Licitacion Licitacion { get; set; }
    }
}