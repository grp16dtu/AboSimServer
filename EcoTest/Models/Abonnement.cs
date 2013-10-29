using System;
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

        public DateTime LaegIntervalTilDato(DateTime dato)
        {
            switch (Interval)
            {
                case "Week":
                    dato = dato.AddDays(7);
                    break;
                case "TwoWeeks":
                    dato = dato.AddDays(14);
                    break;

                case "FourWeeks":
                    dato = dato.AddDays(7 * 4);
                    break;

                case "Month":
                    dato = dato.AddMonths(1);
                    break;

                case "EightWeeks":
                    dato = dato.AddDays(7 * 8);
                    break;

                case "TwoMonths":
                    dato = dato.AddMonths(2);
                    break;

                case "Quarter":
                    dato = dato.AddMonths(3);
                    break;
                case "HalfYear":
                    dato = dato.AddMonths(6);
                    break;
                case "Year":
                    dato = dato.AddYears(1);
                    break;

                case "TwoYears":
                    dato = dato.AddYears(2);
                    break;
                case "ThreeYears":
                    dato = dato.AddYears(3);
                    break;
                case "FourYears":
                    dato = dato.AddYears(4);
                    break;
                case "FiveYears":
                    dato = dato.AddYears(5);
                    break;
            }

            return dato;
        }
    }
}
