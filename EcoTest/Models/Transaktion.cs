namespace EcoTest.Models
{
    public class Transaktion
    {
        public int Aar { get; set; }
        public int Maaned { get; set; }
        public string Debitornummer { get; set; }
        public string Varenummer { get; set; }
        public int? Afdelingsnummer { get; set; }
        public decimal? Antal { get; set; }
        public decimal Beloeb { get; set; }

        public Transaktion(int aar, int maaned, string debitornummer, string varenummer, decimal? antal, decimal beloeb, int? afdelingsnummer)
        {
            Aar = aar;
            Maaned = maaned;
            Debitornummer = debitornummer;
            Varenummer = varenummer; 
            Antal = antal;
            Beloeb = beloeb;
            Afdelingsnummer = afdelingsnummer;
        }
    }
}
