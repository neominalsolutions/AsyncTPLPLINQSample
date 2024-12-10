using AsyncMultiThreadProgramming.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// IoC Container Service Provider
// Uygulama i�erisinde servislerin instance y�netimi merkezi olarak yapmam�z� sa�layan bir geli�tirme tekni�i
builder.Services.AddScoped<IAsyncService, AsyncService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

// uygulama gelen t�m istekleri yakalan middleware request Thread buradan bulaca��z.

app.Use(async (context, next) =>
{
  await Console.Out.WriteLineAsync($"Request Start {Thread.CurrentThread.ManagedThreadId}");

  await next();

});


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run(); // k�sa devre middleware dedi�imiz Run middleware ile request s�reci tamamlan�p kullan�c�ya cevap d�n�yor

// Run dan sonra herhangi bir ara yaz�l�m �al��amaz.