using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Models;

namespace MolenApplicatie.Server.Services
{
    public class PlaceTypeService
    {
        public PlaceType? GetPlaceType(GeoName geoName)
        {
            if (geoName.ToponymName.ToLower().Contains("provincie "))
            {
                return new PlaceType
                {
                    Name = "Provincie",
                    NameEn = "Province",
                    NameMV = "Provincies",
                    Group = "Provincies"
                };
            }
            else if (geoName.ToponymName.ToLower().Contains("gemeente "))
            {
                return new PlaceType
                {
                    Name = "Gemeente",
                    NameEn = "Local authority",
                    NameMV = "Gemeentes",
                    Group = "Gemeentes"
                };
            }
            else if (geoName.FclName.ToLower().Equals("stream, lake, ..."))
            {
                if (geoName.FcodeName.ToLower().Contains("lake"))
                {
                    return new PlaceType
                    {
                        Name = "Meer",
                        NameEn = "Lake",
                        NameMV = "Meren",
                        Group = "Waterlichamen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Contains("stream"))
                {
                    return new PlaceType
                    {
                        Name = "Beek",
                        NameEn = "Stream",
                        NameMV = "Beken",
                        Group = "Waterlichamen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("marine channel"))
                {
                    return new PlaceType
                    {
                        Name = "Zeekanaal",
                        NameEn = "Marine channel",
                        NameMV = "Zeekanalen",
                        Group = "Waterlichamen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Contains("canal") || geoName.FcodeName.ToLower().Equals("navigation channel") || geoName.FcodeName.ToLower().Contains("channel"))
                {
                    return new PlaceType
                    {
                        Name = "Kanaal",
                        NameEn = "Canal",
                        NameMV = "Kanalen",
                        Group = "Waterlichamen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("marsh(es)") || geoName.FcodeName.ToLower().Equals("tidal flat(s)"))
                {
                    return new PlaceType
                    {
                        Name = "Moeras",
                        NameEn = "Marsch",
                        NameMV = "Moerassen",
                        Group = "Waterlichamen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Contains("cove") || geoName.FcodeName.ToLower().Contains("bay"))
                {
                    return new PlaceType
                    {
                        Name = "baai",
                        NameEn = "Bay",
                        NameMV = "baaien",
                        Group = "Waterlichamen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Contains("pond"))
                {
                    return new PlaceType
                    {
                        Name = "Vijver",
                        NameEn = "Pond",
                        NameMV = "Vijvers",
                        Group = "Waterlichamen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Contains("shoal"))
                {
                    return new PlaceType
                    {
                        Name = "Ondiepte",
                        NameEn = "Shoal",
                        NameMV = "Ondieptes",
                        Group = "Waterlichamen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Contains("harbor") || geoName.FcodeName.ToLower().Contains("dock"))
                {
                    return new PlaceType
                    {
                        Name = "Haven",
                        NameEn = "Harbor",
                        NameMV = "Havens",
                        Group = "Waterlichamen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("estuary"))
                {
                    return new PlaceType
                    {
                        Name = "Zeearm",
                        NameEn = "Sstuary",
                        NameMV = "Zeearmen",
                        Group = "Waterlichamen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("bank(s)"))
                {
                    return new PlaceType
                    {
                        Name = "Oever",
                        NameEn = "Shore",
                        NameMV = "Oevers",
                        Group = "Waterlichamen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("sea") || geoName.FcodeName.ToLower().Equals("seas"))
                {
                    return new PlaceType
                    {
                        Name = "Zee",
                        NameEn = "Sea",
                        NameMV = "Zeeën",
                        Group = "Waterlichamen"
                    };
                }
            }
            else if (geoName.FclName.ToLower().Equals("city, village,..."))
            {
                if (geoName.FcodeName.ToLower().Equals("populated locality"))
                {
                    return new PlaceType
                    {
                        Name = "Bevolkte Plaats",
                        NameEn = "Populated Locality",
                        NameMV = "Bevolkte Plaatsen",
                        Group = "Steden en Dorpen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("section of populated place"))
                {
                    return new PlaceType
                    {
                        Name = "Wijk",
                        NameEn = "District",
                        NameMV = "Wijken",
                        Group = "Steden en Dorpen"
                    };
                }
                else
                {
                    return new PlaceType
                    {
                        Name = "Stad, Dorp, Regio",
                        NameEn = "City, Village, Region",
                        NameMV = "Steden, Dorpen, Regio's",
                        Group = "Steden en Dorpen"
                    };
                }
            }
            else if (geoName.FclName.ToLower().Equals("road, railroad "))
            {
                if (geoName.FcodeName.ToLower().Equals("road"))
                {
                    return new PlaceType
                    {
                        Name = "Weg",
                        NameEn = "Road",
                        NameMV = "Wegen",
                        Group = "Wegen en Spoorwegen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Contains("tunnel"))
                {
                    return new PlaceType
                    {
                        Name = "Tunnel",
                        NameEn = "Tunnel",
                        NameMV = "Tunnels",
                        Group = "Wegen en Spoorwegen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("street"))
                {
                    return new PlaceType
                    {
                        Name = "Straat",
                        NameEn = "Street",
                        NameMV = "Straten",
                        Group = "Wegen en Spoorwegen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Contains("railroad"))
                {
                    return new PlaceType
                    {
                        Name = "Spoorlijn",
                        NameEn = "Railroad",
                        NameMV = "Spoorlijnen",
                        Group = "Wegen en Spoorwegen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("trail"))
                {
                    return new PlaceType
                    {
                        Name = "Pad",
                        NameEn = "Trail",
                        NameMV = "Paden",
                        Group = "Wegen en Spoorwegen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("road junction"))
                {
                    return new PlaceType
                    {
                        Name = "Kruispunt",
                        NameEn = "Road junction",
                        NameMV = "Kruispunten",
                        Group = "Wegen en Spoorwegen"
                    };
                }
            }
            else if (geoName.FclName.ToLower().Equals("mountain,hill,rock,... "))
            {
                if (geoName.FcodeName.ToLower().Contains("hill"))
                {
                    return new PlaceType
                    {
                        Name = "Berg",
                        NameEn = "Hill",
                        NameMV = "Bergen",
                        Group = "Bergen en Heuvels"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("beach"))
                {
                    return new PlaceType
                    {
                        Name = "Strand",
                        NameEn = "Beach",
                        NameMV = "Strand",
                        Group = "Bergen en Heuvels"
                    };
                }
                else if (geoName.FcodeName.ToLower().Contains("dune"))
                {
                    return new PlaceType
                    {
                        Name = "Duin",
                        NameEn = "Dune",
                        NameMV = "Duinen",
                        Group = "Bergen en Heuvels"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("cliff"))
                {
                    return new PlaceType
                    {
                        Name = "Klif",
                        NameEn = "Cliff",
                        NameMV = "Kliffen",
                        Group = "Bergen en Heuvels"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("volcano"))
                {
                    return new PlaceType
                    {
                        Name = "Vulkaan",
                        NameEn = "Volcano",
                        NameMV = "Vulkanen",
                        Group = "Bergen en Heuvels"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("polder"))
                {
                    return new PlaceType
                    {
                        Name = "Polder",
                        NameEn = "Polder",
                        NameMV = "Polders",
                        Group = "Bergen en Heuvels"
                    };
                }
                else if (geoName.FcodeName.ToLower().Contains("island"))
                {
                    return new PlaceType
                    {
                        Name = "Eiland",
                        NameEn = "Island",
                        NameMV = "Eiland",
                        Group = "Bergen en Heuvels"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("point"))
                {
                    return new PlaceType
                    {
                        Name = "Locatie",
                        NameEn = "Point",
                        NameMV = "Locatie",
                        Group = "Steden en Dorpen"
                    };
                }
            }
            else if (geoName.FclName.ToLower().Equals("forest,heath,..."))
            {
                if (geoName.FcodeName.ToLower().Equals("heath"))
                {
                    return new PlaceType
                    {
                        Name = "Heide",
                        NameEn = "Heath",
                        NameMV = "Heides",
                        Group = "Bossen en Heiden"
                    };
                }
                else if (geoName.FcodeName.ToLower().Contains("forest"))
                {
                    return new PlaceType
                    {
                        Name = "Bos",
                        NameEn = "Forest",
                        NameMV = "Bossen",
                        Group = "Bossen en Heiden"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("meadow"))
                {
                    return new PlaceType
                    {
                        Name = "Weide",
                        NameEn = "Meadow",
                        NameMV = "Weiden",
                        Group = "Bossen en Heiden"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("grassland"))
                {
                    return new PlaceType
                    {
                        Name = "Grasland",
                        NameEn = "Grassland",
                        NameMV = "Graslanden",
                        Group = "Bossen en Heiden"
                    };
                }
            }
            else if (geoName.FclName.ToLower().Equals("parks,area, ..."))
            {
                if (geoName.FcodeName.ToLower().Equals("park"))
                {
                    return new PlaceType
                    {
                        Name = "Park",
                        NameEn = "Park",
                        NameMV = "Parken",
                        Group = "Parken en Gebieden"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("nature reserve"))
                {
                    return new PlaceType
                    {
                        Name = "Natuurreservaat",
                        NameEn = "Nature Reserve",
                        NameMV = "Natuurreservaten",
                        Group = "Parken en Gebieden"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("locality"))
                {
                    return new PlaceType
                    {
                        Name = "Gebied",
                        NameEn = "Locality",
                        NameMV = "Gebieden",
                        Group = "Parken en Gebieden"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("industrial area"))
                {
                    return new PlaceType
                    {
                        Name = "Industrieel Gebied",
                        NameEn = "Industrial Area",
                        NameMV = "Industriele Gebieden",
                        Group = "Parken en Gebieden"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("snowfield"))
                {
                    return new PlaceType
                    {
                        Name = "Sneeuwveld",
                        NameEn = "Snowfield",
                        NameMV = "Sneeuwvelden",
                        Group = "Parken en Gebieden"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("amusement park"))
                {
                    return new PlaceType
                    {
                        Name = "Attractiepark",
                        NameEn = "Amusement Park",
                        NameMV = "Attractieparken",
                        Group = "Parken en Gebieden"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("area"))
                {
                    return new PlaceType
                    {
                        Name = "Gebied",
                        NameEn = "Area",
                        NameMV = "Gebieden",
                        Group = "Parken en Gebieden"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("region"))
                {
                    return new PlaceType
                    {
                        Name = "Regio",
                        NameEn = "Region",
                        NameMV = "Regio's",
                        Group = "Parken en Gebieden"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("port"))
                {
                    return new PlaceType
                    {
                        Name = "Haven",
                        NameEn = "Harbor",
                        NameMV = "Havens",
                        Group = "Waterlichamen"
                    };
                }
                else if (geoName.FcodeName.ToLower().Contains("forest"))
                {
                    return new PlaceType
                    {
                        Name = "Bos",
                        NameEn = "Forest",
                        NameMV = "Bossen",
                        Group = "Bossen en Heiden"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("business center"))
                {
                    return new PlaceType
                    {
                        Name = "Bedrijventerrein",
                        NameEn = "Business Center",
                        NameMV = "Bedrijventerreinen",
                        Group = "Parken en Gebieden"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("grazing area"))
                {
                    return new PlaceType
                    {
                        Name = "Weidegebied",
                        NameEn = "Grazing Area",
                        NameMV = "Weidegebieden",
                        Group = "Parken en Gebieden"
                    };
                }
            }
            else if (geoName.FclName.ToLower().Equals("spot, building, farm"))
            {
                if (geoName.FcodeName.ToLower().Equals("airport") || geoName.FcodeName.ToLower().Equals("heliport"))
                {
                    return new PlaceType
                    {
                        Name = "Luchthaven",
                        NameEn = "Airport",
                        NameMV = "Luchthavens",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("castle"))
                {
                    return new PlaceType
                    {
                        Name = "Kasteel",
                        NameEn = "Castle",
                        NameMV = "Kastelen",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("railroad station"))
                {
                    return new PlaceType
                    {
                        Name = "Treinstation",
                        NameEn = "Railroad Station",
                        NameMV = "Treinstations",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("mine(s)"))
                {
                    return new PlaceType
                    {
                        Name = "Mijn",
                        NameEn = "Mine",
                        NameMV = "Mijnen",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("bridge"))
                {
                    return new PlaceType
                    {
                        Name = "Brug",
                        NameEn = "Bridge",
                        NameMV = "Bruggen",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("ruin(s)"))
                {
                    if (geoName.ToponymName.ToLower().Contains("kasteel"))
                    {
                        return new PlaceType
                        {
                            Name = "Kasteel Ruïne",
                            NameEn = "Castle Ruin",
                            NameMV = "Kasteel Ruïnes",
                            Group = "Gebouwen en Infrastructuur"
                        };
                    }
                    return new PlaceType
                    {
                        Name = "Ruïne",
                        NameEn = "Ruin",
                        NameMV = "Ruïnes",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("zoo"))
                {
                    return new PlaceType
                    {
                        Name = "Dierentuin",
                        NameEn = "Zoo",
                        NameMV = "Dierentuinen",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("fort"))
                {
                    return new PlaceType
                    {
                        Name = "Fort",
                        NameEn = "Fort",
                        NameMV = "Forten",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("tower"))
                {
                    return new PlaceType
                    {
                        Name = "Toren",
                        NameEn = "Tower",
                        NameMV = "Torens",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("gate"))
                {
                    return new PlaceType
                    {
                        Name = "Poort",
                        NameEn = "Gate",
                        NameMV = "Poorten",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("museum"))
                {
                    return new PlaceType
                    {
                        Name = "Museum",
                        NameEn = "Museum",
                        NameMV = "Museums",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("heliport"))
                {
                    return new PlaceType
                    {
                        Name = "Heliport",
                        NameEn = "Heliport",
                        NameMV = "Heliports",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("historical site"))
                {
                    return new PlaceType
                    {
                        Name = "Historische Plaats",
                        NameEn = "Historical Site",
                        NameMV = "Historische Plaatsen",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("tomb(s)"))
                {
                    return new PlaceType
                    {
                        Name = "Hunebed",
                        NameEn = "Tomb",
                        NameMV = "Hunebedden",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("monument"))
                {
                    return new PlaceType
                    {
                        Name = "Monument",
                        NameEn = "Monument",
                        NameMV = "Monumenten",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("palace"))
                {
                    return new PlaceType
                    {
                        Name = "Paleis",
                        NameEn = "Palace",
                        NameMV = "Paleizen",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("dike"))
                {
                    return new PlaceType
                    {
                        Name = "Dijk",
                        NameEn = "Dike",
                        NameMV = "Dijken",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("lighthouse"))
                {
                    return new PlaceType
                    {
                        Name = "Vuurtoren",
                        NameEn = "Lighthouse",
                        NameMV = "Vuurtorens",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("stadium"))
                {
                    return new PlaceType
                    {
                        Name = "Stadion",
                        NameEn = "Stadium",
                        NameMV = "Stadions",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("pier"))
                {
                    return new PlaceType
                    {
                        Name = "Pier",
                        NameEn = "Pier",
                        NameMV = "Piers",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
                else if (geoName.FcodeName.ToLower().Equals("racetrack"))
                {
                    return new PlaceType
                    {
                        Name = "Racebaan",
                        NameEn = "Racetrack",
                        NameMV = "Racebanen",
                        Group = "Gebouwen en Infrastructuur"
                    };
                }
            }
            return null;
        }
    }
}
