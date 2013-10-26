using System.Collections.Generic;

namespace EcoTest.Models
{
    public class Abonnement
    {
        public int Id { get; set; }
        public string Navn { get; set; }
        public int Nummer { get; set; }
        public string Interval { get; set; }
        public bool KalenderAar { get; set; }
        public string Opkraevning { get; set; }
        public List<Abonnent> Abonnenter { get; set; }
        public List<Varelinje> Varelinjer { get; set; }

        public Abonnement(int id, string navn, int nummer,  bool kalenderAar, string interval, string opkraevning)
        {
            this.Id = id;
            this.Navn = navn;
            this.Nummer = nummer;
            this.KalenderAar = kalenderAar;
            this.Interval = interval;
            this.Opkraevning = opkraevning;

            this.Abonnenter = new List<Abonnent>();
            this.Varelinjer = new List<Varelinje>();
        }

        public bool OpkraevesForholdsmaessigt()
        {
            return Opkraevning.Equals("Proportional");
        }
    }
}
