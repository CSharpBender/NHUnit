using System;
using System.Collections.Generic;

namespace NHUnitExample.Entities
{
    public class CustomerCart
    {
        public CustomerCart()
        {
            Products = new List<Product>();
        }

        public virtual int Id { get; set; }
        public virtual DateTime LastUpdated { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual IList<Product> Products { get; set; }
    }
}
