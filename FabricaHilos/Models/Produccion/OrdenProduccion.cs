using System.ComponentModel.DataAnnotations;
using FabricaHilos.Models.RecursosHumanos;

namespace FabricaHilos.Models.Produccion
{
    public enum EstadoOrden
    {
        Pendiente,
        EnProceso,
        Completada,
        Cancelada
    }

    public class OrdenProduccion
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Número de Orden")]
        [StringLength(20)]
        public string NumeroOrden { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Fecha de Inicio")]
        [DataType(DataType.Date)]
        public DateTime FechaInicio { get; set; }

        [Display(Name = "Fecha Fin Estimada")]
        [DataType(DataType.Date)]
        public DateTime? FechaFinEstimada { get; set; }

        [Required]
        [Display(Name = "Tipo de Hilo")]
        [StringLength(100)]
        public string TipoHilo { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Cantidad Planificada")]
        [Range(0, double.MaxValue)]
        public decimal CantidadPlanificada { get; set; }

        [Display(Name = "Cantidad Producida")]
        [Range(0, double.MaxValue)]
        public decimal CantidadProducida { get; set; }

        [Display(Name = "Estado")]
        public EstadoOrden Estado { get; set; } = EstadoOrden.Pendiente;

        [Display(Name = "Responsable")]
        public int? EmpleadoId { get; set; }
        public Empleado? Empleado { get; set; }

        [Display(Name = "Observaciones")]
        [StringLength(500)]
        public string? Observaciones { get; set; }
    }
}
