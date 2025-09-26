using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ProyectoCGAPYS.Models
{
    public class Proyectos
    {

        public Proyectos()
        {
            // Inicializamos la lista para que nunca sea nula
            CostosDelProyecto = new HashSet<Proyectos_Costos>();
        }
        [Key]
        public string Id { get; set; }

        [Required(ErrorMessage = "El nombre del proyecto es obligatorio.")]
        [StringLength(255)]
        public string NombreProyecto { get; set; }

        [Required(ErrorMessage = "La descripción del proyecto es obligatoria.")]
        public string Descripcion { get; set; }

        public DateTime? FechaSolicitud { get; set; }

        public DateTime? FechaFinalizacionAprox { get; set; }

        [Column(TypeName = "decimal(15, 2)")]
        public decimal Presupuesto { get; set; }

        [Required(ErrorMessage = "El estatus del proyecto es obligatorio.")]
        [StringLength(100)]
        public string Estatus { get; set; }

        [Required(ErrorMessage = "El nombre del responsable es obligatorio.")]
        [StringLength(255)]
        public string NombreResponsable { get; set; }

        [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
        [StringLength(255)]
        public string Correo { get; set; }

        [EmailAddress(ErrorMessage = "El celular del responsable es obligatorio")]
        [StringLength(20)]
        public string Celular { get; set; } 


        [StringLength(255)]
        public string? NombreAnteproyecto { get; set; }


        [Required(ErrorMessage = "La latitud es necesaria")]
        [StringLength(50)]
        public string Latitud { get; set; }

        [Required(ErrorMessage = "La longitud es necesaria")]
        [StringLength(50)]
        public string Longitud { get; set; }

        [StringLength(50)]
        public string Folio { get; set; }

        // --- Llaves Foráneas Obligatorias ---

        // El error para estos campos se valida sobre la llave foránea (Id)
        // y generalmente se muestra en un campo <select> o dropdown en el HTML.

        public int? IdFaseFk { get; set; }
        [ForeignKey("IdFaseFk")]
        public virtual Fases Fase { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un campus.")]
        public int IdCampusFk { get; set; }
        [ForeignKey("IdCampusFk")]
        public virtual Campus Campus { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una dependencia.")]
        public string IdDependenciaFk { get; set; }
        [ForeignKey("IdDependenciaFk")]
        public virtual Dependencias Dependencia { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un tipo de fondo.")]
        public string IdTipoFondoFk { get; set; }
        [ForeignKey("IdTipoFondoFk")]
        public virtual TiposFondo TipoFondo { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un tipo de proyecto.")]
        public string IdTipoProyectoFk { get; set; }
        [ForeignKey("IdTipoProyectoFk")]
        public virtual TiposProyecto TipoProyecto { get; set; }

        public virtual ICollection<Proyectos_Costos> CostosDelProyecto { get; set; }
        public virtual ICollection<ProyectoImagen> Imagenes { get; set; }

        [StringLength(10)]
        public string? Prioridad { get; set; }

        public virtual ICollection<DocumentosProyecto> Documentos { get; set; }
    }
}