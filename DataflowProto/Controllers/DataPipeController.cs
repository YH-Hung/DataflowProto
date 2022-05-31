using System.Threading.Tasks.Dataflow;
using DataflowProto.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataflowProto.Controllers;

[ApiController]
[Route("[controller]")]
public class DataPipeController : ControllerBase
{
    private readonly BufferBlock<ConsumerDto> _consumerBuffer;

    public DataPipeController(BufferBlock<ConsumerDto> consumerBuffer)
    {
        _consumerBuffer = consumerBuffer;
    }

    [HttpPost]
    public string Post([FromBody] ConsumerDto consumerEvent)
    {
        _consumerBuffer.Post(consumerEvent);
        return "Consume one event, data flow start...";
    }
}
