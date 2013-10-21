using System.Collections.Generic;

namespace EcoTest.Models
{
    public class Abonnement
    {
        public int Id { get; set; }
        public string Navn { get; set; }
        public int Nummer { get; set; }
        // Interval
        public bool KalenderAar { get; set; }
        //Opkraevning
        public List<Abonnent> Abonnenter { get; set; }
        public List<Varelinje> Varelinjer { get; set; }

        public Abonnement(int id, string navn, int nummer,  bool kalenderAar)
        {
            this.Id = id;
            this.Navn = navn;
            this.Nummer = nummer;
            this.KalenderAar = kalenderAar;

            this.Abonnenter = new List<Abonnent>();
            this.Varelinjer = new List<Varelinje>();
        }
    }
}
