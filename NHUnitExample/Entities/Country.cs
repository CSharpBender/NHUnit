using System.Collections.Generic;

namespace NHUnitExample.Entities
{
    public class Country
    {
        public Country()
        {
            Addresses = new List<Address>();
        }

        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual IList<Address> Addresses { get; set; }
    }
}
