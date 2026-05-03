using System.Data;

namespace ShipExecAgent.DAL;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
