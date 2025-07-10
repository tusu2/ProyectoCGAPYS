using System.ComponentModel.DataAnnotations;

namespace ProyectoCGAPYS.Models
{
    public class Fases
    {
        [Key] // Define Id como la clave primaria.
        public int Id { get; set; }

        [Required] // Corresponde a NOT NULL.
        [StringLength(255)] // Corresponde a nvarchar(255).
        public string Nombre { get; set; }
        public virtual ICollection<Proyectos> Proyectos { get; set; }

    }
}