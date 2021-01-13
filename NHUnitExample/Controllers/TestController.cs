using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NHUnitExample.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Color = NHUnitExample.Entities.Color;

namespace NHUnitExample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private readonly IDbContext _dbContext;

        public TestController(ILogger<TestController> logger, IDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpGet("/initializeDatabase")]
        public async Task<IActionResult> InitializeDatabase(CancellationToken token)
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
            await _dbContext.Countries.InsertManyAsync(countries, token);

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
            await _dbContext.Colors.InsertManyAsync(colors, token);

            //phone number types
            var phoneNumberTypes = new List<PhoneNumberType>
            {
                new PhoneNumberType(){ Name = "Fixed"},
                new PhoneNumberType(){ Name = "Mobile"},
                new PhoneNumberType(){ Name = "Business"},
            };
            await _dbContext.PhoneNumberTypes.InsertManyAsync(phoneNumberTypes, token);
            await _dbContext.SaveChangesAsync(token); // save changes but do not commit transaction (not required in this case)

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
            await _dbContext.Products.InsertManyAsync(products, token);

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
            await _dbContext.Customers.InsertAsync(customer, token);

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
            await _dbContext.Customers.InsertAsync(customer2, token);

            //customer 3 - basic info
            var customer3 = new Customer()
            {
                BirthDate = DateTime.Now.AddYears(-16),
                FirstName = "Mike",
                LastName = "Golden"
            };
            await _dbContext.Customers.InsertAsync(customer3, token);

            await _dbContext.SaveChangesAsync(token);
            await _dbContext.CommitTransactionAsync(token);
            return Ok("Initialization complete");
        }

        [HttpGet("/getAllCustomers")]
        public async Task<IActionResult> GetAllCustomer(CancellationToken token)
        {
            var customers = await _dbContext.WrapQuery(
                                                _dbContext.Customers.All())
                                                .Unproxy() //without this you might download the whole DB
                                                .ListAsync(token);
            return Ok(customers);
        }

        [HttpGet("/getAllForCustomer")]
        public async Task<IActionResult> GetAllForCustomer(CancellationToken token, int customerId = 11)
        {
            //get the customer with all nested data and execute the above query too
            var customer = await _dbContext.Customers.Get(customerId) //returns a wrapper to configure the query
               .Include(c => c.Addresses.Single().Country, //include Addresses in the result using the fastest approach: join, new query, batch fetch. Usage: c.Child1.ChildList2.Single().Child3 which means include Child1 and ChildList2 with ALL Child3 properties
                   c => c.PhoneNumbers.Single().PhoneNumberType, //include all PhoneNumbers with PhoneNumberType
                   c => c.Cart.Products.Single().Colors) //include Cart with Products and details about products
               .Unproxy() //instructs the framework to strip all the proxy classes when the Value is returned
               .ValueAsync(token); //this is where the query(s) get executed

            return Ok(customer);
        }

        [HttpGet("/testProjections")]
        public async Task<IActionResult> TestProjections(CancellationToken token)
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

            var pagedProducts = await _dbContext.WrapQuery(query).ListAsync(token);

            return Ok(new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                PagedProducts = pagedProducts,
                Count = pagedProducts.Count
            });
        }

        [HttpGet("/testMultipleQueries")]
        public async Task<IActionResult> TestMultipleQueries(CancellationToken token)
        {
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
            var customersWithEmptyCart = await customersWithEmptyCartPromise.ListAsync(token);
            //we can unproxy the object at any time
            customersWithEmptyCart = _dbContext.Unproxy(customersWithEmptyCart);

            // List/ListAsync shouldn't hit the Database unless it doesn't support futures. Ex: Oracle
            var expensiveProducts = await expensiveProductsPromise.ListAsync(token);
            var lastActiveCustomer = await lastActiveCustomerPromise.ValueAsync(token);
            return Ok(new
            {
                LastActiveCustomer = lastActiveCustomer,
                CustomersWithEmptyCart = customersWithEmptyCart,
                Top10ExpensiveProducts = expensiveProducts,
            });
        }

        [HttpGet("/testSqlQuery")]
        public async Task<IActionResult> TestSqlQuery(CancellationToken token, int customerId = 11)
        {
            var sqlQuery = @"select Id as CustomerId,
                                    Concat(FirstName,' ',LastName) as FullName,
                                    BirthDate
                             from ""Customer""
                             where Id= :customerId";
            var customResult = await _dbContext.ExecuteScalarAsync<SqlQueryCustomResult>(sqlQuery, new { customerId }, token);
            return Ok(customResult);
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
