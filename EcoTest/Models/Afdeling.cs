using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoTest.Models
{
    public class Afdeling
    {
        public int Nummer { get; set; }
        public string Navn { get; set; }

        public Afdeling(int nummer, string navn)
        {
            Nummer = nummer;
            Navn = navn;
        }
    }
}
