using FluentNHibernate.Mapping;
using NHUnitExample.Entities;


namespace NHUnitExample.Mappings
{
    public class CustomerMap : ClassMap<Customer>
    {
        public CustomerMap()
        {
            Id(o => o.Id).GeneratedBy.SeqHiLo("Seq_Customer", "10").Column("Id");
            Map(o => o.FirstName).Column("FirstName").Length(64).Not.Nullable();
            Map(o => o.LastName).Column("LastName").Length(64).Not.Nullable();
            Map(o => o.BirthDate);
            HasMany(o => o.Addresses).KeyColumn("CustomerId").Cascade.All().Inverse();
            HasMany(o => o.PhoneNumbers).KeyColumn("CustomerId").Cascade.All().Inverse();
            References(x => x.Cart).Unique().Column("CustomerId").Cascade.All();
        }
    }
}
