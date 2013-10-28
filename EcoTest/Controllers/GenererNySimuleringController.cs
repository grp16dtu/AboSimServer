using System;
using System.Collections.Generic;
using System.Linq;
using EcoTest.Models;

namespace EcoTest.Controllers
{
    public class GenererNySimuleringController
    {
        public void GenererNySimulering()
        {
            int aftalenummer = 387892;
            string brugernavn = "DTU";
            string kodeord = "Trustno1";

            const int ANTAL_SIMULERINGSMAANEDER = 12;
            const int BRUGERINDEX = 1;

            EconomicController economicController = new EconomicController(aftalenummer, brugernavn, kodeord);
            EconomicUdtraek economicUdtraek = economicController.HentData();
            List<Abonnement> abonnementer = economicController.ForbindData(economicUdtraek);
            List<Transaktion> transaktioner = economicController.GenererTransaktioner(abonnementer, ANTAL_SIMULERINGSMAANEDER, BRUGERINDEX);

            MySQL mySql = new MySQL();
            //mySql.InsertTransactions(transaktioner);

            //Udlæs transaktioner
            Console.WriteLine(transaktioner.Count() + " transaktion(er) over " + ANTAL_SIMULERINGSMAANEDER + " simuleringsmåned(er)");

            foreach (var transaction in transaktioner)
            {
                Console.WriteLine("Dato: {0}{1}, DN: {2}, PN: {3}, Ant: {4}, Sum: {5}, Afd: {6}", transaction.Aar, transaction.Maaned, transaction.Debitornummer, transaction.Varenummer, transaction.Antal, transaction.Beloeb, transaction.Afdelingsnummer);
            }

            Console.ReadLine();
        }
    }
}
