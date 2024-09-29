using Bogus;
using JornadaMilhas.Dados;
using JornadaMilhasV1.Modelos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.MsSql;

namespace JornadaMilhas.Test.Integracao
{
    public class ContextoFixture : IAsyncLifetime
        
    {
        public JornadaMilhasContext Context { get; private set; }
        private readonly MsSqlContainer _msSqlContainer =
            new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        //para poder criar os containers usamos IAsyncLifeTime do Xunit
        public async Task InitializeAsync()
        {
            //inicializa o container
            await _msSqlContainer.StartAsync();
            var options = new DbContextOptionsBuilder<JornadaMilhasContext>
              ()
              //.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=JornadaMilhas;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False")
              .UseSqlServer(_msSqlContainer.GetConnectionString())
              .Options;

            Context = new JornadaMilhasContext(options);
            Context.Database.Migrate();
        }

        public void CriaDadosFake()
        {

            Periodo periodo = new PeriodoFaker().Build();

            var rota = new Rota("Curitiba", "São Paulo");

            var fakerOferta = new Faker<OfertaViagem>()
                .CustomInstantiator(f => new OfertaViagem(
                    rota,
                    new PeriodoFaker().Build() ,
                    100 * f.Random.Int(1, 100))
                )
                .RuleFor(o => o.Desconto, f => 40)
                .RuleFor(o => o.Ativa, f => true);

            var lista = fakerOferta.Generate(200);
            Context.OfertasViagem.AddRange(lista);
            Context.SaveChanges();
        }

        public async Task LimpaDadosDoBanco()
        {
            //Limpa os dados 1 a 1
            //Context.OfertasViagem.RemoveRange(Context.OfertasViagem);
            //Context.Rotas.RemoveRange(Context.Rotas);
            //await Context.SaveChangesAsync();

            Context.Database.ExecuteSqlRaw("DELETE FROM OfertasViagem");
            Context.Database.ExecuteSqlRaw("DELETE FROM Rotas");

        }
        public async Task DisposeAsync()
        {
            await _msSqlContainer.StopAsync();
        }
    }
}
