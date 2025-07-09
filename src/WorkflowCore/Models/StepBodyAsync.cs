using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public abstract class StepBodyAsync : IStepBody
    {
        public abstract ValueTask<ExecutionResult> RunAsync(IStepExecutionContext context);
    }
}
