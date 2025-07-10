using System.ComponentModel.DataAnnotations;

namespace ProyectoCGAPYS.Models
{
    public class Categorias
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Nombre { get; set; }
    }
}