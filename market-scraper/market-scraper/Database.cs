using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace market_scraper
{
    public class Database
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public Database()
        {
            CreateTable().Wait();
        }

        private SqliteConnection GetConnection()
        {
            var dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "scrapeDb.db");
            var connectionString = $"Filename={dbPath};Cache=Shared";
            var connection = new SqliteConnection(connectionString);
            return connection;
        }

        public async Task ClearProductsAsync()
        {
            var sql = "DELETE FROM Products";
            await ExecuteNonQueryAsync(sql);
        }

        public async Task ExecuteNonQueryAsync(string sql)
        {
            await _semaphore.WaitAsync();
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();

                    var command = new SqliteCommand(sql, connection);
                    await command.ExecuteNonQueryAsync();

                    connection.Close();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<DataTable> ExecuteQueryAsync(string sql)
        {
            await _semaphore.WaitAsync();
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();

                    var command = new SqliteCommand(sql, connection);
                    var reader = await command.ExecuteReaderAsync();

                    var dataTable = new DataTable();
                    dataTable.Load(reader);

                    connection.Close();

                    return dataTable;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task CreateTable()
        {
            var createTableSql = @"
CREATE TABLE IF NOT EXISTS products (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    product_name TEXT NOT NULL,
    product_price REAL NOT NULL,
    platform TEXT NOT NULL,
    search_term TEXT NOT NULL,
    date DATETIME NOT NULL
);";

            await ExecuteNonQueryAsync(createTableSql);
        }


        public async Task SaveProductAsync(Product product)
        {
            var sql = @"INSERT INTO products (product_name, product_price, platform, search_term, date)
                 VALUES (@productName, @productPrice, @platform, @searchTerm, @date);";

            await _semaphore.WaitAsync();
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();

                    var command = new SqliteCommand(sql, connection);
                    command.Parameters.AddWithValue("@productName", product.ProductName);
                    command.Parameters.AddWithValue("@productPrice", product.ProductPrice);
                    command.Parameters.AddWithValue("@platform", product.Platform);
                    command.Parameters.AddWithValue("@searchTerm", product.SearchTerm);
                    command.Parameters.AddWithValue("@date", product.Date);

                    await command.ExecuteNonQueryAsync();

                    connection.Close();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<Product>> GetProductsBySearchTermAndPlatformAsync(string searchTerm, string platform)
        {
            var sql = @"SELECT * FROM products
                WHERE search_term = @searchTerm AND platform = @platform;";

            await _semaphore.WaitAsync();
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();

                    var command = new SqliteCommand(sql, connection);
                    command.Parameters.AddWithValue("@searchTerm", searchTerm);
                    command.Parameters.AddWithValue("@platform", platform);
                    var reader = await command.ExecuteReaderAsync();

                    var products = new List<Product>();
                    while (await reader.ReadAsync())
                    {
                        var product = new Product
                        {
                            Id = reader.GetInt32(0),
                            ProductName = reader.GetString(1),
                            ProductPrice = reader.GetDouble(2),
                            Platform = reader.GetString(3),
                            SearchTerm = reader.GetString(4),
                            Date = reader.GetDateTime(5)
                        };

                        products.Add(product);
                    }

                    connection.Close();

                    return products;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<double> GetAveragePriceBySearchTermAndPlatformAsync(string searchTerm, string platform)
        {
            var sql = @"SELECT AVG(product_price) FROM products
                WHERE search_term = @searchTerm AND platform = @platform;";

            await _semaphore.WaitAsync();
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();

                    var command = new SqliteCommand(sql, connection);
                    command.Parameters.AddWithValue("@searchTerm", searchTerm);
                    command.Parameters.AddWithValue("@platform", platform);
                    var result = await command.ExecuteScalarAsync();

                    connection.Close();

                    return result == DBNull.Value ? 0 : Convert.ToDouble(result);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}