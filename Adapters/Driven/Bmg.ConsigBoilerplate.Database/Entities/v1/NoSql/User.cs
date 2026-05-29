using Bmg.NoSqlConnection.Manager.Entities;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bmg.ConsigBoilerplate.Database.Entities.v1.NoSql
{
    public class User : DbEntity<ObjectId>
    {
        public string Name { get; set; }
        public DateTime BirthDate { get; set; }
    }
}
