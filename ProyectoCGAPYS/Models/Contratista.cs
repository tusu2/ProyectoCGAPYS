using Microsoft.AspNetCore.Identity; // Asegúrate de tener esta referencia
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ProyectoCGAPYS.Models
{
    public class Contratista
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La Razón Social es obligatoria")]
        [Display(Name = "Razón Social / Nombre")]
        public string RazonSocial { get; set; }


        // Relaciones
        public virtual ICollection<LicitacionContratista> LicitacionesParticipadas { get; set; }
    }

}