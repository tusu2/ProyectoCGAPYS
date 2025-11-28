// En: ViewModels/CrearProyectoViewModel.cs

using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ProyectoCGAPYS.ViewModels
{
    public class CrearProyectoViewModel
    {
        // --- Propiedades para los campos del formulario ---
        [Required(ErrorMessage = "El nombre del proyecto es obligatorio.")]
        public string NombreProyecto { get; set; }

        // --- CAMBIO 1: Nombre de propiedad ajustado para mayor claridad ---
        [Required(ErrorMessage = "Debe seleccionar una dependencia.")]
        public string IdDependenciaFk { get; set; }

    
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "La fecha de solicitud es obligatoria.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? FechaSolicitud { get; set; }


        [Required(ErrorMessage = "La fecha de finalización es obligatoria.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]

        public DateTime? FechaFinalizacionAprox { get; set; } 

        // --- CAMBIO 2: Nombre de propiedad ajustado ---
       
        public string? IdTipoFondoFk { get; set; }

        //[Required(ErrorMessage = "El nombre del responsable es obligatorio.")]
        public string? NombreResponsable { get; set; }

        //[Required(ErrorMessage = "El correo es obligatorio.")]
       // [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
        public string? Correo { get; set; }


       // [Required(ErrorMessage = "El celular es obligatorio.")]
       // [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "El celular debe tener 10 dígitos.")]
        public string? Celular { get; set; }


        [Required(ErrorMessage = "Debe asignar un usuario responsable.")]
        public string UsuarioResponsableId { get; set; }

        // 3. Lista para llenar el Select (Dropdown)
        public List<SelectListItem> UsuariosOptions { get; set; } = new List<SelectListItem>();
        [Required(ErrorMessage = "La latitud es obligatoria.")]
        public string Latitud { get; set; }

        [Required(ErrorMessage = "La longitud es obligatoria.")]
        public string Longitud { get; set; }

        // --- CAMBIO 3: Nombre de propiedad ajustado ---
        [Required(ErrorMessage = "Debe seleccionar un tipo de proyecto.")]
        public string IdTipoProyectoFk { get; set; }

        public string? Folio { get; set; }
        public IFormFile? AnteproyectoFile { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un Campus.")]
        public int IdCampusFk { get; set; }

        // --- Propiedades para llenar los menús desplegables (ESTO ESTÁ PERFECTO) ---
        public List<SelectListItem> CampusOptions { get; set; } = new List<SelectListItem>(); 
        public List<SelectListItem> DependenciaOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> TipoFondoOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> TipoProyectoOptions { get; set; } = new List<SelectListItem>();
    }
}