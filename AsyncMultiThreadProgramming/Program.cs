using AsyncMultiThreadProgramming.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// IoC Container Service Provider
// Uygulama içerisinde servislerin instance yönetimi merkezi olarak yapmamýzý saðlayan bir geliþtirme tekniði
builder.Services.AddScoped<IAsyncService, AsyncService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

// uygulama gelen tüm istekleri yakalan middleware request Thread buradan bulacaðýz.

app.Use(async (context, next) =>
{
  await Console.Out.WriteLineAsync($"Request Start {Thread.CurrentThread.ManagedThreadId}");

  await next();

});


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run(); // kýsa devre middleware dediðimiz Run middleware ile request süreci tamamlanýp kullanýcýya cevap dönüyor

// Run dan sonra herhangi bir ara yazýlým çalýþamaz.