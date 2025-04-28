using MolenApplicatie.Server.Models;
using SQLite;

namespace MolenApplicatie.Server.Services
{
    public class DbConnection
    {
        public readonly SQLiteAsyncConnection _db;

        public DbConnection(string dbRoute)
        {
            _db = new SQLiteAsyncConnection(dbRoute);
            InitializeDB().Wait();
        }

        public async Task InitializeDB()
        {
            try
            {
                await _db.CreateTableAsync<MolenTBNOld>();
                await _db.CreateTableAsync<MolenDataOld>();
                await _db.CreateTableAsync<MolenTypeOld>();
                await _db.CreateTableAsync<MolenTypeAssociationOld>();
                await _db.CreateTableAsync<LastSearchedForNewDataOld>();
                await _db.CreateTableAsync<PlaceOld>();
                await _db.CreateTableAsync<VerdwenenYearInfoOld>();
                await _db.CreateTableAsync<MolenMakerOld>();
                await _db.CreateTableAsync<MolenImageOld>();
                await _db.CreateTableAsync<AddedImageOld>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing the database: {ex.Message}");
                throw;
            }
        }

        public async Task<int> UpdateAsync<T>(T item) where T : new()
        {
            try
            {
                return await _db.UpdateAsync(item);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating item in the database: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteAsync<T>(T item) where T : new()
        {
            try
            {
                return await _db.DeleteAsync(item);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting item from the database: {ex.Message}");
                throw;
            }
        }

        public async Task<List<T>> QueryAsync<T>(string query, params object[] args) where T : new()
        {
            try
            {
                return await _db.QueryAsync<T>(query, args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing query: {ex.Message}");
                throw;
            }
        }

        public async Task<List<T>> Table<T>() where T : new()
        {
            try
            {
                return await _db.Table<T>().ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving items from the table: {ex.Message}");
                throw;
            }
        }

        public async Task<T> FindWithQueryAsync<T>(string query, params object[] args) where T : new()
        {
            try
            {
                return await _db.FindWithQueryAsync<T>(query);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing query: {ex.Message}");
                throw;
            }
        }

        public async Task<int> InsertAsync<T>(T item) where T : new()
        {
            try
            {
                return await _db.InsertAsync(item);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting item to the database: {ex.Message}");
                throw;
            }
        }

        public async Task<T> ExecuteScalarAsync<T>(string query, params object[] args)
        {
            try
            {
                var result = await _db.ExecuteScalarAsync<T>(query, args);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing scalar query: {ex.Message}");
                throw;
            }
        }

        public Task CloseConnectionAsync()
        {
            return _db.CloseAsync();
        }
    }
}
