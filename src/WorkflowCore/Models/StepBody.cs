using System;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public abstract class StepBody : IStepBody
    {
        public virtual ExecutionResult Run(IStepExecutionContext context)
        {
            throw new NotImplementedException();
        }

        public virtual ValueTask<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            return new ValueTask<ExecutionResult>(Run(context));
        }        

        protected ExecutionResult OutcomeResult(object value)
        {
            return new ExecutionResult()
            {
                Proceed = true,
                OutcomeValue = value
            };
        }

        protected ExecutionResult PersistResult(object persistenceData)
        {
            return new ExecutionResult()
            {
                Proceed = false,
                PersistenceData = persistenceData
            };
        }

        protected ExecutionResult SleepResult(object persistenceData, TimeSpan sleep)
        {
            return new ExecutionResult()
            {
                Proceed = false,
                PersistenceData = persistenceData,
                SleepFor = sleep
            };
        }
    }
}
