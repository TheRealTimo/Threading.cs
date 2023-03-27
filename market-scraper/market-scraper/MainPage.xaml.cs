using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
            await HtmlToolTest.Main();
            
            
            try
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
            catch (Exception ex)
            {
                Debug.WriteLine("Error in SearchButton_Click: " + ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private async Task<List<AmazonProduct>> FetchAmazonPrices(string searchTerm, int maxThreads, int pageNum)
        {
            try
            {
                var products = new List<AmazonProduct>();
                var sem = new SemaphoreSlim(maxThreads);
                var htmlTools = new HtmlTools();

                async Task ScrapePage(int page)
                {
                    await sem.WaitAsync();
                    try
                    {
                        string amazonUrl = $"https://www.amazon.nl/s?k={searchTerm}&page={page}";

                        string html = await htmlTools.GetHtml(amazonUrl);
                        var rootNode = htmlTools.ParseHtml(html);

                        var items = htmlTools.SelectNodes(rootNode, ".s-result-item").ToList();

                        foreach (var item in items)
                        {
                            string title = htmlTools.SelectNodes(item, ".a-text-normal").FirstOrDefault()?.InnerText
                                .Trim();
                            string price = htmlTools.SelectNodes(item, ".a-price-whole").FirstOrDefault()?.InnerText
                                .Trim();

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
            catch (Exception ex)
            {
                Debug.WriteLine("Error in FetchAmazonPrices: " + ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return null;
            }
        }

    }
}