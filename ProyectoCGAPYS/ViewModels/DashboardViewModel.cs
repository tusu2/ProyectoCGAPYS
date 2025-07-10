namespace ProyectoCGAPYS.ViewModels
{
    public class DashboardViewModel
    {
        public KPIsViewModel KPIs { get; set; }
        public List<FondoViewModel> EstadoPorFondo { get; set; }
        public List<FaseViewModel> ProyectosPorFase { get; set; }
        public List<ProyectoAlertaViewModel> ProyectosPorVencer { get; set; }
        public List<ProyectoAlertaViewModel> EstimacionesPendientes { get; set; }

        public DashboardViewModel()
        {
            // Inicializamos para evitar errores de referencia nula en la vista
            KPIs = new KPIsViewModel();
            EstadoPorFondo = new List<FondoViewModel>();
            ProyectosPorFase = new List<FaseViewModel>();
            ProyectosPorVencer = new List<ProyectoAlertaViewModel>();
            EstimacionesPendientes = new List<ProyectoAlertaViewModel>();
        }
    }
}