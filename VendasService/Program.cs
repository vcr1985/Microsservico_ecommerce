using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VendasService.Data;
using VendasService.Services;
using RabbitMQ.Client;
using Serilog;
using VendasService.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Conexão com banco de dados
builder.Services.AddDbContext<VendasContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 32))));

// Configuração RabbitMQ
builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var factory = new ConnectionFactory()
    {
        HostName = builder.Configuration["RabbitMQ:HostName"] ?? "localhost",
        UserName = builder.Configuration["RabbitMQ:UserName"] ?? "guest",
        Password = builder.Configuration["RabbitMQ:Password"] ?? "guest",
        Port = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672")
    };
    return factory;
});

// Serviços personalizados
builder.Services.AddScoped<IEstoqueService, EstoqueService>();
builder.Services.AddScoped<IRabbitMQPublisher, RabbitMQPublisher>();
builder.Services.AddScoped<ITokenService, TokenService>(); // ADICIONE ESTA LINHA
builder.Services.AddHttpClient<IEstoqueService, EstoqueService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:EstoqueService"] ?? "https://localhost:7145");
});

// Configuração JWT
var key = builder.Configuration["Jwt:Key"] ?? "ChaveSuperSecretaParaJWTTokenSeguro@2025!";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

// Controllers e Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Vendas API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Criar banco de dados automaticamente
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<VendasContext>();
    try
    {
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erro ao criar/migrar banco de dados");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Middleware de tratamento de exceções
app.UseMiddleware<ExceptionMiddleware>();

app.MapControllers();

app.Run();
