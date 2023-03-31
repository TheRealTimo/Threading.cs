using System;

namespace market_scraper
{
    public class Product
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public double ProductPrice { get; set; }
        public string Platform { get; set; }
        public string SearchTerm { get; set; }
        public DateTime Date { get; set; }
    }
}