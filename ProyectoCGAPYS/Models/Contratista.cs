using Microsoft.AspNetCore.Identity; // Asegúrate de tener esta referencia
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ProyectoCGAPYS.Models
{
    public class Contratista
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } // FK a IdentityUser (AspNetUsers.Id)

        [ForeignKey("UsuarioId")]
        // CORRECCIÓN AQUÍ: Usamos IdentityUser en lugar de ApplicationUser
        public virtual IdentityUser Usuario { get; set; }

        [Required]
        [StringLength(255)]
        public string RazonSocial { get; set; }

        [Required]
        [StringLength(13)]
        public string RFC { get; set; }

        public string? Domicilio { get; set; }

        [Required]
        public string NombreContacto { get; set; }

        public DateTime FechaRegistro { get; set; }

        // Relaciones
        public virtual ICollection<LicitacionContratista> LicitacionesParticipadas { get; set; }
    }

}