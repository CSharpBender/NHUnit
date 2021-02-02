using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NHibernate.Util;
using NHUnitExample.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Color = NHUnitExample.Entities.Color;

namespace NHUnitExample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestNHUnitController : ControllerBase
    {
        private readonly ILogger<TestNHUnitController> _logger;
        private readonly IDbContext _dbContext;

        public TestNHUnitController(ILogger<TestNHUnitController> logger, IDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        /// <summary>
        /// You need to setup the database before testing any other endpoint.
        /// At every project start the DB schema is recreated: Check Startup.cs - BuildSchema(Configuration config)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("/initializeDatabase")]
        public async Task<IActionResult> InitializeDatabase(CancellationToken cancellationToken)
        {
            if (_dbContext.Countries.All().Any())
                return Ok("Already initialized");

            _dbContext.CommandTimeout = 60;
            _dbContext.BeginTransaction();
            //countries
            var countries = new List<Country>
            {
                new Country(){ Name = "USA"},
                new Country(){ Name = "China"},
                new Country(){ Name = "India"},
                new Country(){ Name = "Romania"},
            };
            await _dbContext.Countries.InsertManyAsync(countries, cancellationToken);

            //colors
            var colors = new List<Color>
            {
                new Color(){ Name = "White"},
                new Color(){ Name = "Black"},
                new Color(){ Name = "Red"},
                new Color(){ Name = "Green"},
                new Color(){ Name = "Blue"},
                new Color(){ Name = "Grey"},
            };
            await _dbContext.Colors.InsertManyAsync(colors, cancellationToken);

            //phone number types
            var phoneNumberTypes = new List<PhoneNumberType>
            {
                new PhoneNumberType(){ Name = "Fixed"},
                new PhoneNumberType(){ Name = "Mobile"},
                new PhoneNumberType(){ Name = "Business"},
            };
            await _dbContext.PhoneNumberTypes.InsertManyAsync(phoneNumberTypes, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken); // save changes but do not commit transaction (not required in this case)

            var random = new Random(15);

            //products
            var products = Enumerable.Range(1, 1000).Select(i =>

                 new Product()
                 {
                     Name = "Product " + i,
                     Colors = new List<Color> { colors.ElementAt(random.Next(0, colors.Count - 1)) },
                     Price = random.Next(100, 10000) / 100m
                 }
            ).ToList();
            await _dbContext.Products.InsertManyAsync(products, cancellationToken);

            //customers
            var customer = new Customer()
            {
                BirthDate = DateTime.Now.AddYears(-40),
                FirstName = "John",
                LastName = "Doe"
            };
            customer.AddAddress(new Address()
            {
                City = "New York",
                Country = countries.First(),
                StreetName = "Cooper Square",
                StretNumber = 20
            });
            customer.AddPhoneNumber(new CustomerPhone()
            {
                PhoneNumber = "123456789",
                PhoneNumberType = phoneNumberTypes.First()
            });
            var cart = new CustomerCart();
            customer.SetCart(cart);
            Enumerable.Range(1, random.Next(1, 20))
                      .Select(x => products.ElementAt(random.Next(0, products.Count - 1)))
                      .ToList()
                      .ForEach(p => cart.Products.Add(p));
            cart.LastUpdated = DateTime.UtcNow;
            await _dbContext.Customers.InsertAsync(customer, cancellationToken);

            //customer 2 - empty cart
            var customer2 = new Customer()
            {
                BirthDate = DateTime.Now.AddYears(-36),
                FirstName = "CSharp",
                LastName = "Bender"
            };
            customer2.AddAddress(new Address()
            {
                City = "Bucharest",
                Country = countries.Last(),
                StreetName = "Universitatii Street",
                StretNumber = 13
            });
            customer2.AddPhoneNumber(new CustomerPhone()
            {
                PhoneNumber = "123456789",
                PhoneNumberType = phoneNumberTypes.First()
            });
            customer2.SetCart(new CustomerCart());
            await _dbContext.Customers.InsertAsync(customer2, cancellationToken);

            //customer 3 - basic info
            var customer3 = new Customer()
            {
                BirthDate = DateTime.Now.AddYears(-16),
                FirstName = "Mike",
                LastName = "Golden"
            };
            await _dbContext.Customers.InsertAsync(customer3, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _dbContext.CommitTransactionAsync(cancellationToken);
            return Ok("Initialization complete");
        }

        /// <summary>
        /// A simple example of query wrap. You can join multiple entity types
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("/getAllCustomers")]
        public async Task<IActionResult> GetAllCustomer(CancellationToken cancellationToken)
        {
            var customers = await _dbContext.WrapQuery(
                                                _dbContext.Customers.All())
                                                .Unproxy() //without this you might download the whole DB
                                                .ListAsync(cancellationToken);
            return Ok(customers);
        }

        /// <summary>
        /// A simple example for loading an entity with all the nested children.
        /// Behind the scenes it's using join, new future queries or batch fetching.
        /// At the end it's stripping all the NHibernate proxy classes so you don't need to worry about casting or unwanted lazy loading.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="customerId"></param>
        /// <returns></returns>
        [HttpGet("/getAllForCustomer")]
        public async Task<IActionResult> GetAllForCustomer(CancellationToken cancellationToken, int customerId = 11)
        {
            //get the customer with all nested data and execute the above query too
            var customer = await _dbContext.Customers.Get(customerId) //returns a wrapper to configure the query
               .Include(c => c.Addresses.Single().Country, //include Addresses in the result using the fastest approach: join, new query, batch fetch. Usage: c.Child1.ChildList2.Single().Child3 which means include Child1 and ChildList2 with ALL Child3 properties
                   c => c.PhoneNumbers.Single().PhoneNumberType, //include all PhoneNumbers with PhoneNumberType
                   c => c.Cart.Products.Single().Colors) //include Cart with Products and details about products
               .Unproxy() //instructs the framework to strip all the proxy classes when the Value is returned
               .ValueAsync(cancellationToken); //this is where the query(s) get executed

            return Ok(customer);
        }


        /// <summary>
        /// A simple example for loading a list of entities by id with all the nested children.
        /// It's similar to IRepository.Get(id), the only difference is that it expects a collection of ids
        /// </summary>
        /// <param name="token"></param>
        /// <param name="customerIds"></param>
        /// <returns></returns>
        [HttpGet("/getAllForCustomerIds")]
        public async Task<IActionResult> GetAllForCustomerIds(CancellationToken token, List<int> customerIds = null)
        {
            if (customerIds?.Count == 0)
            {
                customerIds = new List<int>() { 11, 12 };
            }

            //get the customer with all nested data and execute the above query too
            var customer = await _dbContext.Customers.GetMany(customerIds) //returns a wrapper to configure the query
                .Include(c => c.Addresses.Single().Country, //include Addresses in the result using the fastest approach: join, new query, batch fetch. Usage: c.Child1.ChildList2.Single().Child3 which means include Child1 and ChildList2 with ALL Child3 properties
                    c => c.PhoneNumbers.Single().PhoneNumberType, //include all PhoneNumbers with PhoneNumberType
                    c => c.Cart.Products.Single().Colors) //include Cart with Products and details about products
                .Unproxy() //instructs the framework to strip all the proxy classes when the Value is returned
                .ListAsync(token); //this is where the query(s) get executed

            return Ok(customer);
        }

        /// <summary>
        /// A simple example of projecting a query
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("/testProjections")]
        public async Task<IActionResult> TestProjections(CancellationToken cancellationToken)
        {
            int pageNumber = 3;
            int pageSize = 10;
            var query = _dbContext.Products.All()
                .OrderBy(p => p.Price)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Name,
                    p.Price,
                    Colors = p.Colors.Select(c => c.Name)
                });

            var pagedProducts = await _dbContext.WrapQuery(query).ListAsync(cancellationToken);

            return Ok(new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                PagedProducts = pagedProducts,
                Count = pagedProducts.Count
            });
        }

        /// <summary>
        /// Execute multiple queries in a single server trip.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("/testMultipleQueries")]
        public async Task<IActionResult> TestMultipleQueries(CancellationToken cancellationToken)
        {
            //notify framework we want the total number of products
            var redProductsCountPromise = _dbContext.WrapQuery(
                                            _dbContext.Products.All().Where(p => p.Colors.Any(c => c.Name == "Red")))
                                                   .Count()
                                                   .Deferred();

            // notify framework we need top 10 expensive products - no execution
            var expensiveProductsPromise = _dbContext.WrapQuery(
                    _dbContext.Products.All().OrderByDescending(p => p.Price).Take(10))
                    .Unproxy() //we don't need to pull the whole database when serializing the response
                    .Deferred();

            // notify framework we need the customer that just added something in the cart - no execution
            var lastActiveCustomerPromise = _dbContext.WrapQuery(
                _dbContext.Customers.All()
                    .Where(c => c.Cart.Products.Any())
                    .OrderByDescending(c => c.Cart.LastUpdated)
                    .Take(1)) //we need only one record returned from the DB
                    .Unproxy() //in case you don't need the whole database
                    .Deferred();

            // notify framework we need all the customers with empty cart - no execution
            var customersWithEmptyCartPromise = _dbContext.WrapQuery(
                _dbContext.Customers.All()
                    .Where(c => !c.Cart.Products.Any())
                )
                .Include(c => c.Addresses)
                .Include(c => c.PhoneNumbers)
                //.Unproxy() //omitted on purpose, it returns a NHibernate proxy object
                .Deferred();

            //execute all deferred queries. If the Database doesn't support futures (Ex: Oracle) the query will get executed when Value() is called
            var customersWithEmptyCart = await customersWithEmptyCartPromise.ListAsync(cancellationToken);
            //we can unproxy the object at any time
            customersWithEmptyCart = _dbContext.Unproxy(customersWithEmptyCart);

            // List/ListAsync shouldn't hit the Database unless it doesn't support futures. Ex: Oracle
            var expensiveProducts = await expensiveProductsPromise.ListAsync(cancellationToken);
            var lastActiveCustomer = await lastActiveCustomerPromise.ValueAsync(cancellationToken);
            var numberOfRedProducts = await redProductsCountPromise.ValueAsync(cancellationToken);
            return Ok(new
            {
                NumberOfRedProducts = numberOfRedProducts,
                Top10ExpensiveProducts = expensiveProducts,
                LastActiveCustomer = lastActiveCustomer,
                CustomersWithEmptyCart = customersWithEmptyCart
            });
        }

        /// <summary>
        /// Increase price by 5 for all products that are cheaper than 100 without loading them in memory.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("/testConditionalUpdate")]
        public async Task<IActionResult> TestConditionalUpdate(CancellationToken cancellationToken)
        {
            var productCount = _dbContext.Products.UpdateWhereAsync(p => p.Price < 100, p => new { Price = p.Price + 5 }, cancellationToken);
            return Ok($"Updated {productCount} products.");
        }

        /// <summary>
        /// Execute your own SQL script using: ExecuteListAsync, ExecuteScalarAsync, ExecuteNonQueryAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="customerId"></param>
        /// <returns></returns>
        [HttpGet("/testScalarQuerySql")]
        public async Task<IActionResult> TestScalarQuerySql(CancellationToken cancellationToken, int customerId = 11)
        {
            var sqlQuery = @"select Id as CustomerId,
                                    Concat(FirstName,' ',LastName) as FullName,
                                    BirthDate
                             from ""Customer""
                             where Id= :customerId";
            var customResult = await _dbContext.ExecuteScalarAsync<SqlQueryCustomResult>(sqlQuery, new { customerId }, cancellationToken);
            return Ok(customResult);
        }

        /// <summary>
        /// Execute your own SQL script to get all customers prices using ExecuteListAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("/testListQuerySql")]
        public async Task<IActionResult> TestListQuerySql(CancellationToken cancellationToken)
        {
            var sqlQuery = @"select Id, FirstName, LastName, BirthDate  from ""Customer""";
            var customers = await _dbContext.ExecuteListAsync<Customer>(sqlQuery, null, cancellationToken);
            return Ok(customers);
        }


        /// <summary>
        /// Execute your own SQL script to update product prices using ExecuteNonQueryAsync
        /// </summary>
        /// <param name="price"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("/testNonQuerySql")]
        public async Task<IActionResult> TestNonQuerySql(CancellationToken cancellationToken, int price = 5)
        {
            var sqlQuery = @"update ""Product"" set price=price+1 where price< :productPrice";
            await _dbContext.ExecuteNonQueryAsync(sqlQuery, new { productPrice = price }, cancellationToken);
            return Ok();
        }


        /// <summary>
        /// Execute a procedure or multiple queries which return multiple results.
        ///  You'll need to cast/select the right collection.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="customerId"></param>
        /// <returns></returns>
        [HttpGet("/testDataSetQuery")]
        public async Task<IActionResult> TestDataSetQuery(CancellationToken cancellationToken, int customerId = 11)
        {
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
            var customerCount = (long)customResult[1].First(); //the type must match: it will fail with int

            return Ok(new
            {
                Customer = customer,
                CustomerCount = customerCount
            });
        }

        class SqlQueryCustomResult
        {
            public SqlQueryCustomResult() { }
            public int CustomerId { get; set; }
            public string FullName { get; set; }
            public DateTime BirthDate { get; set; }
        }
    }
}
