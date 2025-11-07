using Microsoft.AspNetCore.Mvc.Rendering;
using ProyectoCGAPYS.Models;


namespace ProyectoCGAPYS.ViewModels
{
    public class ContratistaEstimacionesViewModel
    {
        // 1. Para el "Panel Kanban" de sus estimaciones
        public Dictionary<string, List<Estimaciones>> EstimacionesAgrupadas { get; set; }

        // 2. Para el formulario de "Crear Nueva"
        public EstimacionCrearViewModel NuevaEstimacion { get; set; }

        // 3. Para el nuevo <select> (dropdown) del formulario
        public SelectList ProyectosEnEjecucion { get; set; }
    }
}
