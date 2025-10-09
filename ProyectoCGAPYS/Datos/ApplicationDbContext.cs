using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Models;
using ProyectoCGAPYS.ViewModels; // <-- ¡ESTA ES LA LÍNEA QUE SOLUCIONA EL ERROR!

namespace ProyectoCGAPYS.Datos
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        // los modelos de la base de datos
        public DbSet<Campus> Campus { get; set; }
        public DbSet<Dependencias> Dependencias { get; set; }
        public DbSet<TiposFondo> TiposFondo { get; set; }
        public DbSet<TiposProyecto> TiposProyecto { get; set; }
        public DbSet<Categorias> Categorias { get; set; }
        public DbSet<Proyectos> Proyectos { get; set; }
        public DbSet<Conceptos> Conceptos { get; set; }
        public DbSet<Proyectos_Costos> Proyectos_Costos { get; set; }
        public DbSet<Fases> Fases { get; set; }
        public DbSet<Estimaciones> Estimaciones { get; set; }
        public DbSet<ProyectoSimpleViewModel> ProyectosSimples { get; set; }
        public DbSet<ProyectoImagen> ProyectoImagenes { get; set; }
        // Los ViewModels que vienen de los Stored Procedures
        public DbSet<KPIsViewModel> KPIsViewModels { get; set; }
        public DbSet<FondoViewModel> FondoViewModels { get; set; }
        public DbSet<FaseViewModel> FaseViewModels { get; set; }
        public DbSet<ProyectoAlertaViewModel> ProyectoAlertaViewModels { get; set; }
        public DbSet<HistorialFase> HistorialFases { get; set; }
        public DbSet<DocumentosProyecto> DocumentosProyectos { get; set; }

        public DbSet<Contratista> Contratistas { get; set; }
        public DbSet<Licitacion> Licitaciones { get; set; }
        public DbSet<LicitacionContratista> LicitacionContratistas { get; set; }
        public DbSet<PropuestaContratista> PropuestasContratistas { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ... otro código de OnModelCreating ...

            // Configuramos los ViewModels como entidades sin clave (Keyless)
            modelBuilder.Entity<KPIsViewModel>().HasNoKey();
            modelBuilder.Entity<FondoViewModel>().HasNoKey();
            modelBuilder.Entity<FaseViewModel>().HasNoKey();
            modelBuilder.Entity<ProyectoAlertaViewModel>().HasNoKey();
            modelBuilder.Entity<ProyectoSimpleViewModel>().HasNoKey();

        }
    }
}