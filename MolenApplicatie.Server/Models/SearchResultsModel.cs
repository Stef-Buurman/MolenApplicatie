using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Models
{
    public class SearchResultsModel
    {
        public List<SearchModel<MolenData>> Molens { get; set; } = new List<SearchModel<MolenData>>();
        public List<KeyValuePair<string, List<SearchModel<Place>>>> Places { get; set; } = new List<KeyValuePair<string, List<SearchModel<Place>>>>();
        public List<SearchModel<MolenType>> MolenTypes { get; set; } = new List<SearchModel<MolenType>>();

        public bool HasResults =>
            Molens.Any() ||
            Places.Any() ||
            MolenTypes.Any();
    }

    public class SearchModel<T>
    {
        public string Reference { get; set; } = string.Empty;
        public T Data { get; set; } = default!;
    }
}
