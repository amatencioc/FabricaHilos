using System.ComponentModel.DataAnnotations;

namespace FabricaHilos.Models;

public class Hilo
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(20, ErrorMessage = "El código no puede superar los 20 caracteres.")]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "El color es obligatorio.")]
    [StringLength(50)]
    public string Color { get; set; } = string.Empty;

    [Required(ErrorMessage = "El gramaje por rollo es obligatorio.")]
    [Range(1, 100000, ErrorMessage = "El gramaje debe estar entre 1 y 100000 gramos.")]
    [Display(Name = "Gramaje por Rollo (g)")]
    public decimal GramajePorRollo { get; set; }

    [Required(ErrorMessage = "El stock es obligatorio.")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    [Display(Name = "Stock (rollos)")]
    public int Stock { get; set; }

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a cero.")]
    [Display(Name = "Precio Unitario")]
    [DataType(DataType.Currency)]
    public decimal Precio { get; set; }

    [Display(Name = "Fecha de Creación")]
    [DataType(DataType.Date)]
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;
}
