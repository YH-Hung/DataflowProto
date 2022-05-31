using System.Text.Json;
using Confluent.Kafka;
using DataflowProto.Models;

namespace DataflowProto.MessageEvent;

public class ProducerFacade : IDisposable
{
    private readonly IConfiguration _config;
    private readonly IProducer<string, string> _producer;

    private readonly ILogger<ProducerFacade> _logger;

    public ProducerFacade(ILogger<ProducerFacade> logger)
    {
        _logger = logger;
        _config = new ConfigurationBuilder().AddIniFile("kafka_setting.properties").Build();
        _producer= new ProducerBuilder<string, string>(_config.AsEnumerable()).Build();
    }

    public void Post(ProducerDto eventDto)
    {
         _producer.Produce("dataflow-proto", new Message<string, string>
         {
             Key = "dataflow",
             Value = JsonSerializer.Serialize(eventDto)
         }, report =>
         {
             if (report.Error.Code != ErrorCode.NoError)
             {
                 _logger.LogWarning("Fail to deliver message {Msg}", report.Error.Reason);
             }
             else
             {
                 _logger.LogInformation("Produce event to topic {Msg}", eventDto.ResponseContent);
             }
         });
    }

    public void Dispose()
    {
        _producer.Flush();
        _producer.Dispose();
    }
}