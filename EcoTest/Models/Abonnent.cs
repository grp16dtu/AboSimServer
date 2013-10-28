using System;

namespace EcoTest.Models
{
    public class Abonnent
    {
        public int AbonnentId { get; set; }
        public Debitor Debitor { get; set; }
        public decimal? RabatSomProcent { get; set; }
        public DateTime? DatoRabatudloeb { get; set; }
        public DateTime Startdato { get; set; }
        public DateTime Slutdato { get; set; }
        public DateTime Registreringsdato { get; set; }
        public DateTime? Ophoer { get; set; }
        public decimal? Antalsfaktor { get; set; }
        public decimal? Prisindex { get; set; }
        public decimal? Saerpris { get; set; }
        

        public Abonnent(int subscriberId, Debitor debtor, decimal? discountAsPercent, DateTime? discountExpiryDate, DateTime endDate, DateTime? expiryDate, decimal? quantityFactor, decimal? priceIndex, DateTime registeredDate, decimal? specialPrice, DateTime startDate)
        {
            AbonnentId = subscriberId;
            Debitor = debtor;
            RabatSomProcent = discountAsPercent;
            DatoRabatudloeb = discountExpiryDate;
            Slutdato = endDate;
            Ophoer = expiryDate;
            Antalsfaktor = quantityFactor;
            Prisindex = priceIndex;
            Registreringsdato = registeredDate;
            Saerpris = specialPrice;
            Startdato = startDate;
        }

        public DateTime EndegyldigSlutdato()
        {
            if (Ophoer != null && Ophoer < Slutdato)    
                return (DateTime)Ophoer;
            else
                return Slutdato;
        }
    }
}
