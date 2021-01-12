using NHUnit;
using NHUnitExample.Entities;

namespace NHUnitExample
{
    public interface IDbContext : IUnitOfWork
    {
        IRepository<Customer> Customers { get; }
        IRepository<Product> Products { get; }
        IRepository<Color> Colors { get; }
        IRepository<Country> Countries { get; }
        IRepository<PhoneNumberType> PhoneNumberTypes { get; set; }
    }
}
