namespace EcoTest.Models
{
    public class Produkt
    {
        public decimal Kostpris { get; set; }
        public string Navn { get; set; }
        public string Nummer { get; set; }
        public decimal Salgpris { get; set; }
        public decimal Volume { get; set; }

        public Produkt(decimal costPrice, string name, string number, decimal salesPrice, decimal volume)
        {
            Kostpris = costPrice;
            Navn = name;
            Nummer = number;
            Salgpris = salesPrice;
            Volume = volume;
        }
    }
}
