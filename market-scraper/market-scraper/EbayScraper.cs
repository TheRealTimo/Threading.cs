﻿using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace market_scraper
{
    public class EbayScraper
    {
        static CultureInfo _culture = MainPage._culture;

        internal static async Task<List<Product>> Scrape(string searchTerm, int maxThreads, int pageNum, bool searchActiveListings, bool searchSoldListings, Database database)
        {
            var products = new List<Product>();
            var sem = new SemaphoreSlim(maxThreads);

            async Task ScrapePage(int page)
            {
                await sem.WaitAsync();
                try
                {
                    string ebayUrl = $"https://www.ebay.com/sch/i.html?_from=R40&_nkw={searchTerm.Replace(" ", "+")}&_sacat=0";

                    if (searchSoldListings)
                    {
                        ebayUrl += "&LH_Sold=1&LH_Complete=1";
                    }
                    else if (searchActiveListings)
                    {
                        ebayUrl = ebayUrl.Replace("&_sacat=0", "&_sacat=20081");
                    }

                    ebayUrl += $"&_pgn={page}";

                    var web = new HtmlWeb();
                    var doc = await web.LoadFromWebAsync(ebayUrl);

                    var items = doc.DocumentNode.QuerySelectorAll(".s-item");

                    foreach (var item in items)
                    {
                        string title = item.QuerySelector(".s-item__title")?.InnerText.Trim();
                        string price = item.QuerySelector(".s-item__price")?.InnerText.Trim();

                        if (title != null && price != null && double.TryParse(price.Replace("$", "").Replace(",", ""), NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double productPrice))
                        {
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