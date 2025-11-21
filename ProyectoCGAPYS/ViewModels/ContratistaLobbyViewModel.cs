
namespace ProyectoCGAPYS.ViewModels
{
    public class ContratistaLobbyViewModel
    {
        public string NombreContratista { get; set; }
        public List<InvitacionViewModel> Invitaciones { get; set; }
        public List<HistorialProyectoViewModel> HistorialProyectos { get; set; }

        public ContratistaLobbyViewModel()
        {
            Invitaciones = new List<InvitacionViewModel>();
            HistorialProyectos = new List<HistorialProyectoViewModel>();
        }
        public List<Notificacion> Notificaciones { get; set; }
    }
}
