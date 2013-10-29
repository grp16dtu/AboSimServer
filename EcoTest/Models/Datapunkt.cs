using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoTest.Models
{
    public class Datapunkt
    {
        public decimal? Antal { get; set; }
        public decimal? DKK { get; set; }

        public DateTime AarMaaned { get; set; }
        public string Produktnavn { get; set; }
        public string Debitornavn { get; set; }
        public string Afdelingsnavn { get; set; }

        public static Datapunkt TidDKK(DateTime aarMaaned, decimal antal, decimal dkk)
        {
            Datapunkt datapunkt = new Datapunkt();
            datapunkt.AarMaaned = aarMaaned;
            datapunkt.Antal = antal;
            datapunkt.DKK = dkk;
            return datapunkt;
        }
    }
}
