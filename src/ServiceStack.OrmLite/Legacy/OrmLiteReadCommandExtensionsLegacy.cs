﻿using System;
using System.Collections.Generic;
using System.Data;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Legacy
{
    [Obsolete(Messages.LegacyApi)]
    internal static class OrmLiteReadCommandExtensionsLegacy
    {
        internal static List<T> SelectFmt<T>(this IDbCommand dbCmd, string sqlFilter, params object[] filterParams)
        {
            return dbCmd.ConvertToList<T>(
                dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sqlFilter, filterParams));
        }

        internal static List<TModel> SelectFmt<TModel>(this IDbCommand dbCmd, Type fromTableType, string sqlFilter, params object[] filterParams)
        {
            var sql = ToSelectFmt<TModel>(dbCmd.GetDialectProvider(), fromTableType, sqlFilter, filterParams);

            return dbCmd.ConvertToList<TModel>(sql);
        }

        internal static string ToSelectFmt<TModel>(IOrmLiteDialectProvider dialectProvider, Type fromTableType, string sqlFilter, object[] filterParams)
        {
            var sql = StringBuilderCache.Allocate();
            var modelDef = ModelDefinition<TModel>.Definition;
            sql.AppendFormat("SELECT {0} FROM {1}", dialectProvider.GetColumnNames(modelDef),
                             dialectProvider.GetQuotedTableName(fromTableType.GetModelDefinition()));
            if (!string.IsNullOrEmpty(sqlFilter))
            {
                sqlFilter = sqlFilter.SqlFmt(filterParams);
                sql.Append(" WHERE ");
                sql.Append(sqlFilter);
            }
            return StringBuilderCache.ReturnAndFree(sql);
        }

        internal static IEnumerable<T> SelectLazyFmt<T>(this IDbCommand dbCmd, string filter, params object[] filterParams)
        {
            var dialectProvider = dbCmd.GetDialectProvider();
            dbCmd.CommandText = dialectProvider.ToSelectStatement(typeof(T), filter, filterParams);

            if (OrmLiteConfig.ResultsFilter != null)
            {
                foreach (var item in OrmLiteConfig.ResultsFilter.GetList<T>(dbCmd))
                {
                    yield return item;
                }
                yield break;
            }

            using (var reader = dbCmd.ExecReader(dbCmd.CommandText))
            {
                var indexCache = reader.GetIndexFieldsCache(ModelDefinition<T>.Definition, dialectProvider);
                var values = new object[reader.FieldCount];
                while (reader.Read())
                {
                    var row = OrmLiteUtils.CreateInstance<T>();
                    row.PopulateWithSqlReader(dialectProvider, reader, indexCache, values);
                    yield return row;
                }
            }
        }
    }
}