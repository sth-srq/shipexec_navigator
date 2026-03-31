using System.Data;

namespace ShipExecNavigator.DAL;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
