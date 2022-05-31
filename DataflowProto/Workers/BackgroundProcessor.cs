using System.Threading.Tasks.Dataflow;
using DataflowProto.MessageEvent;
using DataflowProto.Models;

namespace DataflowProto.Workers;

public class BackgroundProcessor : BackgroundService
{
    private readonly BufferBlock<ConsumerDto> _consumerBuffer;
    private readonly TransformBlock<ConsumerDto, ProducerDto> _transformBlock;
    private readonly ActionBlock<ProducerDto> _finalDataSink;
    private readonly BroadcastBlock<ProducerDto> _produceEventBroadcastBlock;
    private readonly ActionBlock<ProducerDto> _otherDataSink;

    private readonly ProducerFacade _producer;
    
    public BackgroundProcessor(BufferBlock<ConsumerDto> consumerBuffer, ProducerFacade producer)
    {
        _consumerBuffer = consumerBuffer;
        _producer = producer;
        _transformBlock = new TransformBlock<ConsumerDto, ProducerDto>(cDto => new ProducerDto
        {
            Id = cDto.Id,
            ResponseContent = cDto.InitialContent + "Appended by transformer..."
        });

        _produceEventBroadcastBlock = new BroadcastBlock<ProducerDto>(p => p);

        _finalDataSink = new ActionBlock<ProducerDto>(pDto =>
            Console.WriteLine("Id: {0}, Content: {1}", pDto.Id, pDto.ResponseContent));
        _otherDataSink = new ActionBlock<ProducerDto>(pDto => _producer.Post(pDto));

        var dataflowLinkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        _consumerBuffer.LinkTo(_transformBlock, dataflowLinkOptions);
        _transformBlock.LinkTo(_produceEventBroadcastBlock, dataflowLinkOptions);
        _produceEventBroadcastBlock.LinkTo(_finalDataSink, dataflowLinkOptions);
        _produceEventBroadcastBlock.LinkTo(_otherDataSink, dataflowLinkOptions);
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
        await Task.WhenAll(_finalDataSink.Completion, _otherDataSink.Completion);
        await base.StopAsync(cancellationToken);
    }
}
