using System.ComponentModel.DataAnnotations;

namespace FabricaHilos.Models;

public class DetallePedido
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Pedido")]
    public int PedidoId { get; set; }

    public Pedido? Pedido { get; set; }

    [Required(ErrorMessage = "El hilo es obligatorio.")]
    [Display(Name = "Hilo")]
    public int HiloId { get; set; }

    public Hilo? Hilo { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1.")]
    [Display(Name = "Cantidad")]
    public int Cantidad { get; set; }

    [Required]
    [Display(Name = "Precio Unitario")]
    [DataType(DataType.Currency)]
    public decimal PrecioUnitario { get; set; }

    [Display(Name = "Subtotal")]
    [DataType(DataType.Currency)]
    public decimal Subtotal => Cantidad * PrecioUnitario;
}
