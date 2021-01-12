using FluentNHibernate.Mapping;
using NHUnitExample.Entities;


namespace NHUnitExample.Mappings
{
    public class CustomerPhoneMap : ClassMap<CustomerPhone>
    {
        public CustomerPhoneMap()
        {
            Id(o => o.Id).GeneratedBy.SeqHiLo("Seq_CustomerPhone", "10").Column("Id");
            Map(x => x.PhoneNumber).Length(64).Not.Nullable();
            References(o => o.Customer).Column("CustomerId");
            References(o => o.PhoneNumberType).Column("PhoneNumberTypeId");
        }
    }
}
