using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace market_scraper
{
    public class AmazonScraper
    {
        static CultureInfo _culture = MainPage._culture;

        internal static async Task Scrape(string searchTerm, int maxThreads, int pageNum, Database database, Func<Product, Task> productHandler)
        {
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
                            await database.SaveProductAsync(product);

                            await productHandler(product);
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
        }
    }
}
