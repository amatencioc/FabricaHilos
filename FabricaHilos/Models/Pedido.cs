using System.ComponentModel.DataAnnotations;

namespace FabricaHilos.Models;

public class Pedido
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El número de pedido es obligatorio.")]
    [StringLength(20)]
    [Display(Name = "Nº Pedido")]
    public string Numero { get; set; } = string.Empty;

    [Required(ErrorMessage = "El cliente es obligatorio.")]
    [Display(Name = "Cliente")]
    public int ClienteId { get; set; }

    public Cliente? Cliente { get; set; }

    [Required(ErrorMessage = "La fecha de pedido es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha del Pedido")]
    public DateTime FechaPedido { get; set; } = DateTime.Now;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Entrega")]
    public DateTime? FechaEntrega { get; set; }

    [Required(ErrorMessage = "El estado es obligatorio.")]
    [StringLength(30)]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = EstadoPedido.Pendiente;

    [Display(Name = "Total")]
    [DataType(DataType.Currency)]
    public decimal Total { get; set; }

    [StringLength(500)]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }

    public List<DetallePedido> Detalles { get; set; } = new();
}

public static class EstadoPedido
{
    public const string Pendiente = "Pendiente";
    public const string EnProceso = "En Proceso";
    public const string Completado = "Completado";
    public const string Cancelado = "Cancelado";

    public static IEnumerable<string> Todos =>
        new[] { Pendiente, EnProceso, Completado, Cancelado };
}
