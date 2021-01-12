using NHibernate;
using NHUnit;
using NHUnitExample.Entities;

namespace NHUnitExample
{
    public class DbContext : UnitOfWork, IDbContext
    {
        public DbContext(ISessionFactory sessionFactory) : base(sessionFactory, true) { }

        public IRepository<Customer> Customers { get; set; }
        public IRepository<Product> Products { get; set; }
        public IRepository<Color> Colors { get; set; }
        public IRepository<Country> Countries { get; set; }
        public IRepository<PhoneNumberType> PhoneNumberTypes { get; set; }
    }
}
