using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity; // Para el UsuarioIdActivacion


namespace ProyectoCGAPYS.Models
{
    public class Licitacion
    {
        // --- Constructor para inicializar colecciones ---
        public Licitacion()
        {
            ContratistasParticipantes = new HashSet<LicitacionContratista>();
            LicitacionDocumentos = new HashSet<LicitacionDocumento>();
        }

        // --- CAMPOS EXISTENTES ---
        [Key]
        public int Id { get; set; }

        [Required]
        public string ProyectoId { get; set; }

        [Required]
        [StringLength(100)]
        public string NumeroLicitacion { get; set; }

        [Required]
        public string Descripcion { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        public DateTime? FechaFinPropuestas { get; set; }

        [Required]
        [StringLength(50)]
        public string Estado { get; set; }

        public string? UsuarioIdActivacion { get; set; }

        // --- CAMPOS NUEVOS (AÑADIDOS EN PASO 2) ---

        [Required]
        [StringLength(50)]
        public string TipoProceso { get; set; } // Ej: "Adjudicación Directa" o "Licitación Pública"

        public int? ContratistaGanadorId { get; set; } // FK al ganador

        public DateTime? FechaFallo { get; set; } // Fecha del fallo/adjudicación

        [StringLength(100)]
        public string? NumeroContrato { get; set; } // Número de contrato (Modo Control)


        // --- PROPIEDADES DE NAVEGACIÓN ---

        // (Existente) Proyecto al que pertenece
        [ForeignKey("ProyectoId")]
        public virtual Proyectos Proyecto { get; set; }

        // (Existente) Usuario que activó (si aplica)
        [ForeignKey("UsuarioIdActivacion")]
        public virtual IdentityUser UsuarioActivacion { get; set; }

        // (Existente) Lista de participantes (Modo Gestión)
        public virtual ICollection<LicitacionContratista> ContratistasParticipantes { get; set; }

        // (Nueva) El contratista ganador (Modo Control)
        [ForeignKey("ContratistaGanadorId")]
        public virtual Contratista ContratistaGanador { get; set; }

        // (Nueva) Los documentos de la licitación (Fallo, Contrato, Fianza)
        public virtual ICollection<LicitacionDocumento> LicitacionDocumentos { get; set; }

        public DateTime? FechaInicioEjecucion { get; set; }
        public DateTime? FechaFinEjecucion { get; set; }

        public string? SupervisorAsignadoId { get; set; }

        [ForeignKey("SupervisorAsignadoId")]
        public virtual IdentityUser SupervisorAsignado { get; set; }
    }
}