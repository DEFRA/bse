using System.Data;

namespace BSE.Infrastructure;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
