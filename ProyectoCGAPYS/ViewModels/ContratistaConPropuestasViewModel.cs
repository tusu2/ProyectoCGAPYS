namespace ProyectoCGAPYS.ViewModels
{
    public class ContratistaConPropuestasViewModel
    {
        public string RazonSocial { get; set; }
        public string RFC { get; set; }
        public List<PropuestaResumenViewModel> Propuestas { get; set; }
        public int ContratistaId { get; set; }

    }
}
