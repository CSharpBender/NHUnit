# NHUnit
NHUnit is a simple but powerful Unit of Work and Repository pattern implementation for NHibernate which fixes proxy issues and simplifies child nodes loading.
If at some point you decide to change the ORM, you just need to provide your own implementation for just two interfaces instead of rewriting the whole application.
NHUnit provides sync and async versions for each method.

##License
MIT: Enjoy, share, contribute to the project.


## Where can I get it?

Install using the [NuGet package](http://nuget.org):

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
* Check FluentNHibernateExample project
- In `appsetting.json` update `NhibernateConfig.ConnectionString`
- In `Startup.cs` update the `CreateSessionFactory` method with your desired Database type. It uses PostgreSql.
- Depending on the database type you might need to update the classes from the `Mappings` folder
- Start the project and test the endpoints in the displayed order. The database will be recreated every time the projects starts unless you comment `BuildSchema` from `Startup.cs`

You can either inject IUnitOfWork and IRepository<T>'s in your controller or create your own implementation similar to EntityFramework. The example contains the custom implementation.


# Documentation

## Eager loading
Using lambda expressions you can specify which child properties should be populated. The framework will determine the fastest approach to load the data: using join, future queries or by using batch fetching.
Depending on the number of returned rows and the depth you might need to finetune your queries, but in most of the cases the framework takes the best decision.

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
You can use IUnitOfWork.Unproxy, ISingleEntityWrapper.Unproxy or IEntityListWrapper.Unproxy

```csharp
var customer = await _dbContext.Customers
                    .Get(customerId) //returns a wrapper to configure the query
                    .Unproxy() //instructs the framework to strip all the proxy classes when the Value is returned
                    .ValueAsync(token); //this is where the query(s) get executed
```

## Transactions
IUnitOfWork exposes:
- BeginTransaction
- CommitTransactionAsync - pushes changes and commits transaction 
- RollbackTransactionAsync
- SaveChangesAsync - pushes changes to DB but doesn't commit transaction (if any)
- ClearCache - evict all loaded instances and cancel all pending saves, updates and deletions


## Procedures/Queries
In some rare cases you need to execute your own queries and NHUnit provides this functionality:
- ExecuteListAsync
- ExecuteScalarAsync
- ExecuteNonQueryAsync
