using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using System.Globalization;
using System.Linq;

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
            MinPriceTextBlock.Text = "";
            MaxPriceTextBlock.Text = "";
            AvgPriceTextBlock.Text = "";
            EbaySettingsTextBlock.Text = "";

            string searchTerm = SearchTermTextBox.Text;
            int maxThreads = int.Parse(MaxThreadsTextBox.Text);
            int pageNum = int.Parse(PageNumTextBox.Text);

            var products = await FetchAmazonPrices(searchTerm, maxThreads, pageNum);
            ProductsDataGrid.ItemsSource = products;

            CultureInfo culture = new CultureInfo("nl-NL");
            //CultureInfo needed to let the system know that the decimal separator is a comma and not a dot on amazon

            double minAmazonPrice = products.Min(p => double.Parse(p.Price.Replace("€", ""), culture));
            double maxAmazonPrice = products.Max(p => double.Parse(p.Price.Replace("€", ""), culture));
            double avgAmazonPrice = products.Average(p => double.Parse(p.Price.Replace("€", ""), culture));

            List<double> itemPrices = new List<double>();
            List<string> itemTitles = new List<string>();
            var sem = new SemaphoreSlim(maxThreads);

            async Task ScrapePage(int page)
            {
                await sem.WaitAsync();
                try
                {
                    var client = new HttpClient();
                    string ebayUrl = "https://www.ebay.com/sch/i.html?_from=R40&_nkw=" + searchTerm.Replace(" ", "+") + "&_sacat=0";

                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0");
                    client.DefaultRequestHeaders.Add("Referer", "https://www.ebay.com/");

                    var response = await client.GetAsync($"{ebayUrl}&_pgn={page}");
                    response.EnsureSuccessStatusCode();
                    string ebayHtml = await response.Content.ReadAsStringAsync();

                    HtmlDocument ebayDoc = new HtmlDocument();
                    ebayDoc.LoadHtml(ebayHtml);

                    foreach (HtmlNode node in ebayDoc.DocumentNode.SelectNodes("//span[@class='s-item__price']"))
                    {
                        string priceString = node.InnerText.Trim().Replace("$", "").Replace(",", "");
                        double price;
                        if (Double.TryParse(priceString, out price) && !Double.IsNaN(price))
                        {
                            lock (itemPrices)
                            {
                                itemPrices.Add(price);
                            }
                        }
                    }

                    foreach (HtmlNode node in ebayDoc.DocumentNode.SelectNodes("//div[@class='s-item__title']"))
                    {
                        string title = node.InnerText.Trim();
                        lock (itemTitles)
                        {
                            itemTitles.Add(title);
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

            if (itemPrices.Count == 0)
            {
                EbaySettingsTextBlock.Text = "No results found.";
            }
            else
            {
                string ebayResults = "";
                for (int i = 1; i < itemPrices.Count; i++)
                {
                    ebayResults += itemTitles[i] + " - $" + itemPrices[i].ToString("0.00") + Environment.NewLine;
                }
                EbaySettingsTextBlock.Text = ebayResults;
            }

            double minEbayPrice = itemPrices.Count > 0 ? itemPrices.Min() : 0;
            double maxEbayPrice = itemPrices.Count > 0 ? itemPrices.Max() : 0;
            double avgEbayPrice = itemPrices.Count > 0 ? itemPrices.Average() : 0;
            
            MinPriceTextBlock.Text += Math.Min(minAmazonPrice, minEbayPrice).ToString("0.00", culture);
            MaxPriceTextBlock.Text += Math.Max(maxAmazonPrice, maxEbayPrice).ToString("0.00", culture);
            AvgPriceTextBlock.Text += ((avgAmazonPrice + avgEbayPrice) / 2).ToString("0.00", culture);
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
