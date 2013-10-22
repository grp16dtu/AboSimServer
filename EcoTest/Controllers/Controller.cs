using System;
using System.Collections.Generic;
using System.Linq;
using EcoTest.Models;

namespace EcoTest.Controllers
{
    public class Controller
    {
        public void go()
        {
            int aftalenummer = 387892;
            string brugernavn = "DTU";
            string kodeord = "Trustno1";

            const int ANTAL_SIMULERINGSMAANEDER = 12;
            const int BRUGERINDEX = 1;

            Economic economic = new Economic(aftalenummer, brugernavn, kodeord);
            economic.HentData();
            List<Abonnement> forbundneAbonnementer = economic.ForbindData();
            List<Transaktion> transaktioner = economic.GenererTransaktioner(forbundneAbonnementer, ANTAL_SIMULERINGSMAANEDER, BRUGERINDEX);

            MySQL mySql = new MySQL();
            //mySql.InsertTransactions(transaktioner);


               //Read transactions
            Console.WriteLine(transaktioner.Count() + " transaktion(er) over " + ANTAL_SIMULERINGSMAANEDER + " simuleringsmåned(er)");

            foreach (var transaction in transaktioner)
            {
                Console.WriteLine("Dato: " + transaction.Aar + transaction.Maaned + ", DN: " + transaction.Debitornummer + ", PN: " + transaction.Produktnummer + ", Ant: " + transaction.Antal + ", Sum: " + transaction.Beloeb);
            }
            Console.ReadLine();
        }
    }
}
