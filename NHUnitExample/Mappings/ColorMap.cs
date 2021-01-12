using FluentNHibernate.Mapping;
using NHUnitExample.Entities;

namespace NHUnitExample.Mappings
{
    public class ColorMap : ClassMap<Color>
    {
        public ColorMap()
        {
            Id(o => o.Id).GeneratedBy.SeqHiLo("Seq_Color","10").Column("Id");
            Map(x => x.Name).Length(64).Not.Nullable();
            HasManyToMany(x => x.Products).Cascade.All().Inverse().Table("ProductColors");
        }
    }
}
