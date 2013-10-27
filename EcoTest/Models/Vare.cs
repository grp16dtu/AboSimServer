namespace EcoTest.Models
{
    public class Vare
    {
        public decimal Kostpris { get; set; }
        public string Navn { get; set; }
        public string Nummer { get; set; }
        public decimal Salgpris { get; set; }
        public decimal Volume { get; set; }
        public Afdeling Afdeling { get; set; }

        public Vare(decimal costPrice, string name, string number, decimal salesPrice, decimal volume, Afdeling afdeling)
        {
            Kostpris = costPrice;
            Navn = name;
            Nummer = number;
            Salgpris = salesPrice;
            Volume = volume;
            Afdeling = afdeling;
        }
    }
}
