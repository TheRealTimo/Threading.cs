using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;

namespace market_scraper
{
    public class EbayScraper
    {
        static CultureInfo _culture = MainPage._culture;

        internal static async Task Scrape(string searchTerm, int maxThreads, int pageNum, bool searchActiveListings, bool searchSoldListings, Database database, Func<Product, Task> productHandler)
        {
            var sem = new SemaphoreSlim(maxThreads);

            async Task ScrapePage(int page, bool soldListings)
            {
                await sem.WaitAsync();
                try
                {
                    string baseUrl = "https://www.ebay.nl";
                    string searchUrl = $"{baseUrl}/sch/i.html?_nkw={searchTerm}&_ipg=200";

                    if (soldListings)
                    {
                        searchUrl += "&LH_Sold=1&LH_Complete=1";
                    }

                    searchUrl += $"&_pgn={page}";

                    var web = new HtmlWeb();
                    var doc = await web.LoadFromWebAsync(searchUrl);

                    var items = doc.DocumentNode.QuerySelectorAll(".s-item");

                    foreach (var item in items)
                    {
                        string title = item.QuerySelector(".s-item__title")?.InnerText.Trim();
                        string price = item.QuerySelector(".s-item__price")?.InnerText.Trim();

                        if (title != null && price != null)
                        {
                            string cleanPrice = Regex.Match(price, @"\d+[\.,]\d+").Value;
                            double productPrice = double.Parse(cleanPrice, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, _culture);

                            var product = new Product
                            {
                                ProductName = title,
                                ProductPrice = productPrice,
                                Platform = "eBay",
                                SearchTerm = searchTerm,
                                Date = DateTime.UtcNow
                            };

                            // Save the product to the database
                            await database.SaveProductAsync(product);

                            // Add the product to the UI
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
                if (searchActiveListings)
                {
                    tasks.Add(ScrapePage(i, false));
                }
                if (searchSoldListings)
                {
                    tasks.Add(ScrapePage(i, true));
                }
            }

            await Task.WhenAll(tasks);
        }
    }
}
