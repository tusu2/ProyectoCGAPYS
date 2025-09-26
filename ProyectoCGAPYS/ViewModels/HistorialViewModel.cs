// En un archivo llamado HistorialViewModel.cs dentro de la carpeta Models

public class HistorialViewModel
{
    public DateTime FechaCambio { get; set; }
    public string TipoCambio { get; set; }
    public string? Comentario { get; set; }
    public string NombreUsuario { get; set; } // <-- ¡Aquí está la propiedad que la vista necesita!
}