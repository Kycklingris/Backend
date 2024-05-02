using Certes;
using FluffySpoon.AspNet.EncryptWeMust;
using Redis.OM;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddFluffySpoonLetsEncrypt(new FluffySpoon.AspNet.EncryptWeMust.Certes.LetsEncryptOptions()
{
    Email = Environment.GetEnvironmentVariable("TLS_EMAIL"),
    Domains = [Environment.GetEnvironmentVariable("TLS_HOST")],
    TimeUntilExpiryBeforeRenewal = TimeSpan.FromDays(30),
});

builder.Services.AddFluffySpoonLetsEncryptFileCertificatePersistence("acme.json");
builder.Services.AddFluffySpoonLetsEncryptMemoryChallengePersistence();

builder.Services.AddCors();

builder.Services.AddControllers();

var redisHost = Environment.GetEnvironmentVariable("REDIS_IP");
var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT");
var redisPassword = Environment.GetEnvironmentVariable("REDIS_PASS");


if (redisPort == null)
{
    redisPort = "6379";
}

var redisConnectionConfig = new RedisConnectionConfiguration
{
    Host = redisHost,
    Port = int.Parse(redisPort),
    Password = redisPassword,
};

builder.Services.AddSingleton(new RedisConnectionProvider(redisConnectionConfig));



// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseFluffySpoonLetsEncrypt();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(
    options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
);

// app.UseFileServer();

app.UseAuthorization();

app.MapControllers();


app.Run();
