using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoCGAPYS.Models
{
    public class Estimaciones
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(15, 2)")]
        public decimal Monto { get; set; }

        [Required]
        public DateTime FechaEstimacion { get; set; }

        // Esta propiedad es opcional (nullable), por eso el 'string?'.
        public string? Descripcion { get; set; }

        // --- Relación con la tabla Proyectos ---
        // --- ¡NUEVO CAMPO! ---
        [Required(ErrorMessage = "El estado de la estimación es obligatorio.")]
        [StringLength(50)]
        public string Estado { get; set; } // Posibles valores: "Pendiente", "Pagada", "Rechazada"

        public bool EsFiniquito { get; set; }
        public bool EsAnticipo { get; set; }
        public DateTime? FechaPago { get; set; }
        [Required]
        public string IdProyectoFk { get; set; } // La columna que guarda la llave foránea.

        // La 'propiedad de navegación' que le permite a EF Core entender la relación.
        [ForeignKey("IdProyectoFk")]
        public virtual Proyectos Proyecto { get; set; }

        public virtual ICollection<EstimacionDocumentos> Documentos { get; set; }

        // Una estimación puede tener MUCHOS cambios de estado
        public virtual ICollection<EstimacionHistorial> Historial { get; set; }

      
        // --- CONSTRUCTOR ---
        // (Agrega o modifica tu constructor para inicializar las listas)
        public Estimaciones()
        {
            Documentos = new HashSet<EstimacionDocumentos>();
            Historial = new HashSet<EstimacionHistorial>();
            // No borres otras inicializaciones que ya tengas
        }
    }
}