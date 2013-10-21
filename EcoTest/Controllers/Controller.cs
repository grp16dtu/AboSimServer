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

            Economic economic = new Economic(aftalenummer, brugernavn, kodeord);
            economic.HentData();
            List<Abonnement> forbundneAbonnementer = economic.ForbindData();
            List<Transaktion> transaktioner = economic.GenererTransaktioner(forbundneAbonnementer, 2);

            MySQL mySql = new MySQL();
            //mySql.InsertTransactions(transaktioner);


               //Read transactions
            Console.WriteLine(transaktioner.Count());

            foreach (var transaction in transaktioner)
            {
                Console.WriteLine("Date: " + transaction.Aar + transaction.Maaned + ", DN: " + transaction.Debitornummer + ", PN: " + transaction.Produktnummer + ", Qt: " + transaction.Antal + ", Amount: " + transaction.Beloeb);
            }
            Console.ReadLine();
        }
    }
}
