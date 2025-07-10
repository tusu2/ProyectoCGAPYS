using System.ComponentModel.DataAnnotations;

namespace ProyectoCGAPYS.Models
{
    public class TiposProyecto
    {
        [Key]
        public string Id { get; set; }

        [Required(ErrorMessage = "El nombre del campus es obligatorio.")]

        [StringLength(255)]
        public string Nombre { get; set; }
    }
}