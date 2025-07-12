using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;
using System.Text.Json;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenTBNService : DBDefaultService<MolenTBN>
    {
        public DBMolenTBNService(MolenDbContext context) : base(context)
        {}
        public override bool Exists(MolenTBN molenTBN, out MolenTBN? existing)
        {
            return Exists(e => e.Ten_Brugge_Nr == molenTBN.Ten_Brugge_Nr, out existing);
        }

        //public override async Task<List<MolenTBN>> UpdateRange(List<MolenTBN> entities)
        //{
        //    if (entities == null) return entities;

        //    foreach (var entity in entities)
        //    {

        //        _context.Entry(entity).State = EntityState.Modified;

        //        var trackedEntity = _context.ChangeTracker.Entries<MolenTBN>()
        //            .FirstOrDefault(e => e.Entity.Id == entity.Id)?.Entity;

        //        if (trackedEntity == null)
        //        {
        //            var existing = await _context.Set<MolenTBN>().FindAsync(entity.Id);

        //            if (existing != null)
        //            {
        //                if (entity.Id == Guid.Empty && existing != null && existing.Id != Guid.Empty)
        //                    entity.Id = existing.Id;
        //                _context.Entry(existing).CurrentValues.SetValues(entity);
        //            }
        //            else
        //            {
        //                await _context.AddAsync(entity);
        //            }
        //        }
        //    }

        //    _dbSet.UpdateRange(entities);

        //    return entities;
        //}
    }
}
