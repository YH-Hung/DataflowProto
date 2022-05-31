using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks.Dataflow;
using DataflowProto.Entity;
using DataflowProto.MessageEvent;
using DataflowProto.Models;

namespace DataflowProto.Workers;

[SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
public class BackgroundProcessor : BackgroundService
{
    private readonly BufferBlock<ConsumerDto> _consumerBuffer;
    private readonly TransformBlock<ConsumerDto, ProducerDto> _transformBlock;
    private readonly BroadcastBlock<ProducerDto> _produceEventBroadcastBlock;

    private readonly TransformBlock<ProducerDto, ProducerDto> _retryBlock;

    private readonly ActionBlock<ProducerDto> _finalDataSink;
    private readonly ActionBlock<ProducerDto> _kafkaSink;

    private readonly ProducerFacade _producer;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundProcessor> _logger;

    public BackgroundProcessor(BufferBlock<ConsumerDto> consumerBuffer, ProducerFacade producer,
        IServiceProvider serviceProvider, ILogger<BackgroundProcessor> logger)
    {
        _consumerBuffer = consumerBuffer;
        _producer = producer;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _transformBlock = new TransformBlock<ConsumerDto, ProducerDto>(cDto => new ProducerDto
        {
            Id = cDto.Id,
            ResponseContent = cDto.InitialContent + "Appended by transformer..."
        });

        _produceEventBroadcastBlock = new BroadcastBlock<ProducerDto>(p => p);

        _retryBlock = new TransformBlock<ProducerDto, ProducerDto>(async dto =>
        {
            await Task.Delay(10_000);
            _logger.LogInformation("Send to retry");
            return dto;
        });

        _finalDataSink = new ActionBlock<ProducerDto>(pDto =>
        {
            try
            {
                var newGirl = new Girl
                {
                    Id = pDto.Id,
                    MyComment = pDto.ResponseContent
                };

                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<postgresContext>();
                    dbContext.Girls.Add(newGirl);
                    dbContext.SaveChanges();
                }

                _logger.LogInformation("Id: {Id}, Content: {Content}", pDto.Id, pDto.ResponseContent);
            }
            catch (Exception e)
            {
                _logger.LogWarning("DB connection failed");
                _retryBlock.Post(pDto);
            }
        });

        _kafkaSink = new ActionBlock<ProducerDto>(pDto => _producer.Post(pDto));

        var dataflowLinkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        _consumerBuffer.LinkTo(_transformBlock, dataflowLinkOptions);
        _transformBlock.LinkTo(_produceEventBroadcastBlock, dataflowLinkOptions);
        _produceEventBroadcastBlock.LinkTo(_finalDataSink, dataflowLinkOptions);
        _produceEventBroadcastBlock.LinkTo(_kafkaSink, dataflowLinkOptions);
        _retryBlock.LinkTo(_finalDataSink);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await DoWork(stoppingToken);
    }

    private async Task DoWork(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Let blocks do the job...
            await Task.Delay(30_000, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _consumerBuffer.Complete();
        _retryBlock.Complete();
        await Task.WhenAll(_finalDataSink.Completion, _kafkaSink.Completion);
        await base.StopAsync(cancellationToken);
    }
}