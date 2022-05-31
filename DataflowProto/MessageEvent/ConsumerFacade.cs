using System.Text.Json;
using System.Threading.Tasks.Dataflow;
using Confluent.Kafka;
using DataflowProto.Models;

namespace DataflowProto.MessageEvent;

public class ConsumerFacade : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IConsumer<string, string> _consumer;
    
    private readonly BufferBlock<ConsumerDto> _consumerBuffer;

    private readonly ILogger<ConsumerFacade> _logger;

    public ConsumerFacade(ILogger<ConsumerFacade> logger, BufferBlock<ConsumerDto> consumerBuffer)
    {
        _logger = logger;
        _consumerBuffer = consumerBuffer;
        _config = new ConfigurationBuilder().AddIniFile("kafka_setting.properties").Build();
        _config["group.id"] = "dataflow-proto";
        _config["auto.offset.reset"] = "earliest";

        _consumer = new ConsumerBuilder<string, string>(_config.AsEnumerable()).Build();
        _consumer.Subscribe("dataflow-req");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(20_000, stoppingToken);
        _logger.LogInformation("Consumer is ready");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var cr = _consumer.Consume(stoppingToken);
                ConsumerDto? consumerEvent = JsonSerializer.Deserialize<ConsumerDto>(cr.Message.Value);
                if (consumerEvent is not null)
                {
                    _logger.LogInformation("Consumer request fetch {Request}", cr.Message.Value);
                    await _consumerBuffer.SendAsync(consumerEvent, stoppingToken);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            _consumer.Close();
            _logger.LogInformation("Consumer closed");
        }
    }
}