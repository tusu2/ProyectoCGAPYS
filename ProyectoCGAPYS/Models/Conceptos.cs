using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ProyectoCGAPYS.Models
{
    public class Conceptos
    {
        [Key]
        public string Id { get; set; }

        [Required(ErrorMessage = "La clave del concepto es obligatoria.")]
        [StringLength(100)]
        public string Clave { get; set; }

        [Required(ErrorMessage = "La descripción del concepto es obligatoria.")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
        [StringLength(50)]
        public string Unidad { get; set; }

        // Un concepto siempre debe pertenecer a una categoría.
        // El error se mostraría en un dropdown si no se selecciona nada.
        public int IdCategoriaFk { get; set; }

        [ForeignKey("IdCategoriaFk")]
        public virtual Categorias Categoria { get; set; }
    }
}