using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity; // <-- AÑADE ESTA LÍNEA

namespace ProyectoCGAPYS.Models
{
    public class HistorialFase
    {
        [Key]
        public int Id { get; set; }

        // --- Clave foránea para el Proyecto ---
        public string ProyectoId { get; set; }
        [ForeignKey("ProyectoId")]
        public virtual Proyectos Proyecto { get; set; }

        // --- Clave foránea para el Usuario que realizó el cambio ---
        // 🔑 Guardamos el ID del usuario de Identity.
        public string? UsuarioId { get; set; }
        [ForeignKey("UsuarioId")]
        public virtual IdentityUser Usuario { get; set; } // Propiedad de navegación


        // --- Claves foráneas para las Fases (pueden ser nulas) ---
        public int? FaseAnteriorId { get; set; }
        [ForeignKey("FaseAnteriorId")]
        public virtual Fases FaseAnterior { get; set; }

        public int? FaseNuevaId { get; set; }
        [ForeignKey("FaseNuevaId")]
        public virtual Fases FaseNueva { get; set; }


        // --- Datos del Registro ---
        public string? Comentario { get; set; }

        public DateTime FechaCambio { get; set; } = DateTime.Now;

        public string TipoCambio { get; set; } // "Aprobado", "Rechazado", "Actualizado", etc.
    }
}