using System.ComponentModel.DataAnnotations;

namespace FabricaHilos.Models;

public class Material
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
    [StringLength(20)]
    [Display(Name = "Unidad de Medida")]
    public string UnidadMedida { get; set; } = string.Empty;

    [Required(ErrorMessage = "El stock es obligatorio.")]
    [Range(0, double.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public decimal Stock { get; set; }

    [Required(ErrorMessage = "El costo unitario es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El costo unitario debe ser mayor a cero.")]
    [Display(Name = "Costo Unitario")]
    [DataType(DataType.Currency)]
    public decimal CostoUnitario { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;
}
