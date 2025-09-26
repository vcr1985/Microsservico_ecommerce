using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using MMLib.SwaggerForOcelot.DependencyInjection;
using MMLib.SwaggerForOcelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Adiciona configuração do Ocelot
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration);

// Adiciona Swagger for Ocelot
builder.Services.AddSwaggerForOcelot(builder.Configuration);

var app = builder.Build();

// Middleware do Swagger
app.UseSwaggerForOcelotUI(opt =>
{
    // Caminho do Swagger gerado pelos microserviços
    opt.PathToSwaggerGenerator = "/swagger/docs";
    // Não usamos RoutePrefix na versão atual
});

// Middleware do Ocelot
await app.UseOcelot();

app.Run();
