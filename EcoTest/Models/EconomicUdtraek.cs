using EcoTest.EconomicSOAP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoTest.Models
{
    class EconomicUdtraek
    {
        public SubscriptionData[] Abonnementer { get; set; }
        public SubscriberData[] Abonnenter { get; set; }
        public SubscriptionLineData[] Varelinjer { get; set; }
        public ProductData[] Produkter { get; set; }
        public DebtorData[] Debitorer { get; set; }
        public DepartmentData[] Afdelinger { get; set; }

        public EconomicUdtraek(SubscriptionData[] abonnementer, SubscriberData[] abonnenter, SubscriptionLineData[] varelinjer, ProductData[] produkter, DebtorData[] debitorer, DepartmentData[] afdelinger)
        {
            Abonnementer = abonnementer;
            Abonnenter = abonnenter;
            Varelinjer = varelinjer;
            Produkter = produkter;
            Debitorer = debitorer;
            Afdelinger = afdelinger;
        }
    }

    
}
