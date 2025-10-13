// LicitacionDetalleViewModel.cs

using System;
using System.Collections.Generic;

namespace ProyectoCGAPYS.ViewModels
{
    public class LicitacionDetalleViewModel
    {
        public int LicitacionId { get; set; }
        public string ProyectoId { get; set; }
        public string NumeroLicitacion { get; set; }
        public string ProyectoNombre { get; set; }
        public string DescripcionLicitacion { get; set; }
        public string Estado { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFinPropuestas { get; set; }

        // CAMPOS NUEVOS: Coordenadas para el mapa
        public string Latitud { get; set; }
        public string Longitud { get; set; }

        public List<ParticipanteViewModel> Participantes { get; set; }
        public List<DocumentoViewModel> Documentos { get; set; }
        public string FolioProyecto { get; set; }
        public string CampusNombre { get; set; }
        public string DependenciaNombre { get; set; }
        public string TipoFondoNombre { get; set; }
        public decimal PresupuestoProyecto { get; set; }

        public LicitacionDetalleViewModel()
        {
            Participantes = new List<ParticipanteViewModel>();
            Documentos = new List<DocumentoViewModel>(); // Asegúrate de inicializar esta lista también
        }
    }

    // Sub-modelo para representar a cada contratista en la lista.
    public class ParticipanteViewModel
    {
        public int ContratistaId { get; set; }
        public string RazonSocial { get; set; }
        public string RFC { get; set; }
        public string EstadoParticipacion { get; set; }
        public DateTime? FechaInvitacion { get; set; }
    }
}