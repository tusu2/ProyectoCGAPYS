using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

// Este ViewModel es un "molde" para tu formulario de creación.
public class EstimacionCrearViewModel
{
    // --- Datos de la Estimación ---

    // El ID del proyecto. Es [Required] y está bien.
    [Required(ErrorMessage = "Debe seleccionar un proyecto.")]
    [Display(Name = "Proyecto")]
    public string IdProyectoFk { get; set; }

    [Required(ErrorMessage = "El monto es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser positivo.")]
    [Display(Name = "Monto de la Estimación")]
    public decimal Monto { get; set; }

    [Display(Name = "Fecha de la Estimación")]
    [DataType(DataType.Date)]
    public DateTime FechaEstimacion { get; set; }

    [Display(Name = "Descripción (Conceptos)")]
    [DataType(DataType.MultilineText)]
    public string Descripcion { get; set; }

    // --- Lista para el DropDown ---
    // ¡¡AQUÍ!! Esta propiedad NO LLEVA [Required]
    public SelectList ProyectosAsignados { get; set; }

    // --- Archivos Requeridos ---

    [Display(Name = "Archivos Adjuntos")]
    public List<IFormFile> Archivos { get; set; } = new List<IFormFile>();

    [Display(Name = "Etiqueta del Archivo")]
    public List<string> Etiquetas { get; set; } = new List<string>();

    public bool EsFiniquito { get; set; }
    public bool EsAnticipo { get; set; }

    public EstimacionCrearViewModel()
    {
        FechaEstimacion = DateTime.Today;
    }
}