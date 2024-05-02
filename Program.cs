using Redis.OM;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseCors(
    options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
);

// app.UseFileServer();

app.UseAuthorization();

app.MapControllers();


app.Run();
