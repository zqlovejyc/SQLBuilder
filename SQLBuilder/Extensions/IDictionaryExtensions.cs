﻿#region License
/***
 * Copyright © 2018-2025, 张强 (943620963@qq.com).
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * without warranties or conditions of any kind, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion

using Dapper;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using SQLBuilder.Attributes;
using SQLBuilder.Parameters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using static Dapper.SqlMapper;

namespace SQLBuilder.Extensions
{
    /// <summary>
    /// IDictionary扩展类
    /// </summary>
    public static class IDictionaryExtensions
    {
        #region ToDynamicParameters
        /// <summary>
        /// DbParameter转换为DynamicParameters
        /// </summary>
        /// <param name="this"></param>
        /// <returns></returns>
        public static IDynamicParameters ToDynamicParameters(this DbParameter[] @this)
        {
            if (@this == null || @this.Length == 0)
                return null;

            var args = new DynamicParameters();
            @this.ToList().ForEach(p => args.Add(p.ParameterName, p.Value, p.DbType, p.Direction, p.Size));
            return args;
        }

        /// <summary>
        /// DbParameter转换为DynamicParameters
        /// </summary>
        /// <param name="this"></param>
        /// <returns></returns>
        public static IDynamicParameters ToDynamicParameters(this List<DbParameter> @this)
        {
            if (@this == null || @this.Count == 0)
                return null;

            var args = new DynamicParameters();
            @this.ForEach(p => args.Add(p.ParameterName, p.Value, p.DbType, p.Direction, p.Size));
            return args;
        }

        /// <summary>
        ///  DbParameter转换为DynamicParameters
        /// </summary>
        /// <param name="this"></param>
        /// <returns></returns>
        public static IDynamicParameters ToDynamicParameters(this DbParameter @this)
        {
            if (@this == null)
                return null;

            var args = new DynamicParameters();
            args.Add(@this.ParameterName, @this.Value, @this.DbType, @this.Direction, @this.Size);
            return args;
        }

        /// <summary>
        ///  IDictionary转换为DynamicParameters
        /// </summary>
        /// <param name="this"></param>        
        /// <returns></returns>
        public static IDynamicParameters ToDynamicParameters(this IDictionary<string, object> @this)
        {
            if (@this == null || @this.Count == 0)
                return null;

            var args = new DynamicParameters();
            foreach (var item in @this)
                args.Add(item.Key, item.Value);

            return args;
        }

        /// <summary>
        ///  IDictionary转换为DynamicParameters
        /// </summary>
        /// <param name="this"></param>        
        /// <returns></returns>
        public static IDynamicParameters ToDynamicParameters(this IDictionary<string, (object data, DataTypeAttribute type)> @this)
        {
            if (@this.IsNull() || @this.Count == 0)
                return null;

            //OracleDbType
            if (@this.Values.Any(x => x.type?.IsOracleDbType == true))
            {
                var parameter = new OracleDynamicParameters();

                foreach (var item in @this)
                    parameter.Add(item.Key, item.Value.data,
                        item.Value.type?.IsOracleDbType == true
                            ? item.Value.type.OracleDbType
                            : null,
                        item.Value.type?.IsFixedLength == true
                            ? item.Value.type.FixedLength
                            : null);

                return parameter;
            }
            //DbType
            else
            {
                var parameter = new DynamicParameters();

                foreach (var item in @this)
                    parameter.Add(item.Key, item.Value.data,
                        item.Value.type?.IsDbType == true
                            ? item.Value.type.DbType
                            : null,
                        size: item.Value.type?.IsFixedLength == true
                            ? item.Value.type.FixedLength
                            : null);

                return parameter;
            }
        }
        #endregion

        #region ToDbParameters
        /// <summary>
        /// An IDictionary&lt;string,object&gt; extension method that converts this object to a database parameters.
        /// </summary>
        /// <param name="this">The @this to act on.</param>
        /// <param name="command">The command.</param>        
        /// <returns>The given data converted to a DbParameter[].</returns>
        public static DbParameter[] ToDbParameters(this IDictionary<string, object> @this, DbCommand command)
        {
            if (@this == null || @this.Count == 0)
                return null;

            return @this.Select(x =>
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = x.Key;
                parameter.Value = x.Value;

                return parameter;

            }).ToArray();
        }

        /// <summary>
        /// An IDictionary&lt;string,object&gt; extension method that converts this object to a database parameters.
        /// </summary>
        /// <param name="this">The @this to act on.</param>
        /// <param name="command">The command.</param>        
        /// <returns>The given data converted to a DbParameter[].</returns>
        public static DbParameter[] ToDbParameters(this IDictionary<string, (object data, DataTypeAttribute type)> @this, DbCommand command)
        {
            if (@this == null || @this.Count == 0)
                return null;

            return @this.Select(x =>
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = x.Key;
                parameter.Value = x.Value.data;

                if (x.Value.type == null)
                    return parameter;

                if (x.Value.type.IsDbType)
                    parameter.DbType = x.Value.type.DbType;

                if (x.Value.type.IsFixedLength)
                    parameter.Size = x.Value.type.FixedLength;

                return parameter;

            }).ToArray();
        }

        /// <summary>
        ///  An IDictionary&lt;string,object&gt; extension method that converts this object to a database parameters.
        /// </summary>
        /// <param name="this">The @this to act on.</param>
        /// <param name="connection">The connection.</param>        
        /// <returns>The given data converted to a DbParameter[].</returns>
        public static DbParameter[] ToDbParameters(this IDictionary<string, object> @this, DbConnection connection)
        {
            if (@this == null || @this.Count == 0)
                return null;

            var command = connection.CreateCommand();
            return @this.Select(x =>
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = x.Key;
                parameter.Value = x.Value;

                return parameter;

            }).ToArray();
        }

        /// <summary>
        ///  An IDictionary&lt;string,object&gt; extension method that converts this object to a database parameters.
        /// </summary>
        /// <param name="this">The @this to act on.</param>
        /// <param name="connection">The connection.</param>        
        /// <returns>The given data converted to a DbParameter[].</returns>
        public static DbParameter[] ToDbParameters(this IDictionary<string, (object data, DataTypeAttribute type)> @this, DbConnection connection)
        {
            if (@this == null || @this.Count == 0)
                return null;

            var command = connection.CreateCommand();
            return @this.Select(x =>
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = x.Key;
                parameter.Value = x.Value.data;

                if (x.Value.type == null)
                    return parameter;

                if (x.Value.type.IsDbType)
                    parameter.DbType = x.Value.type.DbType;

                if (x.Value.type.IsFixedLength)
                    parameter.Size = x.Value.type.FixedLength;

                return parameter;

            }).ToArray();
        }
        #endregion

        #region ToSqlParameters
        /// <summary>
        /// An IDictionary&lt;string,object&gt; extension method that converts the @this to a SQL parameters.
        /// </summary>
        /// <param name="this">The @this to act on.</param>        
        /// <returns>@this as a SqlParameter[].</returns>
        public static SqlParameter[] ToSqlParameters(this IDictionary<string, object> @this)
        {
            if (@this == null || @this.Count == 0)
                return null;

            return @this.Select(x => new SqlParameter(x.Key.Replace("?", "@").Replace(":", "@"), x.Value)).ToArray();
        }

        /// <summary>
        /// An IDictionary&lt;string,object&gt; extension method that converts the @this to a SQL parameters.
        /// </summary>
        /// <param name="this">The @this to act on.</param>        
        /// <returns>@this as a SqlParameter[].</returns>
        public static SqlParameter[] ToSqlParameters(this IDictionary<string, (object data, DataTypeAttribute type)> @this)
        {
            if (@this == null || @this.Count == 0)
                return null;

            return @this.Select(x =>
            {
                var parameter = new SqlParameter(x.Key.Replace("?", "@").Replace(":", "@"), x.Value.data);

                if (x.Value.type == null)
                    return parameter;

                if (x.Value.type.IsDbType)
                    parameter.DbType = x.Value.type.DbType;

                if (x.Value.type.IsSqlDbType)
                    parameter.SqlDbType = x.Value.type.SqlDbType;

                if (x.Value.type.IsFixedLength)
                    parameter.Size = x.Value.type.FixedLength;

                return parameter;

            }).ToArray();
        }
        #endregion

        #region ToMySqlParameters
        /// <summary>
        /// An IDictionary&lt;string,object&gt; extension method that converts the @this to a MySQL parameters.
        /// </summary>
        /// <param name="this">The @this to act on.</param>        
        /// <returns>@this as a MySqlParameter[].</returns>
        public static MySqlParameter[] ToMySqlParameters(this IDictionary<string, object> @this)
        {
            if (@this == null || @this.Count == 0)
                return null;

            return @this.Select(x => new MySqlParameter(x.Key.Replace("@", "?").Replace(":", "?"), x.Value)).ToArray();
        }

        /// <summary>
        /// An IDictionary&lt;string,object&gt; extension method that converts the @this to a MySQL parameters.
        /// </summary>
        /// <param name="this">The @this to act on.</param>        
        /// <returns>@this as a MySqlParameter[].</returns>
        public static MySqlParameter[] ToMySqlParameters(this IDictionary<string, (object data, DataTypeAttribute type)> @this)
        {
            if (@this == null || @this.Count == 0)
                return null;

            return @this.Select(x =>
            {
                var parameter = new MySqlParameter(x.Key.Replace("@", "?").Replace(":", "?"), x.Value.data);

                if (x.Value.type == null)
                    return parameter;

                if (x.Value.type.IsDbType)
                    parameter.DbType = x.Value.type.DbType;

                if (x.Value.type.IsMySqlDbType)
                    parameter.MySqlDbType = x.Value.type.MySqlDbType;

                if (x.Value.type.IsFixedLength)
                    parameter.Size = x.Value.type.FixedLength;

                return parameter;

            }).ToArray();
        }
        #endregion

        #region ToSqliteParameters
        /// <summary>
        /// An IDictionary&lt;string,object&gt; extension method that converts the @this to a Sqlite parameters.
        /// </summary>
        /// <param name="this">The @this to act on.</param>        
        /// <returns>@this as a SQLiteParameter[].</returns>
        public static SQLiteParameter[] ToSqliteParameters(this IDictionary<string, object> @this)
        {
            if (@this == null || @this.Count == 0)
                return null;

            return @this.Select(x => new SQLiteParameter(x.Key.Replace("?", "@").Replace(":", "@"), x.Value)).ToArray();
        }

        /// <summary>
        /// An IDictionary&lt;string,object&gt; extension method that converts the @this to a Sqlite parameters.
        /// </summary>
        /// <param name="this">The @this to act on.</param>        
        /// <returns>@this as a SQLiteParameter[].</returns>
        public static SQLiteParameter[] ToSqliteParameters(this IDictionary<string, (object data, DataTypeAttribute type)> @this)
        {
            if (@this == null || @this.Count == 0)
                return null;

            return @this.Select(x =>
            {
                var parameter = new SQLiteParameter(x.Key.Replace("?", "@").Replace(":", "@"), x.Value.data);

                if (x.Value.type == null)
                    return parameter;

                if (x.Value.type.IsDbType)
                    parameter.DbType = x.Value.type.DbType;

                if (x.Value.type.IsFixedLength)
                    parameter.Size = x.Value.type.FixedLength;

                return parameter;

            }).ToArray();
        }
        #endregion

        #region ToOracleParameters
        /// <summary>
        /// An IDictionary&lt;string,object&gt; extension method that converts the @this to a Oracle parameters.
        /// </summary>
        /// <param name="this">The @this to act on.</param>        
        /// <returns>@this as a OracleParameter[].</returns>
        public static OracleParameter[] ToOracleParameters(this IDictionary<string, object> @this)
        {
            if (@this == null || @this.Count == 0)
                return null;

            return @this.Select(x => new OracleParameter(x.Key.Replace("?", ":").Replace("@", ":"), x.Value)).ToArray();
        }

        /// <summary>
        /// An IDictionary&lt;string,object&gt; extension method that converts the @this to a Oracle parameters.
        /// </summary>
        /// <param name="this">The @this to act on.</param>        
        /// <returns>@this as a OracleParameter[].</returns>
        public static OracleParameter[] ToOracleParameters(this IDictionary<string, (object data, DataTypeAttribute type)> @this)
        {
            if (@this == null || @this.Count == 0)
                return null;

            return @this.Select(x =>
            {
                var parameter = new OracleParameter(x.Key.Replace("?", ":").Replace("@", ":"), x.Value.data);

                if (x.Value.type == null)
                    return parameter;

                if (x.Value.type.IsDbType)
                    parameter.DbType = x.Value.type.DbType;

                if (x.Value.type.IsOracleDbType)
                    parameter.OracleDbType = x.Value.type.OracleDbType;

                if (x.Value.type.IsFixedLength)
                    parameter.Size = x.Value.type.FixedLength;

                return parameter;

            }).ToArray();
        }
        #endregion

        #region ToNpgsqlParameters
        /// <summary>
        /// An IDictionary&lt;string,object&gt; extension method that converts the @this to a PostgreSQL parameters.
        /// </summary>
        /// <param name="this">The @this to act on.</param>        
        /// <returns>@this as a NpgsqlParameter[].</returns>
        public static NpgsqlParameter[] ToNpgsqlParameters(this IDictionary<string, object> @this)
        {
            if (@this == null || @this.Count == 0)
                return null;

            return @this.Select(x => new NpgsqlParameter(x.Key.Replace("?", ":").Replace("@", ":"), x.Value)).ToArray();
        }

        /// <summary>
        /// An IDictionary&lt;string,object&gt; extension method that converts the @this to a PostgreSQL parameters.
        /// </summary>
        /// <param name="this">The @this to act on.</param>        
        /// <returns>@this as a NpgsqlParameter[].</returns>
        public static NpgsqlParameter[] ToNpgsqlParameters(this IDictionary<string, (object data, DataTypeAttribute type)> @this)
        {
            if (@this == null || @this.Count == 0)
                return null;

            return @this.Select(x =>
            {
                var parameter = new NpgsqlParameter(x.Key.Replace("?", ":").Replace("@", ":"), x.Value.data);

                if (x.Value.type == null)
                    return parameter;

                if (x.Value.type.IsDbType)
                    parameter.DbType = x.Value.type.DbType;

                if (x.Value.type.IsNpgsqlDbType)
                    parameter.NpgsqlDbType = x.Value.type.NpgsqlDbType;

                if (x.Value.type.IsFixedLength)
                    parameter.Size = x.Value.type.FixedLength;

                return parameter;

            }).ToArray();
        }
        #endregion

        #region TryGetValue
        /// <summary>
        /// This method is used to try to get a value in a dictionary if it does exists.
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="this">The collection object</param>
        /// <param name="key">Key</param>
        /// <param name="defaultValue">default value if key not exists</param>
        /// <returns>The value corresponding to the dictionary key otherwise return the defaultValue</returns>
        public static T TryGetValue<T>(this IDictionary<string, object> @this, string key, T defaultValue)
        {
            if (@this.IsNull() || !@this.ContainsKey(key))
                return defaultValue;

            if (@this[key] is T t)
                return t;

            return defaultValue;
        }

        /// <summary>
        /// This method is used to try to get a value in a dictionary if it does exists.
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="this">The collection object</param>
        /// <param name="key">Key</param>
        /// <param name="ignoreCase">If ignore case the key</param>
        /// <param name="defaultValue">default value if key not exists</param>
        /// <returns>The value corresponding to the dictionary key otherwise return the defaultValue</returns>
        public static T TryGetValue<T>(this IDictionary<string, object> @this, string key, bool ignoreCase, T defaultValue = default)
        {
            if (!ignoreCase)
                return @this.TryGetValue(key, defaultValue);

            if (@this.IsNull() || !@this.Keys.Any(k => k.EqualIgnoreCase(key)))
                return defaultValue;

            if (@this.FirstOrDefault(o => o.Key.EqualIgnoreCase(key)).Value is T t)
                return t;

            return defaultValue;
        }

        /// <summary>
        /// This method is used to try to get a value in a dictionary if it does exists.
        /// </summary>
        /// <typeparam name="TKey">Type of the key</typeparam>
        /// <typeparam name="TValue">Type of the value</typeparam>
        /// <param name="this">The collection object</param>
        /// <param name="key">Key</param>
        /// <param name="defaultValue">default value if key not exists</param>
        /// <returns>The value corresponding to the dictionary key otherwise return the defaultValue</returns>
        public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, TValue defaultValue = default)
        {
            if (@this.IsNull() || !@this.ContainsKey(key))
                return defaultValue;

            return @this[key];
        }

        /// <summary>
        /// This method is used to try to get a value in a dictionary if it does exists.
        /// </summary>
        /// <typeparam name="TKey">Type of the key</typeparam>
        /// <typeparam name="TValue">Type of the value</typeparam>
        /// <param name="this">The collection object</param>
        /// <param name="key">Key</param>
        /// <param name="ignoreCase">If ignore case the key</param>
        /// <param name="defaultValue">default value if key not exists</param>
        /// <returns>The value corresponding to the dictionary key otherwise return the defaultValue</returns>
        public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, bool ignoreCase, TValue defaultValue = default)
        {
            if (!ignoreCase)
                return @this.TryGetValue(key, defaultValue);

            bool match(TKey k) =>
                typeof(TKey) == typeof(string)
                ? string.Equals(k as string, key as string, StringComparison.OrdinalIgnoreCase)
                : Equals(k, key);

            if (@this.IsNull() || !@this.Keys.Any(match))
                return defaultValue;

            return @this.FirstOrDefault(o => match(o.Key)).Value;
        }
        #endregion

        #region TryGetOrAdd
        /// <summary>
        /// This method is used to try to get a value in a dictionary or add value to dictionary.
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="this">The collection object</param>
        /// <param name="key">Key</param>
        /// <param name="func">The delegate for add</param>
        /// <returns>The value corresponding to the dictionary key otherwise return the defaultValue</returns>
        public static T TryGetOrAdd<T>(this IDictionary<string, object> @this, string key, Func<T> func)
        {
            if (@this.IsNull() || func.IsNull())
                return default;

            if (@this.TryGetValue(key, out var val) && val is T retval)
                return retval;

            @this[key] = retval = func();

            return retval;
        }

        /// <summary>
        /// This method is used to try to get a value in a dictionary or add value to dictionary.
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="this">The collection object</param>
        /// <param name="key">Key</param>
        /// <param name="func">The delegate for add</param>
        /// <param name="ignoreCase">If ignore case the key</param>
        /// <returns>The value corresponding to the dictionary key otherwise return the defaultValue</returns>
        public static T TryGetOrAdd<T>(this IDictionary<string, object> @this, string key, Func<T> func, bool ignoreCase)
        {
            if (!ignoreCase)
                return @this.TryGetOrAdd(key, func);

            if (@this.IsNull() || func.IsNull())
                return default;

            if (!@this.Keys.Any(k => k.EqualIgnoreCase(key)))
            {
                var retval = func();
                @this[key] = retval;
                return retval;
            }

            if (@this.FirstOrDefault(o => o.Key.EqualIgnoreCase(key)).Value is T t)
                return t;

            return default;
        }

        /// <summary>
        /// This method is used to try to get a value in a dictionary or add value to dictionary.
        /// </summary>
        /// <typeparam name="TKey">Type of the key</typeparam>
        /// <typeparam name="TValue">Type of the value</typeparam>
        /// <param name="this">The collection object</param>
        /// <param name="key">Key</param>
        /// <param name="func">The delegate for add</param>
        /// <returns>The value corresponding to the dictionary key otherwise return the defaultValue</returns>
        public static TValue TryGetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, Func<TValue> func)
        {
            if (@this.IsNull() || func.IsNull())
                return default;

            if (@this.TryGetValue(key, out var val) && val is TValue retval)
                return retval;

            @this[key] = retval = func();

            return retval;
        }

        /// <summary>
        /// This method is used to try to get a value in a dictionary or add value to dictionary.
        /// </summary>
        /// <typeparam name="TKey">Type of the key</typeparam>
        /// <typeparam name="TValue">Type of the value</typeparam>
        /// <param name="this">The collection object</param>
        /// <param name="key">Key</param>
        /// <param name="func">The delegate for add</param>
        /// <param name="ignoreCase">If ignore case the key</param>
        /// <returns>The value corresponding to the dictionary key otherwise return the defaultValue</returns>
        public static TValue TryGetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, Func<TValue> func, bool ignoreCase)
        {
            if (!ignoreCase)
                return @this.TryGetOrAdd(key, func);

            if (@this.IsNull() || func.IsNull())
                return default;

            bool match(TKey k) =>
                 typeof(TKey) == typeof(string)
                 ? string.Equals(k as string, key as string, StringComparison.OrdinalIgnoreCase)
                 : Equals(k, key);

            if (!@this.Keys.Any(match))
                return @this[key] = func();

            return @this.FirstOrDefault(o => match(o.Key)).Value;
        }
        #endregion

        #region ContainsKey
        /// <summary>
        /// Determines whether the <see cref="IDictionary{Tkey, TValue}"/> contains an element with the specified key.
        /// </summary>
        /// <param name="this">The target dictionary</param>
        /// <param name="key">The key to be checked does it exist</param>
        /// <param name="ignoreCase">The true ignore case of the key, otherwise not ignore case.</param>
        /// <returns></returns>
        public static bool ContainsKey<T>(this IDictionary<string, T> @this, string key, bool ignoreCase)
        {
            if (@this.IsNullOrEmpty())
                return false;

            if (!ignoreCase)
                return @this.ContainsKey(key);

            return @this.Keys.Any(k => k.EqualIgnoreCase(key));
        }
        #endregion
    }
}
