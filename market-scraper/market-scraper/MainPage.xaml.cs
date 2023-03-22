using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using HtmlAgilityPack;

namespace market_scraper
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void btnRetrieveData_Click(object sender, RoutedEventArgs e)
        {
            string soldItemsUrl = "https://www.ebay.com/sch/i.html?_from=R40&_nkw=air+jordan+1+bred&_sacat=0&rt=nc&LH_Sold=1&LH_Complete=1";

            // Send a GET request to the sold items page
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0");
                client.DefaultRequestHeaders.Add("Referer", "https://www.ebay.com/");
                var response = await client.GetAsync(soldItemsUrl);
                response.EnsureSuccessStatusCode();
                string soldItemsHtml = await response.Content.ReadAsStringAsync();
                
                HtmlDocument soldItemsDoc = new HtmlDocument();
                soldItemsDoc.LoadHtml(soldItemsHtml);
                
                List<double> soldItemPrices = new List<double>();
                foreach (HtmlNode node in soldItemsDoc.DocumentNode.SelectNodes("//span[@class='s-item__price']"))
                {
                    string priceString = node.InnerText.Trim().Replace("$", "").Replace(",", "");
                    double price;
                    if (Double.TryParse(priceString, out price))
                    {
                        soldItemPrices.Add(price);
                    }
                }
                
                foreach (double price in soldItemPrices)
                {
                    tbSettingText.Text += price.ToString() + Environment.NewLine;
                }
            }
        }
    }
}