using System.Collections.Generic;

namespace NHUnitExample.Entities
{
    public class PhoneNumberType
    {
        public PhoneNumberType()
        {
            CustomerPhones=new List<CustomerPhone>();
        }

        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual IList<CustomerPhone> CustomerPhones { get; set; }
    }
}
