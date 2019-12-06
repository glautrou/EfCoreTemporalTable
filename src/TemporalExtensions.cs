using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace EfCoreTemporalTable
{
    /// <summary>
    /// Temporal table extensions
    /// </summary>
    public static class TemporalExtensions
    {
        /// <summary>
        /// Get temporal data
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="dbSet">Data set</param>
        /// <param name="temporalCriteria">Temporal criteria of the SQL query</param>
        /// <param name="arguments">Temporal SQL arguments</param>
        /// <returns>Temporal data</returns>
        private static IQueryable<T> AsTemporal<T>(this DbSet<T> dbSet, string temporalCriteria, params object[] arguments) where T : class
        {
            var table = dbSet
                .GetService<ICurrentDbContext>()
                .GetTableName<T>();
            var selectSql = $"SELECT * FROM {table}";
            var sql = FormattableStringFactory.Create(selectSql + " FOR SYSTEM_TIME " + temporalCriteria, arguments);
            return dbSet.FromSql(sql);
        }

        /// <summary>
        /// Returns the union of rows that belong to the current and the history table.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="dbSet">Data set</param>
        /// <returns>Temporal data</returns>
        public static IQueryable<T> AsTemporalAll<T>(this DbSet<T> dbSet) where T : class
        {
            return dbSet.AsTemporal("ALL");
        }

        /// <summary>
        /// Returns a table with a rows containing the values that were actual (current) at the 
        /// specified point in time in the past. Internally, a union is performed between the 
        /// temporal table and its history table and the results are filtered to return the 
        /// values in the row that was valid at the point in time specified by the <date_time> 
        /// parameter. The value for a row is deemed valid if the system_start_time_column_name 
        /// value is less than or equal to the <date_time> parameter value and the 
        /// system_end_time_column_name value is greater than the <date_time> parameter value.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="dbSet">Data set</param>
        /// <param name="date">Exact date</param>
        /// <returns></returns>
        public static IQueryable<T> AsTemporalAsOf<T>(this DbSet<T> dbSet, DateTime date) where T : class
        {
            return dbSet.AsTemporal("AS OF {0}", date.ToUniversalTime());
        }

        /// <summary>
        /// Returns a table with the values for all row versions that were active within the 
        /// specified time range, regardless of whether they started being active before the 
        /// <start_date_time> parameter value for the FROM argument or ceased being active after 
        /// the <end_date_time> parameter value for the TO argument. Internally, a union is 
        /// performed between the temporal table and its history table and the results are 
        /// filtered to return the values for all row versions that were active at any time 
        /// during the time range specified. Rows that ceased being active exactly on the lower 
        /// boundary defined by the FROM endpoint are not included and records that became active 
        /// exactly on the upper boundary defined by the TO endpoint are not included also.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="dbSet">Data set</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Temporal data</returns>
        public static IQueryable<T> AsTemporalFrom<T>(this DbSet<T> dbSet, DateTime startDate, DateTime endDate) where T : class
        {
            return dbSet.AsTemporal("FROM {0} TO {1}", startDate.ToUniversalTime(), endDate.ToUniversalTime());
        }

        /// <summary>
        /// Same as above in the FOR SYSTEM_TIME FROM <start_date_time>TO <end_date_time> 
        /// description, except the table of rows returned includes rows that became active on 
        /// the upper boundary defined by the <end_date_time> endpoint.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="dbSet">Data set</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Temporal data</returns>
        public static IQueryable<T> AsTemporalBetween<T>(this DbSet<T> dbSet, DateTime startDate, DateTime endDate) where T : class
        {
            return dbSet.AsTemporal("BETWEEN {0} AND {1}", startDate.ToUniversalTime(), endDate.ToUniversalTime());
        }

        /// <summary>
        /// Returns a table with the values for all row versions that were opened and closed 
        /// within the specified time range defined by the two datetime values for the CONTAINED 
        /// IN argument. Rows that became active exactly on the lower boundary or ceased being 
        /// active exactly on the upper boundary are included.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="dbSet">Data set</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Temporal data</returns>
        public static IQueryable<T> AsTemporalContained<T>(this DbSet<T> dbSet, DateTime startDate, DateTime endDate) where T : class
        {
            return dbSet.AsTemporal("CONTAINED IN ({0}, {1})", startDate.ToUniversalTime(), endDate.ToUniversalTime());
        }

        /// <summary>
        /// Get the specified entity type SQL column full name
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="dbContext">Database context</param>
        /// <returns>Full name of the table</returns>
        private static string GetTableName<T>(this ICurrentDbContext dbContext) where T : class
        {
            var entityType = dbContext.Context.Model.FindEntityType(typeof(T));
            var mapping = entityType.Relational();
            var schema = mapping.Schema;
            var tableName = mapping.TableName;

            return $"[{schema}].[{tableName}]";
        }
    }
}
