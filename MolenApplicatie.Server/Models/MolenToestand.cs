namespace MolenApplicatie.Server.Models
{
    public class MolenToestand
    {
        public static readonly string Werkend = "Werkend";
        public static readonly string Verdwenen = "Verdwenen";
        public static readonly string Restant = "Restant";
        public static readonly string InAanbouw = "In aanbouw";
        public static readonly string Bestaande = "Bestaande";

        public static string? From(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }
            return input.ToLowerInvariant() switch
            {
                "werkend" => Werkend,
                "verdwenen" => Verdwenen,
                "restant" => Restant,
                "in aanbouw" => InAanbouw,
                "inaanbouw" => InAanbouw,
                "bestaande" => Bestaande,
                _ => null
            };
        }

        public static bool Equals(string? input, string? other)
        {
            input = From(input);
            other = From(other);
            if (input == null && other == null) return true;
            if (input == null || other == null) return false;
            return string.Equals(input, other, StringComparison.OrdinalIgnoreCase);
        }
    }

}
