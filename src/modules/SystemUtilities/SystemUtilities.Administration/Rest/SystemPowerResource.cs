using System.Threading.Tasks;
using Maze.Administration.Library.Clients;
using Maze.Administration.Library.Clients.Helpers;

namespace SystemUtilities.Administration.Rest
{
    public class SystemPowerResource : ResourceBase<SystemPowerResource>
    {
        public SystemPowerResource() : base($"{PrismModule.ModuleName}/Power")
        {
        }

        public static Task Shutdown(ITargetedRestClient restClient) => CreateRequest(HttpVerb.Get, "shutdown").Execute(restClient);
        public static Task Restart(ITargetedRestClient restClient) => CreateRequest(HttpVerb.Get, "restart").Execute(restClient);
        public static Task LogOff(ITargetedRestClient restClient) => CreateRequest(HttpVerb.Get, "logoff").Execute(restClient);
    }
}