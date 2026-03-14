using System.ComponentModel.DataAnnotations;

namespace FabricaHilos.Models.Produccion
{
    public class ParadaProduccion
    {
        public int Id { get; set; }

        [Required]
        public int OrdenProduccionId { get; set; }

        [Required]
        [Display(Name = "Nº Parada")]
        public int NumeroParada { get; set; }

        [Required]
        [Display(Name = "Metraje")]
        [Range(0, double.MaxValue)]
        public decimal Metraje { get; set; }

        public OrdenProduccion? OrdenProduccion { get; set; }
    }
}
