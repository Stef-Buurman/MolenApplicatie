﻿using MolenApplicatie.Server.Models;
using SQLite;
using MolenApplicatie.Server.Utils;

namespace MolenApplicatie.Server.Services
{
    public class DbConnection
    {
        private readonly HttpClient _client;
        private readonly SQLiteAsyncConnection _db;

        public DbConnection()
        {
            _client = new HttpClient();
            _db = new SQLiteAsyncConnection(Globals.DBBestaandeMolens);
            InitializeDB().Wait();
        }

        public async Task InitializeDB()
        {
            try
            {
                await _db.CreateTableAsync<MolenTBN>();
                await _db.CreateTableAsync<MolenData>();
                await _db.CreateTableAsync<MolenType>();
                await _db.CreateTableAsync<MolenTypeAssociation>();
                await _db.CreateTableAsync<LastSearchedForNewData>();
                await _db.CreateTableAsync<Place>();
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
                var result = await _db.ExecuteScalarAsync<object>(query, args);
                return (T)result;
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