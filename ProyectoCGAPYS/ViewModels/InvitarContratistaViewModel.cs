using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProyectoCGAPYS.ViewModels // Reemplaza con tu namespace
{
    public class InvitarContratistaViewModel
    {
        public int LicitacionId { get; set; }
        public string NumeroLicitacion { get; set; }
        public string ProyectoNombre { get; set; }

        // Esta será la lista de todos los contratistas que se pueden seleccionar.
        public List<ContratistaSeleccionable> ContratistasDisponibles { get; set; }

        public InvitarContratistaViewModel()
        {
            ContratistasDisponibles = new List<ContratistaSeleccionable>();
        }
    }

    // Una clase auxiliar para representar a cada contratista en la lista de selección.
    public class ContratistaSeleccionable
    {
        public int Id { get; set; }
        public string RazonSocial { get; set; }
        public string RFC { get; set; }
        // Para marcar el checkbox si ya fue invitado y deshabilitarlo.
        public bool YaEstaInvitado { get; set; }
    }
}