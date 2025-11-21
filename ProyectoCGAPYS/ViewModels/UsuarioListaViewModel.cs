namespace ProyectoCGAPYS.ViewModels
{
    public class UsuarioListaViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; }
        public string Telefono { get; set; }
        public bool EstaBloqueado { get; set; } // Para saber si lo pintamos rojo o verde
    }
}