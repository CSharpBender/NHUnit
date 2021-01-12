using FluentNHibernate.Mapping;
using NHUnitExample.Entities;

namespace NHUnitExample.Mappings
{
    public class CountryMap : ClassMap<Country>
    {
        public CountryMap()
        {
            Id(o => o.Id).GeneratedBy.SeqHiLo("Seq_Country","10").Column("Id");
            Map(x => x.Name).Length(64).Not.Nullable();
            HasMany(x => x.Addresses).KeyColumn("CountryId").Cascade.All().Inverse();
        }
    }
}
