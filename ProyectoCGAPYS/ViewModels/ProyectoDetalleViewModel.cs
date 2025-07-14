using ProyectoCGAPYS.Models;
using System.Collections.Generic;

namespace ProyectoCGAPYS.ViewModels
{
    public class ProyectoDetalleViewModel
    {
        // --- Datos Principales ---
        public Proyectos Proyecto { get; set; } // El proyecto en sí

        // --- Pestaña 2: Control Financiero ---
        public List<Estimaciones> Estimaciones { get; set; }
        public decimal MontoContratado { get; set; } // Lo calcularemos en el controlador
        public decimal TotalEjercido { get; set; }
        public decimal SaldoRestante { get; set; }
        public decimal Anticipo { get; set; }
        public bool AnticipoPagado { get; set; } // Un check simple

        // --- Pestaña 3: Documentos ---
        // (Necesitaremos un modelo 'Documentos' que ahora imagino así)
        public List<Documento> Documentos { get; set; }

        // --- Pestaña 4: Bitácora ---
        // (Igual, imaginemos un modelo 'BitacoraEntrada')
        public List<BitacoraEntrada> EntradasBitacora { get; set; }

        // Para saber qué pestaña mostrar activa
        public string TabActiva { get; set; } = "resumen";
        public List<Proyectos_Costos> CostosDelProyecto { get; set; }
    }

    // --- Modelos Auxiliares que necesitaremos crear ---

    // Modelo para Pestaña 3 (Ejemplo)
    public class Documento
    {
        public int Id { get; set; }
        public string NombreArchivo { get; set; }
        public string UrlArchivo { get; set; }
        public string Categoria { get; set; }
        public DateTime FechaCarga { get; set; }
        public string SubidoPor { get; set; }
        public string IdProyectoFk { get; set; }
    }

    // Modelo para Pestaña 4 (Ejemplo)
    public class BitacoraEntrada
    {
        public int Id { get; set; }
        public string TipoEntrada { get; set; } // "Comentario", "Fotos", "Sistema"
        public string Texto { get; set; }
        public List<string> UrlsFotos { get; set; }
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; }
        public string IdProyectoFk { get; set; }

    }
}