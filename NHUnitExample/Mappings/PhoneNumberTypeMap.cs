using FluentNHibernate.Mapping;
using NHUnitExample.Entities;


namespace NHUnitExample.Mappings
{
    public class PhoneNumberTypeMap : ClassMap<PhoneNumberType>
    {
        public PhoneNumberTypeMap()
        {
            Id(o => o.Id).GeneratedBy.SeqHiLo("Seq_PhoneNumberType", "10").Column("Id");
            Map(p => p.Name).Length(64).Not.Nullable();
            HasMany(p => p.CustomerPhones).KeyColumn("PhoneNumberTypeId").Inverse().Cascade.All();
        }
    }
}
