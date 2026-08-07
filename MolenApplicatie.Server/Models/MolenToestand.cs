namespace MolenApplicatie.Server.Models
{
    public class MolenToestand
    {
        public static readonly string Werkend = "Werkend";
        public static readonly string Restant = "Restant";
        public static readonly string Verdwenen = "Verdwenen";
        public static readonly string InAanbouw = "In aanbouw";
        public static readonly string Opgeslagen = "Opgeslagen";
        public static readonly string Gedemonteerd = "Gedemonteerd";
        public static readonly string NietWerkend = "Niet werkend";
        public static readonly string Bestaande = "Bestaande";

        public static string? From(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            return input.Trim().ToLowerInvariant() switch
            {
                "werkend" => Werkend,
                "functional" => Werkend,
                "functioneel" => Werkend,

                "restant" => Restant,
                "remains" => Restant,
                "remainder" => Restant,
                "ruin" => Restant,
                "ruins" => Restant,

                "verdwenen" => Verdwenen,
                "gone" => Verdwenen,
                "disappeared" => Verdwenen,
                "no longer exists" => Verdwenen,

                "in aanbouw" => InAanbouw,
                "inaanbouw" => InAanbouw,
                "under construction" => InAanbouw,
                "in restoration" => InAanbouw,
                "in restauratie" => InAanbouw,

                "opgeslagen" => Opgeslagen,
                "stored" => Opgeslagen,
                "in opslag" => Opgeslagen,

                "gedemonteerd" => Gedemonteerd,
                "dismantled" => Gedemonteerd,

                "niet werkend" => NietWerkend,
                "niet functioneel" => NietWerkend,
                "not functional" => NietWerkend,
                "no technique" => NietWerkend,
                "geen techniek" => NietWerkend,
                "geen techniek aanwezig" => NietWerkend,

                "bestaande" => Bestaande,
                "bestaand" => Bestaande,
                "existing" => Bestaande,
                _ => null
            };
        }

        public static IReadOnlyList<string> GetDatabaseAliases(string? input)
        {
            var toestand = From(input);

            return toestand switch
            {
                "Werkend" => ["werkend", "functional", "functioneel"],
                "Restant" => ["restant", "remains", "remainder", "ruin", "ruins"],
                "Verdwenen" => ["verdwenen", "gone", "disappeared", "no longer exists"],
                "In aanbouw" =>
                [
                    "in aanbouw",
                    "inaanbouw",
                    "under construction",
                    "in restoration",
                    "in restauratie"
                ],
                "Opgeslagen" => ["opgeslagen", "stored", "in opslag"],
                "Gedemonteerd" => ["gedemonteerd", "dismantled"],
                "Niet werkend" =>
                [
                    "niet werkend",
                    "niet functioneel",
                    "not functional",
                    "no technique",
                    "geen techniek",
                    "geen techniek aanwezig"
                ],
                "Bestaande" =>
                [
                    "bestaande",
                    "bestaand",
                    "existing",
                    "werkend",
                    "functional",
                    "functioneel",
                    "restant",
                    "remains",
                    "remainder",
                    "ruin",
                    "ruins",
                    "in aanbouw",
                    "inaanbouw",
                    "under construction",
                    "in restoration",
                    "in restauratie",
                    "niet werkend",
                    "niet functioneel",
                    "not functional",
                    "no technique",
                    "geen techniek",
                    "geen techniek aanwezig"
                ],
                _ => []
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
