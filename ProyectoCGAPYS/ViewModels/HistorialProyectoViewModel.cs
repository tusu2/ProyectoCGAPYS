namespace ProyectoCGAPYS.ViewModels
{
    public class HistorialProyectoViewModel
    {
        public string ProyectoId { get; set; }
        public string NombreProyecto { get; set; }
        public string Folio { get; set; }
        public decimal PresupuestoEjercido { get; set; } // O el dato que consideres relevante
        public DateTime? FechaFinalizacion { get; set; }
    }
}
