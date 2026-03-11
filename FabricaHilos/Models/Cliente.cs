using System.ComponentModel.DataAnnotations;

namespace FabricaHilos.Models;

public class Cliente
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "Formato de email inválido.")]
    [StringLength(200)]
    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    [Phone(ErrorMessage = "Formato de teléfono inválido.")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [StringLength(300)]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    [Display(Name = "Fecha de Registro")]
    [DataType(DataType.Date)]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    [Display(Name = "Nombre Completo")]
    public string NombreCompleto => $"{Nombre} {Apellido}";
}
