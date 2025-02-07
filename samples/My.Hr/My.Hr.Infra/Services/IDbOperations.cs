using System.Threading.Tasks;
using Pulumi;

namespace My.Hr.Infra.Services;

public interface IDbOperations
{
    Task<int> DeployDbSchemaAsync(string connectionString);
    void ProvisionUsers(Input<string> connectionString, string groupName);
}