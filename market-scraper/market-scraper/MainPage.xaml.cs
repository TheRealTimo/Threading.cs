using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
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
            string itemName = txtSearchItem.Text.Trim();
            bool searchSoldListings = chkSold.IsChecked == true;
            bool searchActiveListings = chkActive.IsChecked == true;
            int maxThreads = int.Parse(MaxThreadsTextBox.Text);
            int pageNum = int.Parse(PageNumTextBox.Text);

            if (itemName.Length == 0 || (!searchSoldListings && !searchActiveListings))
            {
                tbSettingText.Text = "Please enter an item name and select a search type.";
                return;
            }

            string ebayUrl = "https://www.ebay.com/sch/i.html?_from=R40&_nkw=" + itemName.Replace(" ", "+") + "&_sacat=0";
            if (searchSoldListings)
            {
                ebayUrl += "&LH_Sold=1&LH_Complete=1";
            }
            else if (searchActiveListings)
            {
                ebayUrl += "&LH_Auction=1&LH_Sale_Currency=0&LH_Complete=1";
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0");
                client.DefaultRequestHeaders.Add("Referer", "https://www.ebay.com/");

                List<double> itemPrices = new List<double>();
                List<string> itemTitles = new List<string>();
                var sem = new SemaphoreSlim(maxThreads);

                async Task ScrapePage(int page)
                {
                    await sem.WaitAsync();
                    try
                    {
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
                    tbSettingText.Text = "No results found.";
                }
                else
                {
                    for (int i = 1; i < itemPrices.Count; i++)
                    {
                        tbSettingText.Text += itemTitles[i] + " - $" + itemPrices[i].ToString("0.00") + Environment.NewLine;
                    }
                }
            }
        }
    }
}
