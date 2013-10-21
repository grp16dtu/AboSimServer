﻿using System;
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
       

        public List<Transaktion> GenererTransaktioner(List<Abonnement> abonnementer, int antalMndr)
        {
            // Data data nu er linket opstår der ikke performance overhead ved generering af transaktioner.
            var transaktioner = new List<Transaktion>();

            DateTime simuleringsdatoStart = DateTime.Now;
            DateTime simuleringsdatoSlut = simuleringsdatoStart.AddMonths(antalMndr);
            DateTime simuleringsdatoAktuel;
            
            decimal produktPris;
            decimal? produktAntal;
            decimal inputIndex = 1;

            foreach (var abonnement in abonnementer) 
            {
                simuleringsdatoAktuel = simuleringsdatoStart;

                /*
                //Kalenderår
                if (abonnement.KalenderAar)
                    simuleringsdatoAktuel = new DateTime(simuleringsdatoAktuel.Year, simuleringsdatoAktuel.Month, 1);
                 * */
                

                Console.WriteLine(abonnement.Navn);

                //Læg tid til før næste iteration
                simuleringsdatoAktuel = tilfojTidTilSimuleringsdato(simuleringsdatoAktuel, abonnement.Interval);
                while(simuleringsdatoAktuel <= simuleringsdatoSlut)
                {
                    foreach (var varelinje in abonnement.Varelinjer)
                    {
                        foreach (var abonnent in abonnement.Abonnenter)
                        {
                            Console.WriteLine(varelinje.Produkt.Navn);

                            produktPris = varelinje.Produkt.Salgpris;

                            // Tjek om varelinje har en særpris
                            if (varelinje.Specielpris != null)
                            {
                                produktPris = Convert.ToDecimal(varelinje.Specielpris);
                            }

                            // Tjek om abonnent har særpris
                            if (abonnent.Saerpris != null)
                            {
                                produktPris = Convert.ToDecimal(abonnent.Saerpris);
                            }

                            // Udregn produktpris med rabat
                            if (abonnent.RabatSomProcent != null && !erDatoUdlobet(abonnent.DatoRabatudloeb, simuleringsdatoAktuel))
                            {
                                produktPris = produktPris * (100 - Convert.ToDecimal(abonnent.RabatSomProcent)) / 100;
                            }

                            // Udregn produktpris med index
                            if (abonnent.Prisindex != null)
                            {
                                produktPris = produktPris * inputIndex / Convert.ToDecimal(abonnent.Prisindex);
                            }


                            produktAntal = varelinje.Antal;

                            // Gang op ift. Antalsfaktor
                            if (abonnent.Antalsfaktor != null)
                            {
                                produktAntal = produktAntal * abonnent.Antalsfaktor;
                            }

                            transaktioner.Add(new Transaktion(simuleringsdatoAktuel.Year, simuleringsdatoAktuel.Month, abonnent.Debitor.Nummer, varelinje.Produkt.Nummer, produktAntal, produktPris));
                        }
                    }
                    //Læg tid til før næste iteration
                    simuleringsdatoAktuel = tilfojTidTilSimuleringsdato(simuleringsdatoAktuel, abonnement.Interval);
                }
            }

            return transaktioner;
        }

        //private decimal Spececialpris

       
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

        private bool erDatoUdlobet(DateTime? datoAtTjekke, DateTime aktuelDato)
        {
            if (datoAtTjekke == null)
            {
                return false;
            }

            return datoAtTjekke < aktuelDato;
        }

        private DateTime tilfojTidTilSimuleringsdato(DateTime simuleringsdato, string interval)
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
    }
}