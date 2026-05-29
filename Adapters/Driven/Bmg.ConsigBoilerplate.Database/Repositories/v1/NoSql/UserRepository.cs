using Bmg.NoSqlConnection.Manager.Data;
using Bmg.ConsigBoilerplate.Database.Entities.v1.NoSql;
using Bmg.ConsigBoilerplate.Database.Repositories.Interfaces.v1.NoSql;
using Bmg.ConsigBoilerplate.Domain;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bmg.ConsigBoilerplate.Database.Repositories.v1.NoSql
{
    public class UserRepository : GenericRepository<DatabaseNoSqlConnection, ObjectId, User, IMongoCollection<User>>, IUserRepository
    {
        public UserRepository(IServiceProvider provider) : base(DatabaseNoSqlConnection.MongoDB, provider, "users") { }
    }
}
