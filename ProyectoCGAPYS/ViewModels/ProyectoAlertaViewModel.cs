namespace ProyectoCGAPYS.ViewModels
{
    public class ProyectoAlertaViewModel
    {
        public string Id { get; set; } // Para poder crear el enlace al proyecto
        public string NombreProyecto { get; set; }
        public string NombreResponsable { get; set; }
        public DateTime? FechaVencimiento { get; set; } // [cite: 22]
        public int? DiasTranscurridos { get; set; } // [cite: 25]
    }
}