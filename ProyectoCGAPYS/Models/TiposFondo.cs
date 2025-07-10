using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoCGAPYS.Models
{
    public class TiposFondo
    {
        [Key]
        public string Id { get; set; }

        [Required(ErrorMessage = "El nombre del campus es obligatorio.")]
        [StringLength(255)]
        public string Nombre { get; set; }


        [Required(ErrorMessage = "Monto requerido")]
        [Column(TypeName = "decimal(15, 2)")]
        public decimal Monto { get; set; }
    }
}