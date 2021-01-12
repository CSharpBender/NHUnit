using FluentNHibernate.Mapping;
using NHUnitExample.Entities;


namespace NHUnitExample.Mappings
{
    public class ProductMap : ClassMap<Product>
    {
        public ProductMap()
        {
            Id(o => o.Id).GeneratedBy.SeqHiLo("Seq_Product", "10").Column("Id");
            Map(x => x.Name).Length(64).Not.Nullable();
            Map(p => p.Price).CustomSqlType("decimal").Precision(10).Scale(4);
            Version(o => o.RowVersion).CustomSqlType("timestamp");
            HasManyToMany(x => x.CustomerCarts).Cascade.All().Inverse().Table("CartProducts");
            HasManyToMany(x => x.Colors).Cascade.All().Table("ProductColors");
        }
    }
}
