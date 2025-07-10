using System.ComponentModel.DataAnnotations;
namespace ProyectoCGAPYS.Models
{
    public class Dependencias
    {
        [Key]
        public string Id { get; set; }

        [Required(ErrorMessage = "El nombre de la dependencia es obligatorio.")]
        [StringLength(255)]
        public string Nombre { get; set; }
    }
}