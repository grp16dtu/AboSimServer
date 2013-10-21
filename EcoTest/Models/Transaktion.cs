namespace EcoTest.Models
{
    public class Transaktion
    {
        public int Aar { get; set; }
        public int Maaned { get; set; }
        public string Debitornummer { get; set; }
        public string Produktnummer { get; set; }
        //department
        //public int ProjectNumber { get; set; }
        public decimal? Antal { get; set; }
        public decimal Beloeb { get; set; }

        public Transaktion(int year, int month, string debtorNumber, string productNumber, decimal? quantity, decimal amount)
        {
            Aar = year;
            Maaned = month;
            Debitornummer = debtorNumber;
            Produktnummer = productNumber;
            Antal = quantity;
            Beloeb = amount;
        }
    }
}
