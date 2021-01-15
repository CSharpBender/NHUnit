namespace NHUnitExample.Entities
{
    public class CustomerPhone
    {
        public CustomerPhone() { }

        public virtual int Id { get; set; }
        public virtual string PhoneNumber { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual PhoneNumberType PhoneNumberType { get; set; }
    }
}
