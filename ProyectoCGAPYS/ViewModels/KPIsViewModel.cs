using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ProyectoCGAPYS.ViewModels
{
    public class KPIsViewModel
    {
        public int ProyectosTotales { get; set; } // [cite: 5]
        public int ProyectosActivos { get; set; } // [cite: 6]
        public decimal PresupuestoTotalAutorizado { get; set; } // [cite: 7]
        public decimal MontoTotalEjercido { get; set; } // [cite: 8]
        public decimal BalanceGeneralDisponible { get; set; } // [cite: 9]
      
        public decimal PresupuestoContratado { get; set; }
        
    }
}
