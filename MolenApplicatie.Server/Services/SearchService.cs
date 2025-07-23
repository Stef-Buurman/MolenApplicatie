using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models;
using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Utils;

namespace MolenApplicatie.Server.Services
{
    public class SearchService
    {
        private readonly MolenDbContext _dbContext;

        public SearchService(MolenDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<SearchResultsModel> SearchAllAsync(string query, int limit = 10)
        {
            var results = new SearchResultsModel
            {
                Molens = await SearchMolenDataAsync(query, limit),
                Places = await SearchPlacesAsync(query, limit),
                MolenTypes = await SearchMolenTypesAsync(query, limit)
            };

            return results;
        }

        public async Task<List<SearchModel<MolenData>>> SearchMolenDataAsync(string query, int limit)
        {
            List<string> allowedMolenTypes = Globals.AllowedMolenTypes.Select(t => t.ToLower()).ToList();
            query = query.ToLower();

            var directMolens = await _dbContext.MolenData
                .Where(m => m.Name != null && EF.Functions.Like(m.Name.ToLower(), $"%{query}%") && m.Toestand != null
                    && m.Toestand != MolenToestand.Verdwenen && m.MolenTypeAssociations.Any(ma => allowedMolenTypes.Contains(ma.MolenType.Name.ToLower())))
                .Select(m => new { m.Id, m.Name, Source = "Molen naam: " })
                .Take(limit)
                .ToListAsync();

            var buildYearMolens = await _dbContext.MolenData
                .Where(m => m.Bouwjaar != null && EF.Functions.Like(m.Bouwjaar.ToString(), $"%{query}%")
                    && m.Toestand != null && m.Toestand != MolenToestand.Verdwenen && m.MolenTypeAssociations.Any(ma => allowedMolenTypes.Contains(ma.MolenType.Name.ToLower())))
                .Select(m => new { m.Id, Name = m.Bouwjaar.ToString(), Source = "Molen Bouwjaar: " })
                .Take(limit)
                .ToListAsync();

            var tbnMolens = await _dbContext.MolenTBNs
                .Where(t => t.Ten_Brugge_Nr != null && EF.Functions.Like(t.Ten_Brugge_Nr.ToLower(), $"%{query}%"))
                .Select(t => new { t.MolenData.Id, Name = t.Ten_Brugge_Nr, Source = "Molen TBN: " })
                .Take(limit)
                .ToListAsync();

            var typeMolens = await _dbContext.MolenTypeAssociations
                .Where(ma => ma.MolenType.Name != null && EF.Functions.Like(ma.MolenType.Name.ToLower(), $"%{query}%")
                    && ma.MolenData.Toestand != null && ma.MolenData.Toestand != MolenToestand.Verdwenen && allowedMolenTypes.Contains(ma.MolenType.Name.ToLower()))
                .Select(ma => new { ma.MolenData.Id, ma.MolenType.Name, Source = "Molen Type: " })
                .Take(limit)
                .ToListAsync();

            var placeMolens = await _dbContext.MolenData
                .Where(m => m.Plaats != null && EF.Functions.Like(m.Plaats.ToLower(), $"%{query}%")
                    && m.Toestand != null && m.Toestand != MolenToestand.Verdwenen && m.MolenTypeAssociations.Any(ma => allowedMolenTypes.Contains(ma.MolenType.Name.ToLower())))
                .Select(m => new { m.Id, Name = m.Plaats, Source = "Molen Plaats: " })
                .Take(limit)
                .ToListAsync();

            var combined = directMolens
                .Concat(tbnMolens)
                .Concat(typeMolens)
                .Concat(placeMolens)
                .Concat(buildYearMolens);

            var references = combined
                .GroupBy(x => x.Id)
                .Select(g => new
                {
                    Id = g.Key,
                    Reference = g.First().Source + g.First().Name
                })
                .Take(limit)
                .ToList();

            var molenIds = references.Select(r => r.Id).ToList();

            var molenDataDict = await _dbContext.MolenData
                .Where(m => molenIds.Contains(m.Id))
                .Include(m => m.MolenTypeAssociations)
                .ThenInclude(ma => ma.MolenType)
                .ToDictionaryAsync(m => m.Id);

            var results = references
                .Where(r => molenDataDict.ContainsKey(r.Id))
                .Select(r => new SearchModel<MolenData>
                {
                    Reference = r.Reference,
                    Data = molenDataDict[r.Id]
                })
                .ToList();

            return results;
        }

        public async Task<List<KeyValuePair<string, List<SearchModel<Place>>>>> SearchPlacesAsync(string query, int limit)
        {
            query = query.ToLower();

            var matchedPlaces = await _dbContext.Places
                .Where(p => p.Name != null && EF.Functions.Like(p.Name.ToLower(), $"%{query}%"))
                .Select(p => new
                {
                    Place = p,
                    TypeName = p.Type.Group ?? "Onbekend"
                })
                .Take(limit)
                .ToListAsync();

            var results = matchedPlaces
                .GroupBy(x => x.TypeName)
                .Select(g =>
                {
                    var list = g.Select(x => new SearchModel<Place>
                    {
                        Reference = "Plaats naam: " + x.Place.Name,
                        Data = x.Place
                    })
                    .OrderByDescending(x => x.Data.Population)
                    .ToList();

                    var totalPopulation = list.Sum(x => x.Data.Population);

                    return new
                    {
                        Key = g.Key,
                        Places = list,
                        TotalPopulation = totalPopulation
                    };
                })
                .OrderByDescending(x => x.TotalPopulation)
                .Select(x => new KeyValuePair<string, List<SearchModel<Place>>>(x.Key, x.Places))
                .ToList();

            return results;
        }



        public async Task<List<SearchModel<MolenType>>> SearchMolenTypesAsync(string query, int limit)
        {
            query = query.ToLower();

            var molenTypes = await _dbContext.MolenTypes
                .Where(mt => mt.Name != null && EF.Functions.Like(mt.Name.ToLower(), $"%{query}%"))
                .Take(limit)
                .ToListAsync();

            return molenTypes.Select(mt => new SearchModel<MolenType>
            {
                Reference = "Molen Type: " + mt.Name,
                Data = mt
            }).ToList();
        }

    }
}