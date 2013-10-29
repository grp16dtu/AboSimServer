using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using EcoTest.EconomicSOAP;
using EcoTest.Models;
using System.Security.Authentication;

namespace EcoTest.Controllers
{
    class GenererNySimuleringController
    {
        private int _economicAftalenummer;
        private string _economicBrugernavn;
        private string _economicKodeord;

        private readonly EconomicWebServiceSoapClient _economicSOAPklient;

        public GenererNySimuleringController(int economicAftalenummer, string economicBrugernavn, string economicKodeord)
        {
            this._economicAftalenummer = economicAftalenummer;
            this._economicBrugernavn = economicBrugernavn;
            this._economicKodeord = economicKodeord;
            this._economicSOAPklient = new EconomicWebServiceSoapClient();
        }

        public void GenererNySimulering(int antalSimuleringsmaaneder, int brugerIndex)
        {
            try
            {
                EconomicUdtraek economicUdtraek = HentAbonnementsrelateretData();
                List<Abonnement> abonnementer = ForbindData(economicUdtraek);
                List<Transaktion> transaktioner = GenererTransaktioner(abonnementer, antalSimuleringsmaaneder, brugerIndex);

                MySQL mySql = new MySQL();
                mySql.InsertTransactions(transaktioner);

                List<Datapunkt> datapunkter =  mySql.HentAlt();

                //Udlæs transaktioner
                Console.WriteLine("Transaktioner: " + transaktioner.Count() + " transaktion(er) over " + antalSimuleringsmaaneder + " simuleringsmåned(er)");

                foreach (var transaction in transaktioner)
                {
                    Console.WriteLine("Dato: {0}, DN: {2}, PN: {3}, Ant: {4}, Sum: {5}, Afd: {6}", transaction.AarMaaned.ToShortDateString(), transaction.AarMaaned, transaction.Debitornummer, transaction.Varenummer, transaction.Antal, transaction.Beloeb, transaction.Afdelingsnummer);
                }

                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }

        private void TilslutEconomic()
        {
            try
            {
                ((BasicHttpBinding)_economicSOAPklient.Endpoint.Binding).AllowCookies = true;
                _economicSOAPklient.Connect(_economicAftalenummer, _economicBrugernavn, _economicKodeord);
            }
            catch (Exception)
            {
                Exception exception = new Exception("Fejl i forbindelse. Tjek aftalenummer, brugernavn og kodeord.");
                throw exception;
            }
        }

        private void AfbrydEconomic()
        {
            _economicSOAPklient.Disconnect();
        }

        /// <summary>
        /// Henter data fra e-conomic.
        /// </summary>
        /// <returns>Economic dataudtræk med relevans til abonnementer.</returns>
        private EconomicUdtraek HentAbonnementsrelateretData() 
        {
            try
            {
                TilslutEconomic();

                // Abonnementer
                SubscriptionHandle[] abonnementHandlers = _economicSOAPklient.Subscription_GetAll();
                SubscriptionData[] abonnementerData = _economicSOAPklient.Subscription_GetDataArray(abonnementHandlers);

                // Abonnenter
                SubscriberHandle[] abonnentHandlers = _economicSOAPklient.Subscriber_FindBySubscriptonList(abonnementHandlers);
                SubscriberData[] abonnenterData = _economicSOAPklient.Subscriber_GetDataArray(abonnentHandlers);

                // Varelinjer
                SubscriptionLineHandle[] varelinjeHandlers = _economicSOAPklient.SubscriptionLine_FindBySubscriptonList(abonnementHandlers);
                SubscriptionLineData[] varelinjerData = _economicSOAPklient.SubscriptionLine_GetDataArray(varelinjeHandlers);

                // Produkter
                ProductHandle[] produktHandlers = HentProduktHandlers(varelinjerData); // Manuel opsamling af "handlers"
                ProductData[] produkterData = _economicSOAPklient.Product_GetDataArray(produktHandlers);

                //Debitorer
                DebtorHandle[] debitorHandlers = HentDebitorHandlers(abonnenterData); // Manuel opsamling af "handlers"
                DebtorData[] debitorerData = _economicSOAPklient.Debtor_GetDataArray(debitorHandlers);

                // Afdelinger
                DepartmentHandle[] afdelingerHandlers = _economicSOAPklient.Department_GetAll();
                DepartmentData[] afdelingerData = _economicSOAPklient.Department_GetDataArray(afdelingerHandlers);

                AfbrydEconomic();

                return new EconomicUdtraek(abonnementerData, abonnenterData, varelinjerData, produkterData, debitorerData, afdelingerData);
            }

            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Forbinder dataudtræk fra e-conomic. Returnerer en liste af abonnementer med linkede varelinjer og abonnenter.
        /// </summary>
        /// <returns></returns>
        private List<Abonnement> ForbindData(EconomicUdtraek economicDataudtraek)
        {
            // Konverter e-conomic dataobjekter til "egne" dataobjekter. Alt data lægges i opslag og køres kun igennem een gang ved konvertering.
            Dictionary<int, Abonnement> abonnementopslag = new Dictionary<int, Abonnement>();
            Dictionary<int, Abonnent> abonnentopslag = new Dictionary<int, Abonnent>();
            Dictionary<int, Afdeling> afdelingsopslag = new Dictionary<int, Afdeling>();
            Dictionary<string, Debitor> debitoropslag = new Dictionary<string, Debitor>();
            Dictionary<string, Vare> produktopslag = new Dictionary<string, Vare>();
            Dictionary<int, Varelinje> varelinjeopslag = new Dictionary<int, Varelinje>();

            foreach (var abonnementData in economicDataudtraek.Abonnementer)
            {
                abonnementopslag.Add(abonnementData.Id, new Abonnement(abonnementData.Id, abonnementData.Name, abonnementData.Number, abonnementData.CalendarYearBasis, abonnementData.SubscriptionInterval.ToString(), abonnementData.Collection.ToString()));
            }

            foreach (var afdelingsData in economicDataudtraek.Afdelinger)
            {
                afdelingsopslag.Add(afdelingsData.Number, new Afdeling(afdelingsData.Number, afdelingsData.Name));
            }

            foreach (var debitorData in economicDataudtraek.Debitorer)
            {
                if (!debitoropslag.ContainsKey(debitorData.Number))
                    debitoropslag.Add(debitorData.Number, new Debitor(debitorData.Address, debitorData.Balance, debitorData.CINumber, debitorData.City, debitorData.Country, debitorData.CreditMaximum, debitorData.Ean, debitorData.Email, debitorData.Name, debitorData.Number, debitorData.PostalCode, debitorData.TelephoneAndFaxNumber));
            }

            foreach (var abonnentData in economicDataudtraek.Abonnenter)
            {
                Abonnent abonnent = new Abonnent(abonnentData.SubscriberId, debitoropslag[abonnentData.DebtorHandle.Number], abonnentData.DiscountAsPercent, abonnentData.DiscountExpiryDate, abonnentData.EndDate, abonnentData.ExpiryDate, abonnentData.QuantityFactor, abonnentData.PriceIndex, abonnentData.RegisteredDate, abonnentData.SpecialPrice, abonnentData.StartDate);
                abonnentopslag.Add(abonnentData.SubscriberId, abonnent);
                abonnementopslag[abonnentData.SubscriptionHandle.Id].Abonnenter.Add(abonnent);
            }

            foreach (var produktData in economicDataudtraek.Produkter)
            {
                // Evt. afdeling
                Afdeling afdeling = null;
                if (produktData.DepartmentHandle != null)
                    afdeling = afdelingsopslag[produktData.DepartmentHandle.Number];
                
                if (!produktopslag.ContainsKey(produktData.Handle.Number))
                    produktopslag.Add(produktData.Handle.Number, new Vare(produktData.CostPrice, produktData.Name, produktData.Number, produktData.SalesPrice, produktData.Volume, afdeling));
            }

            foreach (var varelinjeData in economicDataudtraek.Varelinjer)
            {
                //Evt. afdeling
                Afdeling afdeling = null;
                if (varelinjeData.DepartmentHandle != null)
                    afdeling = afdelingsopslag[varelinjeData.DepartmentHandle.Number];

                if (varelinjeData.ProductHandle != null)
                {
                    Varelinje varelinje = new Varelinje(varelinjeData.Id, varelinjeData.Number, varelinjeData.ProductName, varelinjeData.Quantity, varelinjeData.SpecialPrice, produktopslag[varelinjeData.ProductHandle.Number], afdeling);
                    abonnementopslag[varelinjeData.Id].Varelinjer.Add(varelinje); 

                    if (!varelinjeopslag.ContainsKey(varelinjeData.Id))
                        varelinjeopslag.Add(varelinjeData.Id, varelinje); 
                }
            }

            return abonnementopslag.Values.ToList();
        }
       
        /// <summary>
        /// Genererer en liste af transaktioner på baggrund af den hægtede data. Listen kan indeholde redundant data som skal optimeres.
        /// </summary>
        /// <param name="abonnementer">Liste af abonnementer hentet vha. "ForbindData" metoden.</param>
        /// <param name="antalSimuleringsmaaneder">Antal måneder der ønskes simuleret over.</param>
        /// <param name="brugerIndex">Index til 1 til afgørelsen af produktpris.</param>
        /// <returns>Liste af transaktioner klar til lagring i database.</returns>
        private List<Transaktion> GenererTransaktioner(List<Abonnement> abonnementer, int antalSimuleringsmaaneder, decimal brugerIndex)
        {
            var transaktioner = new List<Transaktion>();

            DateTime simuleringsdatoStart = SaetNaesteFoerste();
            DateTime simuleringsdatoSlut = simuleringsdatoStart.AddMonths(antalSimuleringsmaaneder + 1);
            DateTime simuleringsdato;
            
            foreach (var abonnement in abonnementer) 
            {
                simuleringsdato = simuleringsdatoStart;

                while (AbonnementErSimulerbart(simuleringsdato, simuleringsdatoSlut, abonnement)) 
                {
                    foreach (var varelinje in abonnement.Varelinjer)
                    {
                        foreach (var abonnent in abonnement.Abonnenter)
                        {
                            bool abonnementperiodeErAktiv = AbonnentperiodeErAktiv(abonnent, simuleringsdato);

                            if (abonnementperiodeErAktiv) 
                            {
                                Console.WriteLine("Varelinje - Abonnentperiode: {1} - {2}, SimDato: {3}",varelinje.Produkt.Navn, abonnent.Startdato.ToShortDateString(), abonnent.Slutdato.ToShortDateString(), simuleringsdato.ToShortDateString());
                                Console.WriteLine(simuleringsdato.ToShortDateString());

                                decimal produktpris = BeregnProduktpris(abonnement, varelinje, abonnent, simuleringsdato,RabatErUdlobet(abonnent.DatoRabatudloeb, simuleringsdato), brugerIndex);
                                decimal? produktantal = BeregnProduktantal(varelinje, abonnent);
                                decimal varelinjepris = (decimal)(produktpris * produktantal);
                                int? afdelingsnummer = HentAfdelingsnummer(varelinje);                               
                                transaktioner.Add(new Transaktion(simuleringsdato, abonnent.Debitor.Nummer, varelinje.Produkt.Nummer, produktantal, varelinjepris, afdelingsnummer));
                            }
                        }
                    }
                    simuleringsdato = abonnement.LaegIntervalTilDato(simuleringsdato); 
                }
            }
            return transaktioner;
        }

        private ProductHandle[] HentProduktHandlers(IEnumerable<SubscriptionLineData> varelinjerData)
        {           
            Dictionary<string, ProductHandle> produktHandlers = new Dictionary<string,ProductHandle>();

            foreach (SubscriptionLineData varelinjeData in varelinjerData)
            {
                if (varelinjeData.ProductHandle != null && !produktHandlers.ContainsKey(varelinjeData.ProductHandle.Number))
                {
                    produktHandlers.Add(varelinjeData.ProductHandle.Number, varelinjeData.ProductHandle);
                }
            }

            return produktHandlers.Values.ToArray<ProductHandle>();
        }

        private DebtorHandle[] HentDebitorHandlers(IEnumerable<SubscriberData> abonnenterData)
        {
            Dictionary<string, DebtorHandle> debitorHandlers = new Dictionary<string,DebtorHandle>();

            foreach (SubscriberData abonnentData in abonnenterData)
            {
                if (abonnentData.DebtorHandle != null && !debitorHandlers.ContainsKey(abonnentData.DebtorHandle.Number))
                {
                    debitorHandlers.Add(abonnentData.DebtorHandle.Number, abonnentData.DebtorHandle);
                }
            }

            return debitorHandlers.Values.ToArray<DebtorHandle>();
        }

        private int? HentAfdelingsnummer(Varelinje varelinje)
        {
            if (varelinje.Afdeling != null)
                return varelinje.Afdeling.Nummer;

            else if (varelinje.Produkt.Afdeling != null)
                return varelinje.Produkt.Afdeling.Nummer;

            else return null;
        }

        private bool RabatErUdlobet(DateTime? rabatSlutdato, DateTime aktuelSimuleringsdato)
        {
            if (rabatSlutdato == null)            
                return false;

            return (rabatSlutdato < aktuelSimuleringsdato);
        }

        private decimal? BeregnProduktantal(Varelinje varelinje, Abonnent abonnent)
        {
            decimal? produktantal = varelinje.Antal;

            if (abonnent.Antalsfaktor != null) 
                produktantal = produktantal * abonnent.Antalsfaktor;

            return produktantal;
        }

        private decimal BeregnProduktpris(Abonnement abonnement,  Varelinje varelinje, Abonnent abonnent, DateTime simuleringsdato, bool rabatErUdloebet, decimal brugerIndex)
        {
            // Fuld opkrævning 
            decimal varepris = varelinje.Produkt.Salgpris;

            // Særlig varelinje produktpris
            if (varelinje.Saerpris != null)
                varepris = Convert.ToDecimal(varelinje.Saerpris);

            // Særlig abonnent produktpris
            if (abonnent.Saerpris != null)
                varepris = Convert.ToDecimal(abonnent.Saerpris);

            // Forholdsmæssig opkrævning 
            if (abonnement.OpkraevesForholdsmaessigt())
            {
                DateTime naesteIntervalStartdato = abonnement.LaegIntervalTilDato(simuleringsdato);

                double interval = (naesteIntervalStartdato - simuleringsdato).TotalDays;
                double dageStartSlut = interval;
                
                // Hvis startdato er senere end simuleringsdato
                if (abonnent.Startdato > simuleringsdato)
                    dageStartSlut = (naesteIntervalStartdato - abonnent.Startdato).TotalDays + 1;
                
                    
                // Hvis nuværende simuleringsinterval overskrider endegyldig slutdato for abonnent
                if (naesteIntervalStartdato > abonnent.EndegyldigSlutdato()) 
                {
                    // Hvis simuleringsdato er senere end abonnentens startdato
                    if (abonnent.Startdato <= simuleringsdato)
                        dageStartSlut = (abonnent.EndegyldigSlutdato() - simuleringsdato).TotalDays + 1;

                    // Hvis simuleringsdatoen er tidligere end startdatoen
                    else
                        dageStartSlut = (naesteIntervalStartdato - abonnent.Startdato).TotalDays + 1; 
                }

                Decimal forhold = (Decimal)(dageStartSlut / interval);
                varepris = varepris * forhold;

                Console.WriteLine("Forhold: " + forhold);
                Console.WriteLine("Forholdsmæssigt abonnement - Interval: {0}, Rest: {1}, Pris: {2}", interval, dageStartSlut, varepris);
            }

            // Eventuel rabat
            if (abonnent.RabatSomProcent != null && !rabatErUdloebet)
                varepris = varepris * (100 - Convert.ToDecimal(abonnent.RabatSomProcent)) / 100;

            // Brugerdefineret index på pris
            if (abonnent.Prisindex != null)
                varepris = varepris * brugerIndex / Convert.ToDecimal(abonnent.Prisindex);

            return varepris;
        }

        private bool AbonnentperiodeErAktiv(Abonnent abonnent, DateTime simuleringsdato)
        {
            // Er simulerings dato indenfor abonnentens start/slut interval
            return (SaetTidligereFoerste(abonnent.Startdato) <= simuleringsdato) && (abonnent.EndegyldigSlutdato() >= simuleringsdato);
        }

        private bool AbonnementErSimulerbart(DateTime simuleringsdato, DateTime simuleringsdatoSlut, Abonnement abonnement)
        {
            return (simuleringsdato <= simuleringsdatoSlut) && (abonnement.Varelinjer.Count != 0);
        }

        private DateTime SaetNaesteFoerste()
        {
            DateTime aktuelDato = DateTime.Now;

            if(aktuelDato.Day > 1)
                aktuelDato = aktuelDato.AddMonths(1);

            return new DateTime(aktuelDato.Year, aktuelDato.Month, 1);
        }

        private DateTime SaetTidligereFoerste(DateTime dato)
        {
            return new DateTime(dato.Year, dato.Month, 1);
        }
    }
}