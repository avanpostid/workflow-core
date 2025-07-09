using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IStepBody
    {        
        ValueTask<ExecutionResult> RunAsync(IStepExecutionContext context);        
    }
}
