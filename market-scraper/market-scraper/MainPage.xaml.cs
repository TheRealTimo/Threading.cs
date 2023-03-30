using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Globalization;

namespace market_scraper
{
    public sealed partial class MainPage : Page
    {
        public static readonly CultureInfo _culture = new CultureInfo("nl-NL");
        private readonly Database _database;
        
        public MainPage()
        {
            this.InitializeComponent();
            _database = new Database();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchTerm = SearchTermTextBox.Text;
            int maxThreads = int.Parse(MaxThreadsTextBox.Text);
            int pageNum = int.Parse(PageNumTextBox.Text);
            bool searchActiveListings = chkActive.IsChecked.Value;
            bool searchSoldListings = chkSold.IsChecked.Value;
            bool searchAmazon = chkAmazon.IsChecked.Value;
            bool searchEbay = chkEbay.IsChecked.Value;
            
            if (searchAmazon)
            {
                var amazonProducts = await AmazonScraper.Scrape(searchTerm, maxThreads, pageNum, _database);
                AmazonDataGrid.ItemsSource = amazonProducts;
            }
            if (searchEbay)
            {
                var ebayProducts = await EbayScraper.Scrape(searchTerm, maxThreads, pageNum, searchActiveListings, searchSoldListings, _database);
                EbayDataGrid.ItemsSource = ebayProducts;
            }
            
            DisplayProductData();

        }
        private async void LoadDataButton_Click(object sender, RoutedEventArgs e)
        {
            DisplayProductData();
        }

        private async void DisplayProductData()
        {
            string searchTerm = SearchTermTextBox.Text;
            bool searchAmazon = chkAmazon.IsChecked.Value;
            bool searchEbay = chkEbay.IsChecked.Value;

            if (searchAmazon)
            {
                var amazonProducts = await _database.GetProductsBySearchTermAndPlatformAsync(searchTerm, "Amazon");
                AmazonDataGrid.ItemsSource = amazonProducts;
                DisplayPrices(amazonProducts, AmazonMinPriceTextBlock, AmazonMaxPriceTextBlock, AmazonAvgPriceTextBlock);
            }

            if (searchEbay)
            {
                var ebayProducts = await _database.GetProductsBySearchTermAndPlatformAsync(searchTerm, "eBay");
                EbayDataGrid.ItemsSource = ebayProducts;
                DisplayPrices(ebayProducts, EbayMinPriceTextBlock, EbayMaxPriceTextBlock, EbayAvgPriceTextBlock);
            }
        }

        private void DisplayPrices(List<Product> products, TextBlock minPriceTextBlock, TextBlock maxPriceTextBlock, TextBlock avgPriceTextBlock)
        {
            double minPrice = products.Min(p => p.ProductPrice);
            double maxPrice = products.Max(p => p.ProductPrice);
            double avgPrice = products.Average(p => p.ProductPrice);

            minPriceTextBlock.Text = "Minimum Price: " + minPrice.ToString("0.00", _culture);
            maxPriceTextBlock.Text = "Maximum Price: " + maxPrice.ToString("0.00", _culture);
            avgPriceTextBlock.Text = "Average Price: " + avgPrice.ToString("0.00", _culture);
        }

        private async void ClearDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            await _database.ClearProductsAsync();
        }
    }
    


        
    
    }