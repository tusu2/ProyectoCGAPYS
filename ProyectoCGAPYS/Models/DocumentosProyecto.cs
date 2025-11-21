using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoCGAPYS.Models
{
    public class DocumentosProyecto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string NombreArchivo { get; set; }

        [Required]
        public string RutaArchivo { get; set; }

        public string? Descripcion { get; set; }

        public DateTime FechaSubida { get; set; } = DateTime.Now;

        // Clave foránea para el Proyecto
        public string ProyectoId { get; set; }
        [ForeignKey("ProyectoId")]
        public virtual Proyectos Proyecto { get; set; }
    }
}