using FluentNHibernate.Mapping;
using NHUnitExample.Entities;


namespace NHUnitExample.Mappings
{
    public class AddressMap : ClassMap<Address>
    {
        public AddressMap()
        {
            Id(o => o.Id).GeneratedBy.SeqHiLo("Seq_Address", "10").Column("Id");
            Map(x => x.City).Length(64).Not.Nullable();
            Map(x => x.StreetName).Length(64).Not.Nullable();
            Map(x => x.StretNumber).Not.Nullable();
            References(x => x.Customer).Column("CustomerId");
            References(x => x.Country).Column("CountryId");
        }
    }
}
