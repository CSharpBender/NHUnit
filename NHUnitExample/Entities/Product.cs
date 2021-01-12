using System;
using System.Collections.Generic;

namespace NHUnitExample.Entities
{
    public class Product
    {
        public Product()
        {
            Colors = new List<Color>();
            CustomerCarts = new List<CustomerCart>();
        }

        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual DateTime RowVersion { get; set; }
        public virtual IList<Color> Colors { get; set; }
        public virtual decimal Price { get; set; }
        public virtual IList<CustomerCart> CustomerCarts { get; set; }

    }
}
