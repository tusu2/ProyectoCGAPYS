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
        public string TipoProceso { get; set; }

        // Para los nuevos campos de control
        public DateTime? FechaFallo { get; set; }
        public string NumeroContrato { get; set; }

        // Para mostrar al ganador único
        public ContratistaViewModel ContratistaGanador { get; set; }

        // Para la nueva sección de documentos (Fallo, Contrato, Fianza)
        public List<LicitacionDocumentoViewModel> LicitacionDocumentos { get; set; }
        public DateTime? FechaInicioEjecucion { get; set; }
        public DateTime? FechaFinEjecucion { get; set; }
        public string SupervisorAsignadoID { get; set; }

        // --- PROPIEDADES EXISTENTES (Se mantienen para el Modo Gestión) ---

        // Sigue existiendo para el flujo de "Licitación Pública"





        public LicitacionDetalleViewModel()
        {
            Participantes = new List<ParticipanteViewModel>();
            Documentos = new List<DocumentoViewModel>();
         
            LicitacionDocumentos = new List<LicitacionDocumentoViewModel>();
        }
    }
    public class SubidaMasivaViewModel
    {
        public int LicitacionId { get; set; }
        public List<IFormFile> Archivos { get; set; }
        public List<string> TiposDocumento { get; set; }
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
    public class ContratistaViewModel
    {
        public int Id { get; set; }
        public string RazonSocial { get; set; }
        public string RFC { get; set; }
    }

    // --- CLASE NUEVA ---
    // Para la nueva lista de documentos de licitación
    public class LicitacionDocumentoViewModel
    {
        public int Id { get; set; }
        public string TipoDocumento { get; set; }
        public string NombreArchivo { get; set; }
        public string RutaArchivo { get; set; }
        public DateTime FechaSubida { get; set; }
    }
}