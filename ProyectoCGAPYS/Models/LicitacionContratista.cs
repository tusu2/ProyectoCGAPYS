using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
namespace ProyectoCGAPYS.Models
{
    [PrimaryKey(nameof(LicitacionId), nameof(ContratistaId))] // Clave primaria compuesta
    public class LicitacionContratista
    {
        public int LicitacionId { get; set; }

        [ForeignKey("LicitacionId")]
        public virtual Licitacion Licitacion { get; set; }

        public int ContratistaId { get; set; }

        [ForeignKey("ContratistaId")]
        public virtual Contratista Contratista { get; set; }

        public DateTime FechaInvitacion { get; set; }

        [Required]
        [StringLength(50)]
        public string EstadoParticipacion { get; set; }
    }
}