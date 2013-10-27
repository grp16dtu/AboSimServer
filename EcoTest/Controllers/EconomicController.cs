﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using EcoTest.EconomicSOAP;
using EcoTest.Models;

namespace EcoTest.Controllers
{
    class EconomicController
    {
        // E-conomic adgang
        private int _aftalenummer;
        private string _brugernavn;
        private string _kodeord;

        // SOAP klient
        private readonly EconomicWebServiceSoapClient _economicKlient;

        // Rå data fra e-conomic
        private SubscriptionData[] _abonnementerData;
        private SubscriberData[] _abonnenterData;
        private SubscriptionLineData[] _varelinjerData;
        private ProductData[] _produkterData;
        private DebtorData[] _debitorerData;
        private DepartmentData[] _afdelingerData;
      
        public EconomicController(int aftalenummer, string brugernavn, string kodeord)
        {
            this._aftalenummer = aftalenummer;
            this._brugernavn = brugernavn;
            this._kodeord = kodeord;
            this._economicKlient = new EconomicWebServiceSoapClient();
        }

        private void Forbind()
        {
            ((BasicHttpBinding)_economicKlient.Endpoint.Binding).AllowCookies = true;
            _economicKlient.Connect(_aftalenummer, _brugernavn, _kodeord);
        }

        private void Afbryd()
        {
            _economicKlient.Disconnect();
        }

        /// <summary>
        /// Henter data fra e-conomic. Data er gemt internt og linkes først sammen ved kørsel af "ForbindData".
        /// </summary>
        public void HentDataFraEconomic()
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
            ProductHandle[] _produktHandlers = HentProduktHandlers(_varelinjerData); // Manuel opsamling af "handlers"
            _produkterData = _economicKlient.Product_GetDataArray(_produktHandlers);
        
            //Debitorer
            DebtorHandle[] debitorHandlers = HentDebitorHandlers(_abonnenterData); // Manuel opsamling af "handlers"
            _debitorerData = _economicKlient.Debtor_GetDataArray(debitorHandlers);

            // Afdelinger
            //DepartmentHandle[] afdelingerHandlers = HentAfdelingHandlers(_produkterData); // Manuel opsamling af "handlers"
            DepartmentHandle[] afdelingerHandlers = _economicKlient.Department_GetAll();
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
            Dictionary<int, Abonnent> abonnentopslag = new Dictionary<int, Abonnent>();
            Dictionary<int, Afdeling> afdelingsopslag = new Dictionary<int, Afdeling>();
            Dictionary<string, Debitor> debitoropslag = new Dictionary<string, Debitor>();
            Dictionary<string, Vare> produktopslag = new Dictionary<string, Vare>();
            Dictionary<int, Varelinje> varelinjeopslag = new Dictionary<int, Varelinje>();

            foreach (var abonnementData in _abonnementerData)
            {
                abonnementopslag.Add(abonnementData.Id, new Abonnement(abonnementData.Id, abonnementData.Name, abonnementData.Number, abonnementData.CalendarYearBasis, abonnementData.SubscriptionInterval.ToString(), abonnementData.Collection.ToString()));
            }

            foreach (var afdelingsData in _afdelingerData)
            {
                afdelingsopslag.Add(afdelingsData.Number, new Afdeling(afdelingsData.Number, afdelingsData.Name));
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
                // Evt. afdeling
                Afdeling afdeling = null;
                if (produktData.DepartmentHandle != null)
                    afdeling = afdelingsopslag[produktData.DepartmentHandle.Number];
                
                if (!produktopslag.ContainsKey(produktData.Handle.Number))
                    produktopslag.Add(produktData.Handle.Number, new Vare(produktData.CostPrice, produktData.Name, produktData.Number, produktData.SalesPrice, produktData.Volume, afdeling));
            }

            foreach (var varelinjeData in _varelinjerData)
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
        public List<Transaktion> GenererTransaktioner(List<Abonnement> abonnementer, int antalSimuleringsmaaneder, decimal brugerIndex)
        {
            var transaktioner = new List<Transaktion>();

            DateTime simuleringsdatoStart = SaetKommendeFoerste();
            DateTime simuleringsdatoSlut = simuleringsdatoStart.AddMonths(antalSimuleringsmaaneder + 1);
            DateTime simuleringsdato;
            //Console.WriteLine(simuleringsdatoStart.ToShortDateString());
            
            foreach (var abonnement in abonnementer) 
            {
                simuleringsdato = simuleringsdatoStart;

                while (AbonnementErSimulerbart(simuleringsdato, simuleringsdatoSlut, abonnement)) 
                {
                    foreach (var varelinje in abonnement.Varelinjer)
                    {
                        foreach (var abonnent in abonnement.Abonnenter)
                        {
                            bool abonnementsperiodeErAktiv = ErAbonnentperiodeAktiv(abonnent, simuleringsdato);

                            if (abonnementsperiodeErAktiv) 
                            {
                                Console.WriteLine("Varelinje - Abonnentperiode: {1} - {2}, SimDato: {3}",varelinje.Produkt.Navn, abonnent.Startdato.ToShortDateString(), abonnent.Slutdato.ToShortDateString(), simuleringsdato.ToShortDateString());
                                Console.WriteLine(simuleringsdato.ToShortDateString());

                                decimal produktpris = BeregnProduktpris(abonnement, varelinje, abonnent, simuleringsdato,!ErRabatUdlobet(abonnent.DatoRabatudloeb, simuleringsdato), brugerIndex);
                                decimal? produktantal = BeregnProduktantal(varelinje, abonnent);
                                decimal varelinjepris = (decimal)(produktpris * produktantal);
                                int? afdelingsnummer = HentAfdelingsnummer(varelinje);                               
                                transaktioner.Add(new Transaktion(simuleringsdato.Year, simuleringsdato.Month, abonnent.Debitor.Nummer, varelinje.Produkt.Nummer, produktantal, varelinjepris, afdelingsnummer));
                            }
                        }
                    }
                    simuleringsdato = TilfoejIntervalTilDato(simuleringsdato, abonnement.Interval); 
                }
            }
            return transaktioner;
        }

        
       
        private ProductHandle[] HentProduktHandlers(IEnumerable<SubscriptionLineData> varelinjerData)
        {
            return (from t in varelinjerData where t.ProductHandle != null select t.ProductHandle).ToArray();
        }

        private DebtorHandle[] HentDebitorHandlers(IEnumerable<SubscriberData> abonnenterData)
        {
            return (from t in abonnenterData where t.DebtorHandle != null select t.DebtorHandle).ToArray();
        }

        private int? HentAfdelingsnummer(Varelinje varelinje)
        {
            if (varelinje.Afdeling != null)
                return varelinje.Afdeling.Nummer;

            else if (varelinje.Produkt.Afdeling != null)
                return varelinje.Produkt.Afdeling.Nummer;

            else return null;
        }

        private bool ErRabatUdlobet(DateTime? rabatSlutdato, DateTime aktuelSimuleringsdato)
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
            decimal produktpris = varelinje.Produkt.Salgpris;

            // Forholdsmæssig opkrævning overskriver fuld opkrævning
            if (abonnement.OpkraevesForholdsmaessigt())
            {
                DateTime naesteIntervalStartdato = TilfoejIntervalTilDato(simuleringsdato, abonnement.Interval);

                double antalDageIndtilEndegyldigSlutdato = (naesteIntervalStartdato - simuleringsdato).TotalDays;
                double antalDageIInterval = (naesteIntervalStartdato - simuleringsdato).TotalDays;
                

                //Simulering før start dato
                if (abonnent.Startdato > simuleringsdato)
                {
                    antalDageIndtilEndegyldigSlutdato = (naesteIntervalStartdato - abonnent.Startdato).TotalDays;
                }
                    

                if (naesteIntervalStartdato > abonnent.EndegyldigSlutdato()) // Slutdato stopper før simulering
                {
                    if (abonnent.Startdato <= simuleringsdato)
                        antalDageIndtilEndegyldigSlutdato = (abonnent.EndegyldigSlutdato() - simuleringsdato).TotalDays + 1; // + 1 for at få første dag med

                    else
                        antalDageIndtilEndegyldigSlutdato = (naesteIntervalStartdato - abonnent.Startdato).TotalDays + 1; // + 1 for at få første dag med
                }


                Decimal forhold = (Decimal)(antalDageIndtilEndegyldigSlutdato / antalDageIInterval);
                produktpris = produktpris * forhold;

                Console.WriteLine("Forholdsmæssigt abonnement - Interval: {0}, Rest: {1}, Pris: {2}", antalDageIInterval, antalDageIndtilEndegyldigSlutdato, produktpris);
            }
            

            // Særlig varelinje produktpris
            if (varelinje.Saerpris != null)
                produktpris = Convert.ToDecimal(varelinje.Saerpris);

            // Særlig abonnent produktpris
            if (abonnent.Saerpris != null)
                produktpris = Convert.ToDecimal(abonnent.Saerpris);

            // Eventuel rabat
            if (abonnent.RabatSomProcent != null && !rabatErUdloebet)
                produktpris = produktpris * (100 - Convert.ToDecimal(abonnent.RabatSomProcent)) / 100;

            // Brugerdefineret index på pris
            if (abonnent.Prisindex != null)
                produktpris = produktpris * brugerIndex / Convert.ToDecimal(abonnent.Prisindex);

            return produktpris;
        }

        private bool ErAbonnentperiodeAktiv(Abonnent abonnent, DateTime simuleringsdato)
        {
            bool abonnentperiodeErAktiv;

            // Abonnement startdato
            abonnentperiodeErAktiv = (SaetTidligereFoerste(abonnent.Startdato) <= simuleringsdato);

            // Abonnement slutdato 
            abonnentperiodeErAktiv = abonnentperiodeErAktiv && (abonnent.Slutdato >= simuleringsdato);

            // Abonnent udløbsdato 
            abonnentperiodeErAktiv = abonnentperiodeErAktiv && (abonnent.Ophoer >= simuleringsdato);
            return abonnentperiodeErAktiv;
        }

        private bool AbonnementErSimulerbart(DateTime simuleringsdato, DateTime simuleringsdatoSlut, Abonnement abonnement)
        {
            return (simuleringsdato <= simuleringsdatoSlut) && (abonnement.Varelinjer.Count != 0);
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

                case "FourWeeks":
                    simuleringsdato = simuleringsdato.AddDays(7 * 4);
                    break;

                case "Month":
                    simuleringsdato = simuleringsdato.AddMonths(1);
                    break;

                case "EightWeeks":
                    simuleringsdato = simuleringsdato.AddDays(7 * 8);
                    break;

                case "TwoMonths":
                    simuleringsdato = simuleringsdato.AddMonths(2);
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
            }

            return simuleringsdato;
        }

        private DateTime SaetKommendeFoerste()
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