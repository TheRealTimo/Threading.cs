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
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchTerm = SearchTermTextBox.Text;
            int maxThreads = int.Parse(MaxThreadsTextBox.Text);
            int pageNum = int.Parse(PageNumTextBox.Text);


            var products = await FetchAmazonPrices(searchTerm, maxThreads, pageNum);
            ProductsDataGrid.ItemsSource = products;
            
            CultureInfo culture = new CultureInfo("nl-NL");
            //CultureInfo needed to let the system know that the decimal separator is a comma and not a dot on amazon
            
            double minPrice = products.Min(p => double.Parse(p.Price.Replace("€", ""), culture));
            double maxPrice = products.Max(p => double.Parse(p.Price.Replace("€", ""), culture));
            double avgPrice = products.Average(p => double.Parse(p.Price.Replace("€", ""), culture));
            
            MinPriceTextBlock.Text += minPrice.ToString("0.00", culture);
            MaxPriceTextBlock.Text += maxPrice.ToString("0.00", culture);
            AvgPriceTextBlock.Text += avgPrice.ToString("0.00", culture);
        }

        private async Task<List<AmazonProduct>> FetchAmazonPrices(string searchTerm, int maxThreads, int pageNum)
        {
            var products = new List<AmazonProduct>();
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
                            lock (products)
                            {
                                products.Add(new AmazonProduct { Name = title, Price = "€" + price });
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