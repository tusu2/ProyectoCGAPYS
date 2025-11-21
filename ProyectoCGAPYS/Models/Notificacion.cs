public class Notificacion
{
    public int Id { get; set; }
    public string UsuarioId { get; set; }
    public string Mensaje { get; set; }
    public string? Url { get; set; }
    public DateTime FechaCreacion { get; set; }
    public bool Leida { get; set; }
    public bool AccionRealizada { get; set; }
}