using Newbe.Claptrap;
using System.Threading.Tasks;
using EventData = Newbe.Claptrap.ScaffoldGeneratorTest.TestEventDataType;
using StateData = Newbe.Claptrap.ScaffoldGeneratorTest.TestStateDataType;
namespace Claptrap._20EventMethods
{
    public class TestTaskMethod : ITestTaskMethod
    {
        public Task<EventMethodResult<EventData>> Invoke(StateData stateData)
        {
            throw new NotImplementedException();
        }
    }
}