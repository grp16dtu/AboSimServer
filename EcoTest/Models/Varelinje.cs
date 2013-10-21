namespace EcoTest.Models
{
    public class Varelinje
    {
        public int Id { get; set; }
        public int Nummer { get; set; }

        public string Produktnavn { get; set; }
        public decimal? Antal { get; set; }
        public decimal? Specielpris { get; set; }
        public Produkt Produkt { get; set; }

        public Varelinje(int id, int number, string productName, decimal? quantity, decimal? specialPrice, Produkt product)
        {
            Id = id;
            Nummer = number;
            Produktnavn = productName;
            Antal = quantity;
            Specielpris = specialPrice;
            Produkt = product;
        }
    }
}
