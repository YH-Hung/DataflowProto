using System.Threading.Tasks.Dataflow;
using DataflowProto.Entity;
using DataflowProto.MessageEvent;
using DataflowProto.Models;
using DataflowProto.Workers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var consumerBuffer = new BufferBlock<ConsumerDto>();
builder.Services.AddSingleton(consumerBuffer);
builder.Services.AddSingleton<ProducerFacade>();

builder.Services.AddHostedService<BackgroundProcessor>();
builder.Services.AddHostedService<ConsumerFacade>();

builder.Services.AddDbContext<postgresContext>(o =>
{
    o.UseNpgsql("Server=localhost;Database=postgres;User Id=dotnetApp;");
});
    
builder.Services.AddControllers();
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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
