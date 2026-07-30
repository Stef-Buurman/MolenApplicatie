using System.ComponentModel.DataAnnotations;

namespace MolenApplicatie.Server.Models
{
    public class MolenMapFilter
    {
        [Required]
        [Range(-180d, 180d)]
        public double West { get; set; }

        [Required]
        [Range(-90d, 90d)]
        public double South { get; set; }

        [Required]
        [Range(-180d, 180d)]
        public double East { get; set; }

        [Required]
        [Range(-90d, 90d)]
        public double North { get; set; }

        [Required]
        [Range(0, 19)]
        public int Zoom { get; set; }

        public string? MolenType { get; set; }
        public string? Provincie { get; set; }
        public string? MolenState { get; set; }
        public bool? HasImage { get; set; }
    }
}
