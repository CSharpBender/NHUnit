namespace NHUnitExample.Entities
{
    public class Address
    {
        public Address() { }

        public virtual int Id { get; set; }
        //public virtual int CountryId { get; set; }
        public virtual Country Country { get; set; }
        public virtual string City { get; set; }
        public virtual string StreetName { get; set; }
        public virtual int StretNumber { get; set; }
        //public virtual int CustomerId { get; set; }
        public virtual Customer Customer { get; set; }
    }
}
