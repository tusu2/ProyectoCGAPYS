using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Asegúrate de que este namespace coincida con el de tus otros modelos
namespace ProyectoCGAPYS.Models
{
    public class ProyectoImagen
    {
        [Key] // Indica que esta es la llave primaria
        public int Id { get; set; }

        [Required] // Indica que este campo es obligatorio
        public string IdProyectoFk { get; set; }

        [Required] // La URL de la imagen también es obligatoria
        public string ImagenUrl { get; set; }

        public string Descripcion { get; set; }

        // --- Propiedad de Navegación ---
        // Esto le dice a Entity Framework cómo se relaciona esta tabla con la de Proyectos.
        // Nos permite hacer cosas como "imagen.Proyecto.NombreProyecto" en nuestro código.
        [ForeignKey("IdProyectoFk")]
        public virtual Proyectos Proyecto { get; set; }
    }
}