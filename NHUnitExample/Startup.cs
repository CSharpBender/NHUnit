using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Conventions.AcceptanceCriteria;
using FluentNHibernate.Conventions.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Tool.hbm2ddl;
using NHUnit;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace NHUnitExample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.Formatting = Formatting.Indented;
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            //supress the implicit validation filter
            services.Configure<ApiBehaviorOptions>(opt =>
            {
                opt.SuppressModelStateInvalidFilter = true;
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Version = "v1",
                    Title = "FluentNHibernate Test",
                    Description = "Test NHibernate & FluentNHibernate on .netcore",
                    TermsOfService = "None"
                });
            });

            services.AddSingleton<ISessionFactory>(CreateSessionFactory());
            services.AddScoped<IDbContext, DbContext>();
        }


        private ISessionFactory CreateSessionFactory()
        {
            var connectionString = Configuration.GetSection("NhibernateConfig").Get<NhibernateConfig>().ConnectionString;
            //var dbCfg = OracleManagedDataClientConfiguration.Oracle10.Dialect<Oracle12cDialect>().ConnectionString(db => db.Is(connectionString));
            var dbCfg = PostgreSQLConfiguration.Standard.Dialect<PostgreSQL83Dialect>().ConnectionString(db => db.Is(connectionString));
            return Fluently.Configure()
                .Database(dbCfg)
                .Cache(cfg => cfg.Not.UseSecondLevelCache())
                .Mappings(cfg =>
                    cfg.FluentMappings.AddFromAssemblyOf<DbContext>()
                    .Conventions
                    .Add(
                        ConventionBuilder.HasManyToMany.When(
                            c => c.Expect(p => p.TableName, Is.Not.Set), // when this is true
                            c => c.Table(string.Concat(c.EntityType.Name, c.ChildType.Name)) // do this
                        ),
                        OptimisticLock.Is(x => x.Version())))
                .ExposeConfiguration(cfg =>
                {
#if DEBUG
                    cfg.DataBaseIntegration(db =>
                    {
                        db.LogFormattedSql = true;
                        db.LogSqlInConsole = true;
                    });
#endif
                    cfg.SetProperty(Environment.BatchSize, "100");
                    cfg.SetProperty(Environment.DefaultBatchFetchSize, "20");
                    cfg.SetProperty(Environment.OrderInserts, "true");
                    cfg.SetProperty(Environment.OrderUpdates, "true");
                    cfg.SetProperty(Environment.BatchVersionedData, "true");
                    BuildSchema(cfg);   //<-- uncomment this to create DB schema
                })
                .BuildSessionFactory();

        }
        private static void BuildSchema(Configuration config)
        {
            // this NHibernate tool takes a configuration (with mapping info in)
            // and exports a database schema from it
            new SchemaExport(config).Create(false, true);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Serilog"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Test Service");
            });

            app.UseMvc();
        }
    }
}