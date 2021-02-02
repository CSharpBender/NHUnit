# NHUnit
NHUnit is a simple but powerful Unit of Work and Repository pattern implementation for NHibernate which fixes proxy issues and simplifies child nodes loading.
If at some point you decide to change the ORM, you just need to provide your own implementation for just two interfaces instead of rewriting the whole application.
NHUnit provides sync and async versions for each method.

## License
MIT: Enjoy, share, contribute to the project.


## Where can I get it?

Install using the [NuGet package](https://www.nuget.org/packages/NHUnit):

```
dotnet add package NHUnit
```


## Constraints
NHUnit uses NHibernate so there are a few constrains:
 - All the properties on your POCO classes must be virtual and use interfaces. For example IList<T> instead of List<T>
 - You must define the NHibernate mappings or use a tool to generate them from existing database.
 - Your configuration must use lazy loading and batch fetching

The example project should get you going in no time with NHUnit (and NHibernate).


## How do I use it?
** Check [FluentNHibernateExample](https://github.com/CSharpBender/NHUnit/tree/master/NHUnitExample/) project **
- In `appsetting.json` update `NhibernateConfig.ConnectionString`
- In `Startup.cs` update the `CreateSessionFactory` method with your desired Database type. It uses PostgreSql.
- Depending on the database type you might need to update the classes from the `Mappings` folder
- Start the project and test the endpoints in the displayed order. The database will be recreated every time the projects starts unless you comment `BuildSchema` from `Startup.cs`
- You should replace the NHUnit project reference with the [package](https://www.nuget.org/packages/NHUnit)

You can either inject IUnitOfWork and IRepository<T>'s in your controller or create your own implementation similar to EntityFramework. The example contains the custom implementation.


# Documentation

## Create your Unit of work
Register [NHibernate.ISessionFactory](https://github.com/CSharpBender/NHUnit/blob/master/NHUnitExample/Startup.cs#L63) into your dependency injection and define the interface for your Database.

```csharp
public interface IDbContext : IUnitOfWork
{
    IRepository<Customer> Customers { get; }
    IRepository<Product> Products { get; }
}

public class DbContext : UnitOfWork, IDbContext
{
    public DbContext(ISessionFactory sessionFactory) : base(sessionFactory, true) { }

    public IRepository<Customer> Customers { get; set; }
    public IRepository<Product> Products { get; set; }
}
```

The framework automatically initializes all your `IRepository` properties when the second constructor parameter is true, otherwise you will need to do this yourself:

```csharp
public DbContext(ISessionFactory sessionFactory) : base(sessionFactory) {
    Customers = new Repository<Customer>();
    Products = new Repository<Product>();
}
```


## Eager loading
Using lambda expressions you can specify which child properties should be populated. The framework will determine the fastest approach to load the data: using join, future queries or by using batch fetching.
Depending on the number of returned rows and the depth you might need to finetune your queries, but in most cases the framework takes the best decision.

```csharp
var customer = await _dbContext.Customers.Get(customerId) //returns a wrapper to configure the query
               .Include(c => c.Addresses.Single().Country, //include Addresses and Country
                        c => c.PhoneNumbers.Single().PhoneNumberType) //include all PhoneNumbers with PhoneNumberType
               .Unproxy() //instructs the framework to strip all the proxy classes when the Value is returned
               .Deferred() //instructs the framework to delay execution
               .ValueAsync(token); //this is where the query(s) get executed
```

The expression `c.Addresses.Single().Country` will load all the nested child objects. 
`Addresses` is a colection and you need to use `Addresses.Single()` to include the nested children. The whole collection will be populated, not just the first item.


## Unproxy
You can use Unproxy from IUnitOfWork, ISingleEntityWrapper, IMultipleEntityWrapper or IEntityListWrapper

```csharp
var customer = await _dbContext.Customers
                    .Get(customerId) //returns a wrapper to configure the query
                    .Unproxy() //instructs the framework to strip all the proxy classes when the Value is returned
                    .ValueAsync(token); //this is where the query(s) get executed
```

An unproxied object will contain the foreign key ids (One to One and Many to One relations).
For example in the above query the Cart property will be instantiated and popuplated with the Id field.


## Deferred Execution
Using `Deferred()` you can execute multiple queries in a single server trip.
Bellow is an example with 3 different queries that get executed in one server trip.

```csharp
//product count future
var prodCountP = _dbContext.WrapQuery(_dbContext.Products.All())
                           .Count()
                           .Deferred();

//most expensive 10 products future
var expProdsP = _dbContext.WrapQuery(_dbContext.Products.All().OrderByDescending(p => p.Price).Take(10))
                          .Deferred();

//get customer by id - executes all queries
var customer = await _dbContext.Customers
                               .Get(customerId)
                               .Deferred()
                               .ValueAsync();
var prodCount = await prodCountP.ValueAsync(); //returns one value
var expProds = await expProdsP.ListAsync(); //returns list
```


## Conditional Queries
If you need to update or delete a set of records that meet a condition, you don't need to load them in memory. Just write an expression and it will be evaluated immediately.
You should always run your commands inside a transaction with `BeginTransaction()` and `CommitTransactionAsync`.

### UpdateWhereAsync
Increase price by 5 for all products that are less than 100, without loading them in memory.

```csharp
await _dbContext.Products.UpdateWhereAsync(p => p.Price < 100, p => new { Price = p.Price + 5 });
```

### DeleteWhereAsync
Delete products that are too cheap, without loading them in memory.

```csharp
await _dbContext.Products.DeleteWhereAsync(p => p.Price < 2);
```


## Transactions
IUnitOfWork exposes:
- BeginTransaction
- CommitTransactionAsync - pushes changes and commits transaction 
- RollbackTransactionAsync
- SaveChangesAsync - pushes changes to DB but doesn't commit transaction (if any)
- ClearCache - evict all loaded instances and cancel all pending saves, updates and deletions


## Procedures/Queries
In some rare cases you need to execute your own queries/procedures and NHUnit provides this functionality:
- ExecuteListAsync : return a list of values

```csharp
var sqlQuery = @"select Id, FirstName, LastName, BirthDate  from ""Customer""";
var customers = await _dbContext.ExecuteListAsync<Customer>(sqlQuery, null, cancellationToken);
```

- ExecuteScalarAsync : return single value/object

```csharp
var sqlQuery = @"select Id as CustomerId,
                        Concat(FirstName,' ',LastName) as FullName,
                        BirthDate
                        from ""Customer""
                        where Id= :customerId";
var customResult = await _dbContext.ExecuteScalarAsync<SqlQueryCustomResult>(sqlQuery, new { customerId = 11 });
```

- ExecuteNonQueryAsync : no value returned

```csharp
var sqlQuery = @"update ""Product"" set price=price+1 where price< :productPrice";
await _dbContext.ExecuteNonQueryAsync(sqlQuery, new { productPrice = 5 }, cancellationToken);
```

- ExecuteMultipleQueriesAsync : return multiple result sets

```csharp
var sqlQuery = @"select Id as CustomerId,
                        Concat(FirstName,' ',LastName) as FullName,
                        BirthDate
                 from ""Customer""
                 where Id= :customerId;

                select count(*) Count from ""Customer"";";
var customResult = await _dbContext.ExecuteMultipleQueriesAsync(sqlQuery, //query
    new { customerId }, //parameters: property name must be the same as the parameter
    cancellationToken,
    typeof(SqlQueryCustomResult), //first result type
    typeof(long));//second result type

//The results are returned in order in their own collection.
var customer = (SqlQueryCustomResult)customResult[0].FirstOrDefault(); //we might not have any results
var customerCount = (long)customResult[1].First(); //the type must match
```