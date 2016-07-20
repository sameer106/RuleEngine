using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RuleEngine.Core.Domain;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.SqlServer;

namespace RuleEngine.Core.Helper
{
    public static class DBUtility
    {
        private static string ConnectionString = ConfigurationManager.ConnectionStrings["RuleConnString"].ConnectionString;
        private static IDbConnectionFactory _ConnectionFactory = new OrmLiteConnectionFactory(ConnectionString, SqlServerOrmLiteDialectProvider.Instance);

        public static List<CommunicationRules> GetRules()
        {
            List<CommunicationRules> communicationRules = new List<CommunicationRules>();
            using (var dbConn = _ConnectionFactory.OpenDbConnection())
            {
                using (var trans = dbConn.BeginTransaction(IsolationLevel.ReadUncommitted))
                {
                    communicationRules.AddRange(
                        dbConn.Select<CommunicationRules>(x => x.Where().OrderByDescending(y => y.CreatedOrModifiedDate)));
                }
            }
            return communicationRules;

        }

        public static List<CommunicationRules> GetRules(DateTime cutOffDateTime)
        {
            List<CommunicationRules> communicationRules = new List<CommunicationRules>();
            using (var dbConn = _ConnectionFactory.OpenDbConnection())
            {
                using (var trans = dbConn.BeginTransaction(IsolationLevel.ReadUncommitted))
                {
                    communicationRules.AddRange(dbConn.Select<CommunicationRules>(x =>x.Where(z => z.CreatedOrModifiedDate > cutOffDateTime).OrderByDescending(z => z.CreatedOrModifiedDate)));
                }
            }
            return communicationRules;

        }
       

    }
}
