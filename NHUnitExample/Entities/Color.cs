using System.Collections.Generic;

namespace NHUnitExample.Entities
{
    public class Color
    {
        public Color()
        {
            Products = new List<Product>();
        }

        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual IList<Product> Products { get; set; }
    }
}
