namespace EcoTest.Models
{
    public class Debitor
    {
        public string Adresse { get; set; }
        public decimal Balance { get; set; }
        public string CINummer { get; set; }
        public string By { get; set; }
        public string Land { get; set; }
        public decimal? KreditMaximum { get; set; }
        public string EAN { get; set; }
        public string Email { get; set; }
        public string Navn { get; set; }
        public string Nummer { get; set; }
        public string Postnummer { get; set; }
        public string TlfOgFaxNummer { get; set; }
        


        public Debitor(string address, decimal balance, string ciNumber, string city, string country, decimal? creditMaximum, string ean, string email, string name, string number, string postalCode, string telephoneAndFaxNumber)
        {
            this.Adresse = address;
            this.Balance = balance;
            this.CINummer = ciNumber;
            this.By = city;
            this.Land = country;
            this.KreditMaximum = creditMaximum;
            this.EAN = ean;
            this.Email = email;
            this.Navn = name;
            this.Nummer = number;
            this.Postnummer = postalCode;
            this.TlfOgFaxNummer = telephoneAndFaxNumber;
           
        }
    }
}
