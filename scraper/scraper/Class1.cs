using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scraper
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using HtmlAgilityPack;
    using Fizzler.Systems.HtmlAgilityPack;

    namespace PriceScraper
    {
        class Program
        {
            static async Task Main(string[] args)
            {
                Console.Write("Please enter a search term: ");
                string searchTerm = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    Console.WriteLine("Search term cannot be empty.");
                    Console.ReadLine();
                    return;
                }

                await FetchAmazonPrices(searchTerm);
                Console.WriteLine("------");
                await FetchEbayPrices(searchTerm);

                Console.WriteLine("\nPress ENTER to exit...");
                Console.ReadLine();
            }

            private static async Task FetchAmazonPrices(string searchTerm)
            {
                string amazonUrl = $"https://www.amazon.nl/s?k={searchTerm}";

                var web = new HtmlWeb();
                var doc = await web.LoadFromWebAsync(amazonUrl);

                var products = doc.DocumentNode.QuerySelectorAll(".s-result-item");

                Console.WriteLine("Amazon Prices:");
                foreach (var product in products)
                {
                    string title = product.QuerySelector(".a-text-normal")?.InnerText.Trim();
                    string price = product.QuerySelector(".a-price-whole")?.InnerText.Trim();

                    if (title != null && price != null)
                    {
                        Console.WriteLine($"{title} - €{price}");
                    }
                }
            }

            private static async Task FetchEbayPrices(string searchTerm)
            {
                string ebayUrl = $"https://www.ebay.com/sch/i.html?_nkw={searchTerm}&_sacat=0&LH";

                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(ebayUrl);
                var content = await response.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(content);

                var products = doc.DocumentNode.QuerySelectorAll(".s-item");

                Console.WriteLine("Ebay Prices:");
                foreach (var product in products)
                {
                    string title = product.QuerySelector(".s-item__title")?.InnerText.Trim();
                    string price = product.QuerySelector(".s-item__price")?.InnerText.Trim();

                    if (title != null && price != null)
                    {
                        Console.WriteLine($"{title} - {price}");
                    }
                }
            }
        }
    }

}
