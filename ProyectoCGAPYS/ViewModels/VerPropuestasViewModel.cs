namespace ProyectoCGAPYS.ViewModels
{
    public class VerPropuestasViewModel
    {
        public int LicitacionId { get; set; }
        public string NumeroLicitacion { get; set; }
        public string ProyectoNombre { get; set; }
        public List<ContratistaConPropuestasViewModel> Contratistas { get; set; }
        public string EstadoLicitacion { get; set; } 
    }
}
