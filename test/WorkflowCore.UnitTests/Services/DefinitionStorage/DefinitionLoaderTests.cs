using FakeItEasy;
using FluentAssertions;
using System;
using System.Linq;
using Newtonsoft.Json;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Primitives;
using WorkflowCore.Services;
using WorkflowCore.Services.DefinitionStorage;
using WorkflowCore.TestAssets.DataTypes;
using Xunit;

namespace WorkflowCore.UnitTests.Services.DefinitionStorage
{
    public class DefinitionLoaderTests
    {

        private readonly IDefinitionLoader _subject;
        private readonly IWorkflowRegistry _registry;

        public DefinitionLoaderTests()
        {
            _registry = A.Fake<IWorkflowRegistry>();
            _subject = new DefinitionLoader(_registry);
        }

        [Fact(DisplayName = "Should register workflow")]
        public void RegisterDefinition()
        {
            _subject.LoadDefinition("{\"Id\": \"HelloWorld\", \"Version\": 1, \"Steps\": []}");

            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Id == "HelloWorld"))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Version == 1))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.DataType == typeof(object)))).MustHaveHappened();
        }

        [Fact(DisplayName = "Should parse definition")]
        public void ParseDefinition()
        {
            _subject.LoadDefinition(TestAssets.Utils.GetTestDefinitionJson());

            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Id == "Test"))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Version == 1))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.DataType == typeof(CounterBoard)))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(MatchTestDefinition, ""))).MustHaveHappened();
        }


        [Fact(DisplayName = "Should parse definition")]
        public void ParseDefinitionDynamic()
        {
            _subject.LoadDefinition(TestAssets.Utils.GetTestDefinitionDynamicJson());

            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Id == "Test"))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Version == 1))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.DataType == typeof(DynamicData)))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(MatchTestDefinition, ""))).MustHaveHappened();
        }

        [Fact]
        public void GenerateByWorkflowBuilder()
        {
            // Arrange
            var workflowBuilder = new WorkflowBuilder();
            workflowBuilder.AddStep(new EndStep());
            var workflowDefinition = workflowBuilder.Build("Test1", 1);
            
            // Act & Assert
            var result = _subject.LoadWorkflowDefinition(workflowDefinition);

            Assert.Equal(workflowDefinition.Id, result.Id);
        }
        

        private bool MatchTestDefinition(WorkflowDefinition def)
        {
            //TODO: make this better
            var step1 = def.Steps.Single(s => s.ExternalId == "Step1");
            var step2 = def.Steps.Single(s => s.ExternalId == "Step2");
            
            step1.Outcomes.Count.Should().Be(1);
            step1.Inputs.Count.Should().Be(1);
            step1.Outputs.Count.Should().Be(1);
            step1.Outcomes.Single().NextStep.Should().Be(step2.Id);

            return true;
        }

    }
}
