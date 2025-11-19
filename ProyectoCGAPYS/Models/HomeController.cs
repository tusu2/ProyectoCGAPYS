// Archivo: Models/ProyectoHistorialBloqueo.cs
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoCGAPYS.Models
{
    public class ProyectoHistorialBloqueo
    {
        [Key]
        public int Id { get; set; }
        public DateTime FechaEvento { get; set; } = DateTime.Now;
        [Required]
        public string Accion { get; set; }
        public string Comentario { get; set; }

        // --- Relaciones ---
        [Required]
        public string ProyectoId { get; set; }
        [ForeignKey("ProyectoId")]
        public virtual Proyectos Proyecto { get; set; }

        [Required]
        public string UsuarioId { get; set; }
        [ForeignKey("UsuarioId")]
        public virtual IdentityUser Usuario { get; set; }
    }
}