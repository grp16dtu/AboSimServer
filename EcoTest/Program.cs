using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoTest.Controllers;

namespace EcoTest
{
    class Program
    {
        static void Main(string[] args)
        {
            int aftalenummer = 387892;
            string brugernavn = "DTU";
            string kodeord = "Trustno1";

            const int ANTAL_SIMULERINGSMAANEDER = 12;
            const int BRUGERINDEX = 1;

            GenererNySimuleringController genererNySimuleringController = new GenererNySimuleringController(aftalenummer, brugernavn, kodeord);
            genererNySimuleringController.GenererNySimulering(ANTAL_SIMULERINGSMAANEDER, BRUGERINDEX);
        }
    }
}
