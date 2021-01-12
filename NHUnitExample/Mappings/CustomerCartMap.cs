using FluentNHibernate.Mapping;
using NHUnitExample.Entities;


namespace NHUnitExample.Mappings
{
    public class CustomerCartMap : ClassMap<CustomerCart>
    {
        public CustomerCartMap()
        {
            Id(o => o.Id).GeneratedBy.SeqHiLo("Seq_Cart", "10").Column("Id");
            Map(x => x.LastUpdated);
            HasManyToMany(x => x.Products).Cascade.All().Table("CartProducts");
            HasOne(x => x.Customer).PropertyRef(c => c.Cart).Constrained();
        }
    }
}
