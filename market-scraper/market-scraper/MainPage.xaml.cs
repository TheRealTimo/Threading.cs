using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using System.Globalization;

namespace market_scraper
{
    public sealed partial class MainPage : Page
    {
        private readonly Database _database;
        readonly CultureInfo _culture = new CultureInfo("nl-NL");
        public MainPage()
        {
            this.InitializeComponent();
            _database = new Database();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchTerm = SearchTermTextBox.Text;
            int maxThreads = int.Parse(MaxThreadsTextBox.Text);
            int pageNum = int.Parse(PageNumTextBox.Text);


            var products = await FetchAmazonPrices(searchTerm, maxThreads, pageNum);
            ProductsDataGrid.ItemsSource = products;
            

        }
        private async void LoadDataButton_Click(object sender, RoutedEventArgs e)
        {
            string searchTerm = SearchTermTextBox.Text;
            string platform = "Amazon"; // Replace with the platform you want to retrieve data for

            // Create a new instance of the Database class
            var database = new Database();

            // Get the products from the database based on searchTerm and platform
            var products = await database.GetProductsBySearchTermAndPlatformAsync(searchTerm, platform);

            // Bind the retrieved products to the ProductsDataGrid
            ProductsDataGrid.ItemsSource = products;
            DisplayProductData();
        }

        private async void DisplayProductData()
        {
            var searchTerm = SearchTermTextBox.Text;
            var platform = "Amazon";

            var products = await _database.GetProductsBySearchTermAndPlatformAsync(searchTerm, platform);
            ProductsDataGrid.ItemsSource = products;

            

            double minPrice = products.Min(p => p.ProductPrice);
            double maxPrice = products.Max(p => p.ProductPrice);
            double avgPrice = products.Average(p => p.ProductPrice);

            MinPriceTextBlock.Text = "Minimum Price: " + minPrice.ToString("0.00", _culture);
            MaxPriceTextBlock.Text = "Maximum Price: " + maxPrice.ToString("0.00", _culture);
            AvgPriceTextBlock.Text = "Average Price: " + avgPrice.ToString("0.00", _culture);
        }
        
        private async void ClearDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            await _database.ClearProductsAsync();
        }



        private async Task<List<Product>> FetchAmazonPrices(string searchTerm, int maxThreads, int pageNum)
        {
            var products = new List<Product>();
            var sem = new SemaphoreSlim(maxThreads);

            async Task ScrapePage(int page)
            {
                await sem.WaitAsync();
                try
                {
                    string amazonUrl = $"https://www.amazon.nl/s?k={searchTerm}&page={page}";

                    var web = new HtmlWeb();
                    var doc = await web.LoadFromWebAsync(amazonUrl);

                    var items = doc.DocumentNode.QuerySelectorAll(".s-result-item");

                    foreach (var item in items)
                    {
                        string title = item.QuerySelector(".a-text-normal")?.InnerText.Trim();
                        string price = item.QuerySelector(".a-price-whole")?.InnerText.Trim();

                        if (title != null && price != null)
                        {
                            
                            double productPrice = double.Parse(price.Replace("€", ""), NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, _culture);


                            var product = new Product
                            {
                                ProductName = title,
                                ProductPrice = productPrice,
                                Platform = "Amazon",
                                SearchTerm = searchTerm,
                                Date = DateTime.UtcNow
                            };

                            // Save the product to the database
                            await _database.SaveProductAsync(product);

                            lock (products)
                            {
                                products.Add(product);
                            }
                        }
                    }
                }
                finally
                {
                    sem.Release();
                }
            }

            var tasks = new List<Task>();

            for (int i = 1; i <= pageNum; i++)
            {
                tasks.Add(ScrapePage(i));
            }

            await Task.WhenAll(tasks);

            return products;
        }


    }
}