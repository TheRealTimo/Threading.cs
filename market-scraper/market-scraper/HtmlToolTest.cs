using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace market_scraper
{
    public class HtmlToolTest
    {
        public static async Task Main()
        {
            var htmlTools = new HtmlTools();

            var exampleUrl = "https://www.example.com";
            var exampleHtml = await htmlTools.GetHtml(exampleUrl);

            var rootNode = htmlTools.ParseHtml(exampleHtml);

            var h1Node = htmlTools.SelectNodes(rootNode, "h1").FirstOrDefault();
            var pNodes = htmlTools.SelectNodes(rootNode, "p").ToList();

            if (h1Node != null)
                Debug.WriteLine("H1 text: " + h1Node.InnerText);
            else
                Debug.WriteLine("No h1 tag found.");

            if (pNodes.Any())
            {
                Debug.WriteLine("P texts:");
                foreach (var pNode in pNodes) Debug.WriteLine(pNode.InnerText);
            }
            else
            {
                Debug.WriteLine("No p tags found.");
            }

            
            
            var amazonHtmlTools = new HtmlTools();

            var searchTerm = "shoes";
            var amazonUrl = $"https://www.amazon.nl/s?k={searchTerm}";
            var amazonHtml = await htmlTools.GetHtml(amazonUrl);

            //Debug.WriteLine("Amazon HTML:");
            //Debug.WriteLine(amazonHtml);


            var amazonRootNode = htmlTools.ParseHtml(amazonHtml);

            var items = amazonHtmlTools.SelectNodes(amazonRootNode, ".s-result-item").ToList();

            var titlesAndPrices = items.Select(item =>
            {
                var title = amazonHtmlTools.SelectNodes(item, ".a-text-normal").FirstOrDefault()?.InnerText.Trim();
                var price = amazonHtmlTools.SelectNodes(item, ".a-price-whole").FirstOrDefault()?.InnerText.Trim();
                return new { Title = title, Price = price };
            }).ToList();


            Debug.WriteLine("Titles and Prices:");
            foreach (var item in titlesAndPrices)
            {
                Debug.WriteLine($"Title: {item.Title}");
                Debug.WriteLine($"Price: €{item.Price}");
                Debug.WriteLine("-----------------");
            }
        }
    }
}