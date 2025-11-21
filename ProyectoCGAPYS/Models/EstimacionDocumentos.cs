
using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ProyectoCGAPYS.Models
{
    public class EstimacionDocumentos
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EstimacionId { get; set; }

        [Required]
        [StringLength(100)]
        public string TipoDocumento { get; set; } // "NumerosGeneradores", "ReporteFotografico", "Factura", etc. [cite: 4]

        [Required]
        public string NombreArchivo { get; set; }

        [Required]
        public string RutaArchivo { get; set; }

        public DateTime FechaSubida { get; set; }

        [Required]
        public string UsuarioId { get; set; }

        // --- Propiedades de Navegación ---
        // Le dice a EF que este documento pertenece a UNA estimación
        [ForeignKey("EstimacionId")]
        public virtual Estimaciones Estimacion { get; set; }

        // Le dice a EF que este documento fue subido por UN usuario
        // (Asegúrate que "ApplicationUser" sea el nombre de tu clase de usuario de Identity)
        // Si no la has modificado, podría ser "AspNetUsers", aunque usualmente se mapea a una clase.
        [ForeignKey("UsuarioId")]
        public virtual IdentityUser Usuario { get; set; }// Ajusta "ApplicationUser" si tu clase se llama diferente

        public EstimacionDocumentos()
        {
            FechaSubida = DateTime.Now;
        }
    }
}
