using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ProyectoCGAPYS.Models
{
    public class EstimacionHistorial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EstimacionId { get; set; }

        [StringLength(50)]
        public string EstadoAnterior { get; set; }

        [Required]
        [StringLength(50)]
        public string EstadoNuevo { get; set; }

        public DateTime FechaCambio { get; set; }

        [Required]
        public string UsuarioId { get; set; } // Quién hizo el cambio [cite: 8]

        public string Comentario { get; set; } // Para los rechazos [cite: 8]

        // --- Propiedades de Navegación ---
        [ForeignKey("EstimacionId")]
        public virtual Estimaciones Estimacion { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual IdentityUser Usuario { get; set; }// Ajusta "ApplicationUser" si es necesario

        public EstimacionHistorial()
        {
            FechaCambio = DateTime.Now;
        }
    }
}
