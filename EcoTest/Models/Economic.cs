using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using EcoTest.EconomicSOAP;

namespace EcoTest.Models
{

    class Economic
    {
        // E-conomic adgang
        private int aftalenummer;
        private string brugernavn;
        private string kodeord;

        // SOAP klient
        private readonly EconomicWebServiceSoapClient _economicKlient;

        // Rå data fra e-conomic
        private SubscriptionData[] _abonnementerData;
        private SubscriberData[] _abonnenterData;
        private SubscriptionLineData[] _varelinjerData;
        private ProductData[] _produkterData;
        private DebtorData[] _debitorerData;
        private ProjectData[] _projekterData;
        private DepartmentData[] _afdelingerData;
      
        public Economic(int aftalenummer, string brugernavn, string kodeord)
        {
            this.aftalenummer = aftalenummer;
            this.brugernavn = brugernavn;
            this.kodeord = kodeord;
            _economicKlient = new EconomicWebServiceSoapClient();
        }

        private void Forbind()
        {
            ((BasicHttpBinding)_economicKlient.Endpoint.Binding).AllowCookies = true;
            _economicKlient.Connect(aftalenummer, brugernavn, kodeord);
        }

        private void Afbryd()
        {
            _economicKlient.Disconnect();
        }

        /// <summary>
        /// Henter data fra e-conomic. Data er gemt internt og linkes først sammen ved kørsel af "ForbindData".
        /// </summary>
        public void HentData()
        {
            Forbind();
 
            // Abonnementer
            SubscriptionHandle[] abonnementHandlers = _economicKlient.Subscription_GetAll();
            _abonnementerData = _economicKlient.Subscription_GetDataArray(abonnementHandlers);

            // Abonnenter
            SubscriberHandle[] abonnentHandlers = _economicKlient.Subscriber_FindBySubscriptonList(abonnementHandlers);
            _abonnenterData = _economicKlient.Subscriber_GetDataArray(abonnentHandlers);

            // Varelinjer
            SubscriptionLineHandle[] varelinjeHandlers = _economicKlient.SubscriptionLine_FindBySubscriptonList(abonnementHandlers);
            _varelinjerData = _economicKlient.SubscriptionLine_GetDataArray(varelinjeHandlers);


            // Produkter
            ProductHandle[] _produktHandlers = hentProduktHandlers(_varelinjerData); // Manuel opsamling af "handlers"
            _produkterData = _economicKlient.Product_GetDataArray(_produktHandlers);
        
            //Debitorer
            DebtorHandle[] debitorHandlers = hentDebitorHandlers(_abonnenterData); // Manuel opsamling af "handlers"
            _debitorerData = _economicKlient.Debtor_GetDataArray(debitorHandlers);

            //Projekter
            ProjectHandle[] projektHandlers = hentProjektHandlers(_abonnenterData); // Manuel opsamling af "handlers"
            _projekterData = _economicKlient.Project_GetDataArray(projektHandlers);

            // Afdelinger
            DepartmentHandle[] afdelingerHandlers = hentAfdelingHandlers(_produkterData); // Manuel opsamling af "handlers"
            _afdelingerData = _economicKlient.Department_GetDataArray(afdelingerHandlers);

            Afbryd();

        }

        /// <summary>
        /// Forbinder data hentet fra e-conomic. Returnerer en liste af abonnementer med linkede varelinjer og abonnenter.
        /// </summary>
        /// <returns></returns>
        public List<Abonnement> ForbindData()
        {
            //
            // Konverter e-conomic dataobjekter til "egne" dataobjekter. Alt data lægges i opslag og køres kun igennem een gang ved konvertering.
            Dictionary<int, Abonnement> abonnementopslag = new Dictionary<int, Abonnement>();
            Dictionary<string, Debitor> debitoropslag = new Dictionary<string, Debitor>();
            Dictionary<int, Abonnent> abonnentopslag = new Dictionary<int, Abonnent>();
            Dictionary<string, Produkt> produktopslag = new Dictionary<string, Produkt>();
            Dictionary<int, Varelinje> varelinjeopslag = new Dictionary<int, Varelinje>();

            foreach (var abonnementData in _abonnementerData)
            {
                abonnementopslag.Add(abonnementData.Id, new Abonnement(abonnementData.Id, abonnementData.Name, abonnementData.Number, abonnementData.CalendarYearBasis, abonnementData.SubscriptionInterval.ToString(), abonnementData.Collection.ToString()));
            }

            foreach (var debitorData in _debitorerData)
            {
                if (!debitoropslag.ContainsKey(debitorData.Number))
                    debitoropslag.Add(debitorData.Number, new Debitor(debitorData.Address, debitorData.Balance, debitorData.CINumber, debitorData.City, debitorData.Country, debitorData.CreditMaximum, debitorData.Ean, debitorData.Email, debitorData.Name, debitorData.Number, debitorData.PostalCode, debitorData.TelephoneAndFaxNumber));
            }

            foreach (var abonnentData in _abonnenterData)
            {
                Abonnent abonnent = new Abonnent(abonnentData.SubscriberId, debitoropslag[abonnentData.DebtorHandle.Number], abonnentData.DiscountAsPercent, abonnentData.DiscountExpiryDate, abonnentData.EndDate, abonnentData.EndDate, abonnentData.QuantityFactor, abonnentData.PriceIndex, abonnentData.RegisteredDate, abonnentData.SpecialPrice, abonnentData.StartDate);
                abonnentopslag.Add(abonnentData.SubscriberId, abonnent);
                abonnementopslag[abonnentData.SubscriptionHandle.Id].Abonnenter.Add(abonnent);
            }

            foreach (var produktData in _produkterData)
            {
                if (!produktopslag.ContainsKey(produktData.Handle.Number))
                    produktopslag.Add(produktData.Handle.Number, new Produkt(produktData.CostPrice, produktData.Name, produktData.Number, produktData.SalesPrice, produktData.Volume));
            }

            foreach (var varelinjeData in _varelinjerData)
            {
                if (varelinjeData.ProductHandle != null)
                {
                    Varelinje varelinje = new Varelinje(varelinjeData.Id, varelinjeData.Number, varelinjeData.ProductName, varelinjeData.Quantity, varelinjeData.SpecialPrice, produktopslag[varelinjeData.ProductHandle.Number]);
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
        public List<Transaktion> GenererTransaktioner(List<Abonnement> abonnementer, int antalSimuleringsmaaneder, decimal brugerIndex)
        {
            // Data data nu er linket opstår der ikke performance overhead ved generering af transaktioner.
            var transaktioner = new List<Transaktion>();

            DateTime simuleringsdatoStart = FindNaesteFoerste();
            Console.WriteLine(simuleringsdatoStart.ToShortDateString());
            DateTime simuleringsdatoSlut = simuleringsdatoStart.AddMonths(antalSimuleringsmaaneder + 1);
            DateTime simuleringsdatoAktuel;
            
            decimal varelinjepris;
            decimal produktpris;
            decimal? produktAntal;

            foreach (var abonnement in abonnementer) 
            {
                simuleringsdatoAktuel = simuleringsdatoStart;

                Console.WriteLine("{0}",abonnement.Navn);

                //Læg tid til før næste iteration
                //simuleringsdatoAktuel = TilfoejIntervalTilDato(simuleringsdatoAktuel, abonnement.Interval);
                
                while (simuleringsdatoAktuel <= simuleringsdatoSlut && abonnement.Varelinjer.Count != 0) // Kun hvis der ER varelinjer i abonnementet og x antal intervaller ikke har passeret simuleringsperioden
                {
                    foreach (var varelinje in abonnement.Varelinjer)
                    {
                        foreach (var abonnent in abonnement.Abonnenter)
                        {
                            bool abonnementsperiodeErAktiv = (FindSidsteFoerste(abonnent.Startdato) <= simuleringsdatoAktuel) && (abonnent.Slutdato >= simuleringsdatoAktuel);

                            // Generer kun transaktion hvis abonnentens start/slutperiode er aktiv
                            if (abonnementsperiodeErAktiv) 
                            {
                                Console.WriteLine("Varelinje - Abonnentperiode: {1} - {2}, SimDato: {3}",varelinje.Produkt.Navn, abonnent.Startdato.ToShortDateString(), abonnent.Slutdato.ToShortDateString(), simuleringsdatoAktuel.ToShortDateString());

                                produktpris = varelinje.Produkt.Salgpris;
                                produktpris = BeregnProduktpris(produktpris, varelinje.Specielpris, abonnent.Saerpris, abonnent.RabatSomProcent, !ErRabatUdlobet(abonnent.DatoRabatudloeb, simuleringsdatoAktuel), abonnent.Prisindex, brugerIndex);
                                produktAntal = BeregnProduktantal(varelinje.Antal, abonnent.Antalsfaktor);
                                
                                varelinjepris = (decimal)(produktpris * produktAntal);
                               
                                transaktioner.Add(new Transaktion(simuleringsdatoAktuel.Year, simuleringsdatoAktuel.Month, abonnent.Debitor.Nummer, varelinje.Produkt.Nummer, produktAntal, varelinjepris));
                                Console.WriteLine(simuleringsdatoAktuel.ToShortDateString());
                            }
                        }
                    }

                    simuleringsdatoAktuel = TilfoejIntervalTilDato(simuleringsdatoAktuel, abonnement.Interval); //Læg tid til før næste iteration
                }
            }

            return transaktioner;
        }

        
       
        private ProductHandle[] hentProduktHandlers(IEnumerable<SubscriptionLineData> varelinjerData)
        {
            return (from t in varelinjerData where t.ProductHandle != null select t.ProductHandle).ToArray();
        }

        private DebtorHandle[] hentDebitorHandlers(IEnumerable<SubscriberData> abonnenterData)
        {
            return (from t in abonnenterData where t.DebtorHandle != null select t.DebtorHandle).ToArray();
        }

        private ProjectHandle[] hentProjektHandlers(IEnumerable<SubscriberData> abonnenterData)
        {
            return (from subscriber in abonnenterData where subscriber.ProjectHandle != null select subscriber.ProjectHandle).ToArray();
        }

        private DepartmentHandle[] hentAfdelingHandlers(IEnumerable<ProductData> produkterData)
        {
            return (from product in produkterData where product.DepartmentHandle != null select product.DepartmentHandle).ToArray();
        }

        private bool ErRabatUdlobet(DateTime? rabatSlutdato, DateTime aktuelSimuleringsdato)
        {
            if (rabatSlutdato == null)            
                return false;

            return (rabatSlutdato < aktuelSimuleringsdato);
        }

        private decimal? BeregnProduktantal(decimal? produktantal, decimal? antalsfaktor)
        {
            if (antalsfaktor != null) // Gang op ift. Antalsfaktor
                produktantal = produktantal * antalsfaktor;

            return produktantal;
        }

        private decimal BeregnProduktpris(decimal produktpris, decimal? varelinjeSaerpris, decimal? abonnentSaerpris, decimal? rabatSomProcent, bool rabatErUdloebet, decimal? abonnentPrisindex, decimal brugerIndex)
        {
            // Tjek om varelinjen har en særlig produktpris
            if (varelinjeSaerpris != null) 
                produktpris = Convert.ToDecimal(varelinjeSaerpris);

            // Tjek om abonnenten har en særlig produktpris
            if (abonnentSaerpris != null) 
                produktpris = Convert.ToDecimal(abonnentSaerpris);

            // Udregn produktpris med eventuel rabat
            if (rabatSomProcent != null && !rabatErUdloebet) 
                produktpris = produktpris * (100 - Convert.ToDecimal(rabatSomProcent)) / 100;

            // Udregn produktpris med brugerdefineret index
            if (abonnentPrisindex != null) 
                produktpris = produktpris * brugerIndex / Convert.ToDecimal(abonnentPrisindex);

            return produktpris;
        }

        private DateTime TilfoejIntervalTilDato(DateTime simuleringsdato, string interval)
        {
            switch (interval)
            {
                case "Week":
                    simuleringsdato = simuleringsdato.AddDays(7);
                    break;
                case "TwoWeeks":
                    simuleringsdato = simuleringsdato.AddDays(14);
                    break;
                case "Month":
                    simuleringsdato = simuleringsdato.AddMonths(1);
                    break;
                case "Quarter":
                    simuleringsdato = simuleringsdato.AddMonths(3);
                    break;
                case "HalfYear":
                    simuleringsdato = simuleringsdato.AddMonths(6);
                    break;
                case "Year":
                    simuleringsdato = simuleringsdato.AddYears(1);
                    break;
                case "TwoMonths":
                    simuleringsdato = simuleringsdato.AddMonths(2);
                    break;
                case "TwoYears":
                    simuleringsdato = simuleringsdato.AddYears(2);
                    break;
                case "ThreeYears":
                    simuleringsdato = simuleringsdato.AddYears(3);
                    break;
                case "FourYears":
                    simuleringsdato = simuleringsdato.AddYears(4);
                    break;
                case "FiveYears":
                    simuleringsdato = simuleringsdato.AddYears(5);
                    break;
                case "EightWeeks":
                    simuleringsdato = simuleringsdato.AddDays(7 * 8);
                    break;
            }

            return simuleringsdato;
        }

        private DateTime FindNaesteFoerste()
        {
            DateTime nu = DateTime.Now;

            if(nu.Day > 1)
            {
                nu = nu.AddMonths(1);
            }

            return new DateTime(nu.Year, nu.Month, 1);
        }

        private DateTime FindSidsteFoerste(DateTime input)
        {
            return new DateTime(input.Year, input.Month, 1);
        }
    }
}