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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using SQLBuilder.Extensions;
using SQLBuilder.Enums;

#region Refrence Alias
//Table
using CusTableAttribute = SQLBuilder.Attributes.TableAttribute;
using SysTableAttribute = System.ComponentModel.DataAnnotations.Schema.TableAttribute;

//Column
using CusColumnAttribute = SQLBuilder.Attributes.ColumnAttribute;
using SysColumnAttribute = System.ComponentModel.DataAnnotations.Schema.ColumnAttribute;

//Key
using CusKeyAttribute = SQLBuilder.Attributes.KeyAttribute;
using SysKeyAttribute = System.ComponentModel.DataAnnotations.KeyAttribute;
#endregion

namespace SQLBuilder.Entry
{
    /// <summary>
    /// SqlWrapper
    /// </summary>
	public class SqlWrapper
    {
        #region Private Field
        /// <summary>
        /// 表别名字典
        /// </summary>
        private readonly Dictionary<string, string> _aliasDictionary;

        /// <summary>
        /// 格式化列名缓存，多用于以数据库关键字命名的列
        /// </summary>
        private readonly List<string> _formatColumns;
        #endregion

        #region Public Property
        /// <summary>
        /// 是否单表操作
        /// </summary>
        public bool IsSingleTable { get; set; }

        /// <summary>
        /// 更新和新增时，是否对null值属性进行sql拼接操作
        /// </summary>
        public bool IsEnableNullValue { get; set; } = false;

        /// <summary>
        /// 是否启用表名和列名格式化
        /// </summary>
        public bool IsEnableFormat { get; set; } = false;

        /// <summary>
        /// 默认T类型
        /// </summary>
        public Type DefaultType { get; set; }

        /// <summary>
        /// 已选择的表字段
        /// </summary>
        public List<string> SelectFields { get; set; }

        /// <summary>
        /// 已选择字段数量
        /// </summary>
        public int FieldCount => this.SelectFields.Count;

        /// <summary>
        /// 已选择的表字段拼接字符串
        /// </summary>
        public string SelectFieldsString => string.Join(",", this.SelectFields);

        /// <summary>
        /// 当前sql
        /// </summary>
        public StringBuilder Sql { get; set; }

        /// <summary>
        /// 当前sql长度
        /// </summary>
        public int Length => this.Sql.Length;

        /// <summary>
        /// 数据库类型
        /// </summary>
        public DatabaseType DatabaseType { get; set; }

        /// <summary>
        /// 数据库参数
        /// </summary>
        public Dictionary<string, object> DbParameters { get; set; }

        /// <summary>
        /// 数据参数化前缀
        /// </summary>
        public string DbParameterPrefix =>
            this.DatabaseType switch
            {
                DatabaseType.Sqlite => "@",
                DatabaseType.SqlServer => "@",
                DatabaseType.MySql => "?",
                DatabaseType.Oracle => ":",
                DatabaseType.PostgreSql => ":",
                _ => "",
            };

        /// <summary>
        /// 格式化模板
        /// </summary>
        public string FormatTemplate =>
            this.DatabaseType switch
            {
                DatabaseType.Sqlite => "\"{0}\"",
                DatabaseType.SqlServer => "[{0}]",
                DatabaseType.MySql => "`{0}`",
                DatabaseType.Oracle => "\"{0}\"",
                DatabaseType.PostgreSql => "\"{0}\"",
                _ => "{0}",
            };

        /// <summary>
        /// 已Join的表实体类型
        /// </summary>
        public List<Type> JoinTypes { get; set; }

        /// <summary>
        /// 是否为Having语法
        /// </summary>
        public bool IsHavingSyntax { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// 构造函数
        /// </summary>
        public SqlWrapper()
        {
            this.Sql = new();
            this.SelectFields = new();
            this.DbParameters = new();
            this.JoinTypes = new();
            this._aliasDictionary = new();
            this._formatColumns = new();
        }
        #endregion

        #region Public Methods
        #region [index]
        /// <summary>
        /// 索引器
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns>char</returns>
        public char this[int index] => this.Sql[index];
        #endregion

        #region Operator
        /// <summary>
        /// 操作符
        /// </summary>
        /// <param name="sqlWrapper">sql包装器</param>
        /// <param name="sql">sql语句</param>
        /// <returns>SqlWrapper</returns>
        public static SqlWrapper operator +(SqlWrapper sqlWrapper, string sql)
        {
            sqlWrapper.Sql.Append(sql);
            return sqlWrapper;
        }

        /// <summary>
        /// 操作符
        /// </summary>
        /// <param name="sqlWrapper">sql包装器</param>
        /// <param name="sql">sql语句</param>
        /// <returns>SqlWrapper</returns>
        public static SqlWrapper operator +(SqlWrapper sqlWrapper, object sql)
        {
            sqlWrapper.Sql.Append(sql);
            return sqlWrapper;
        }
        #endregion

        #region Remove
        /// <summary>
        /// 移除字符串
        /// </summary>
        /// <param name="startIndex">起始索引位置</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        public SqlWrapper Remove(int startIndex, int length)
        {
            this.Sql.Remove(startIndex, length);
            return this;
        }

        /// <summary>
        /// 判断当前SqlWrapper最后一个字符是否为目标字符，如果是则进行移除，否则不进行任何处理
        /// </summary>
        /// <param name="target">目标字符</param>
        /// <returns></returns>
        public SqlWrapper RemoveLast(char target)
        {
            if (this[this.Length - 1] == target)
                this.Remove(this.Length - 1, 1);

            return this;
        }
        #endregion

        #region Replace
        /// <summary>
        /// 替换字符串
        /// </summary>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        /// <returns></returns>
        public SqlWrapper Replace(string oldValue, string newValue)
        {
            this.Sql.Replace(oldValue, newValue);
            return this;
        }

        /// <summary>
        /// 替换字符串
        /// </summary>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        /// <param name="startIndex">起始索引位置</param>
        /// <param name="count">长度</param>
        /// <returns></returns>
        public SqlWrapper Replace(string oldValue, string newValue, int startIndex, int count)
        {
            this.Sql.Replace(oldValue, newValue, startIndex, count);
            return this;
        }
        #endregion

        #region Append
        /// <summary>
        /// 追加字符串
        /// </summary>
        /// <param name="value">字符串</param>
        /// <returns></returns>
        public SqlWrapper Append(string value)
        {
            this.Sql.Append(value);
            return this;
        }

        /// <summary>
        /// 追加字符串
        /// </summary>
        /// <param name="value">对象值</param>
        /// <returns></returns>
        public SqlWrapper Append(object value)
        {
            this.Sql.Append(value);
            return this;
        }

        /// <summary>
        /// 追加格式化字符串
        /// </summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public SqlWrapper AppendFormat(string format, params object[] args)
        {
            this.Sql.AppendFormat(format, args);
            return this;
        }
        #endregion

        #region Insert
        /// <summary>
        /// 插入字符串
        /// </summary>
        /// <param name="index">起始索引位置</param>
        /// <param name="value">要插入的字符串</param>
        /// <returns></returns>
        public SqlWrapper Insert(int index, string value)
        {
            this.Sql.Insert(index, value);
            return this;
        }
        #endregion

        #region IndexOf
        /// <summary>
        /// 索引
        /// </summary>
        /// <param name="value">字符串</param>
        /// <returns></returns>
        public int IndexOf(string value)
        {
            return this.ToString().IndexOf(value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 索引
        /// </summary>
        /// <param name="value">字符串</param>
        /// <param name="startIndex">起始索引位置</param>
        /// <returns></returns>
        public int IndexOf(string value, int startIndex)
        {
            return this.ToString().IndexOf(value, startIndex, StringComparison.OrdinalIgnoreCase);
        }
        #endregion

        #region LastIndexOf
        /// <summary>
        /// 索引
        /// </summary>
        /// <param name="value">字符串</param>
        /// <returns></returns>
        public int LastIndexOf(string value)
        {
            return this.ToString().LastIndexOf(value, StringComparison.OrdinalIgnoreCase);
        }
        #endregion

        #region EndsWith
        /// <summary>
        /// 是否已指定字符串结尾
        /// </summary>
        /// <param name="value">指定的字符串</param>
        /// <returns></returns>
        public bool EndsWith(params string[] value)
        {
            return this.ToString().Trim().EndsWithIgnoreCase(value);
        }
        #endregion

        #region Substring
        /// <summary>
        /// 从分隔符开始向尾部截取字符串
        /// </summary>
        /// <param name="value">分隔字符串</param>
        /// <returns></returns>
        public string Substring(string value)
        {
            return this.ToString().ToUpper().Substring(value);
        }

        /// <summary>
        /// 截取字符串
        /// </summary>
        /// <param name="startIndex">起始索引位置</param>
        /// <param name="length">截取长度</param>
        /// <returns></returns>
        public string Substring(int startIndex, int length)
        {
            return this.ToString().ToUpper().Substring(startIndex, length);
        }
        #endregion

        #region Contains
        /// <summary>
        /// 是否包含指定字符串，忽略大小写
        /// </summary>
        /// <param name="value">目标字符串</param>
        /// <returns></returns>
        public bool Contains(params string[] value)
        {
            return this.ToString().ContainsIgnoreCase(value);
        }
        #endregion

        #region Clear
        /// <summary>
        /// 清空
        /// </summary>
        public void Clear()
        {
            this.Sql.Clear();
            this.DbParameters.Clear();
            this.SelectFields.Clear();
            this._aliasDictionary.Clear();
            this._formatColumns.Clear();
        }
        #endregion

        #region Reset
        /// <summary>
        /// 重置清空所有数据
        /// </summary>
        /// <returns></returns>
        public SqlWrapper Reset()
        {
            this.Sql.Clear();
            return this;
        }

        /// <summary>
        /// 重置为指定sql语句
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public SqlWrapper Reset(string sql)
        {
            this.Reset().Append(sql);
            return this;
        }

        /// <summary>
        /// 重置为指定sql语句
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public SqlWrapper Reset(StringBuilder sql)
        {
            this.Reset().Append(sql);
            return this;
        }
        #endregion

        #region JoinType
        /// <summary>
        /// 添加已Join的表实体类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public SqlWrapper AddJoinType(Type type)
        {
            this.JoinTypes.Add(type);
            return this;
        }

        /// <summary>
        /// 是否已被Join
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool IsJoined(Type type)
        {
            return this.JoinTypes.Contains(type);
        }
        #endregion

        #region AddField
        /// <summary>
        /// 添加已选择字段
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public SqlWrapper AddField(string field)
        {
            this.SelectFields.Add(field);
            return this;
        }
        #endregion

        #region AddDbParameter
        /// <summary>
        /// 新增格式化参数
        /// </summary>
        /// <param name="parameterValue">参数值</param>
        /// <param name="parameterKey">参数名称</param>
        public void AddDbParameter(object parameterValue, string parameterKey = null)
        {
            if (parameterValue == null || parameterValue == DBNull.Value)
                this.Sql.Append("NULL");

            else if (parameterKey.IsNullOrEmpty())
            {
                var name = this.DbParameterPrefix + "p__" + (this.DbParameters.Count + 1);
                this.DbParameters.Add(name, parameterValue);
                this.Sql.Append(name);
            }
            else
            {
                var name = this.DbParameterPrefix + parameterKey;
                this.DbParameters.Add(name, parameterValue);
                this.Sql.Append(name);
            }
        }
        #endregion

        #region SetTableAlias
        /// <summary>
        /// 设置表别名
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="tableAlias">表别名</param>
        /// <returns>bool</returns>
        public bool SetTableAlias(string tableName, string tableAlias = null)
        {
            if (tableAlias.IsNullOrEmpty())
                tableAlias = "t";

            tableAlias = this.GetFormatName(tableAlias);

            if (!this._aliasDictionary.Keys.Contains(tableAlias))
            {
                this._aliasDictionary.Add(tableAlias, tableName);
                return true;
            }

            return false;
        }
        #endregion

        #region GetTableAlias
        /// <summary>
        /// 获取表别名
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="tableAlias">表别名</param>
        /// <returns>string</returns>
        public string GetTableAlias(string tableName, string tableAlias = null)
        {
            if (!this.IsSingleTable)
            {
                if (tableAlias.IsNotNullOrEmpty())
                {
                    tableAlias = this.GetFormatName(tableAlias);

                    //表别名+表名 同时满足
                    if (_aliasDictionary.Keys.Contains(tableAlias) && _aliasDictionary[tableAlias] == tableName)
                        return tableAlias;
                }

                //根据表名获取别名
                if (_aliasDictionary.Values.Contains(tableName))
                    return _aliasDictionary.FirstOrDefault(x => x.Value == tableName).Key;
            }

            return string.Empty;
        }
        #endregion

        #region GetFormatName
        /// <summary>
        /// 获取格式化名称
        /// </summary>
        /// <param name="name">待格式化的原始名称</param>
        /// <param name="format">是否强制指定格式化</param>
        /// <returns></returns>
        public string GetFormatName(string name, bool format = false)
        {
            if ((format || this.IsEnableFormat) &&
                !name.IsNullOrEmpty() &&
                !name.StartsWith("[") &&
                !name.StartsWith("`") &&
                !name.StartsWith("\""))
                name = string.Format(this.FormatTemplate, name);

            return name;
        }
        #endregion

        #region GetTableName
        /// <summary>
        /// 获取表名
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>string</returns>
        public string GetTableName(Type type)
        {
            var tableName = this.GetFormatName(type.Name);

            if (type.GetFirstOrDefaultAttribute<CusTableAttribute>() is CusTableAttribute cta)
            {
                if (cta.Name.IsNotNullOrEmpty())
                    tableName = this.GetFormatName(cta.Name, cta.Format);

                if (cta.Schema.IsNotNullOrEmpty())
                    tableName = $"{this.GetFormatName(cta.Schema, cta.Format)}.{tableName}";
            }
            else if (type.GetFirstOrDefaultAttribute<SysTableAttribute>() is SysTableAttribute sta)
            {
                if (sta.Name.IsNotNullOrEmpty())
                    tableName = this.GetFormatName(sta.Name);

                if (sta.Schema.IsNotNullOrEmpty())
                    tableName = $"{this.GetFormatName(sta.Schema)}.{tableName}";
            }

            return tableName;
        }
        #endregion

        #region GetColumnName
        /// <summary>
        /// 获取列名
        /// </summary>
        /// <param name="columnName">列名</param>
        /// <returns></returns>
        public string GetColumnName(string columnName)
        {
            var format = _formatColumns.Any(x => x.EqualIgnoreCase(columnName));

            return this.GetFormatName(columnName, format);
        }
        #endregion

        #region GetColumnInfo
        /// <summary>
        /// 获取列信息
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="member">成员</param>
        /// <returns>Tuple</returns>
        public (string columnName, bool isInsert, bool isUpdate) GetColumnInfo(Type type, MemberInfo member)
        {
            string columnName = null;
            var isInsert = true;
            var isUpdate = true;
            var format = false;

            //判断列成员信息是否为空
            if (member == null)
                return (columnName, isInsert, isUpdate);

            //反射获取属性
            var props = type.GetProperties();

            //判断列是否含有Column特性
            var hasColumnAttribute = props.Any(x => x.ContainsAttribute<CusColumnAttribute>());
            if (!hasColumnAttribute)
                hasColumnAttribute = props.Any(x => x.ContainsAttribute<SysColumnAttribute>());

            //含有Column特性
            if (hasColumnAttribute)
            {
                if (member.GetFirstOrDefaultAttribute<CusColumnAttribute>() is CusColumnAttribute cca)
                {
                    columnName = cca.Name;
                    isInsert = cca.Insert;
                    isUpdate = cca.Update;
                    format = cca.Format;
                }
                else if (member.GetFirstOrDefaultAttribute<SysColumnAttribute>() is SysColumnAttribute sca)
                    columnName = sca.Name;
                else
                {
                    var prop = props.Where(x => x.Name == member.Name).FirstOrDefault();
                    if (prop != null)
                    {
                        if (prop.GetFirstOrDefaultAttribute<CusColumnAttribute>() is CusColumnAttribute cus)
                        {
                            columnName = cus.Name;
                            isInsert = cus.Insert;
                            isUpdate = cus.Update;
                            format = cus.Format;
                        }
                        else if (prop.GetFirstOrDefaultAttribute<SysColumnAttribute>() is SysColumnAttribute sys)
                            columnName = sys.Name;
                    }
                }
            }

            //列名
            columnName ??= member.Name;

            //判断列是否含有key特性
            var hasKeyAttribute = props.Any(x => x.ContainsAttribute<CusKeyAttribute>());
            if (!hasKeyAttribute)
                hasKeyAttribute = props.Any(x => x.ContainsAttribute<SysKeyAttribute>());

            //含有Key特性
            if (hasKeyAttribute)
            {
                if (member.GetFirstOrDefaultAttribute<CusKeyAttribute>() is CusKeyAttribute cka)
                {
                    isUpdate = false;
                    format = cka.Format;

                    if (cka.Identity)
                        isInsert = false;

                    if (cka.Name.IsNotNullOrEmpty() && cka.Name != columnName)
                        columnName = cka.Name;
                }
                else if (member.GetFirstOrDefaultAttribute<SysKeyAttribute>() is SysKeyAttribute)
                {
                    isUpdate = false;

                    //判断是否自增
                    if (member.GetFirstOrDefaultAttribute<DatabaseGeneratedAttribute>() is DatabaseGeneratedAttribute dga)
                    {
                        if (dga.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)
                            isInsert = false;
                    }
                }
                else
                {
                    var prop = props.Where(x => x.Name == member.Name).FirstOrDefault();
                    if (prop != null)
                    {
                        if (prop.GetFirstOrDefaultAttribute<CusKeyAttribute>() is CusKeyAttribute cus)
                        {
                            isUpdate = false;
                            format = cus.Format;

                            if (cus.Identity)
                                isInsert = false;

                            if (cus.Name.IsNotNullOrEmpty() && cus.Name != columnName)
                                columnName = cus.Name;
                        }
                        else if (prop.GetFirstOrDefaultAttribute<SysKeyAttribute>() is SysKeyAttribute)
                        {
                            isUpdate = false;

                            //判断是否自增
                            if (prop.GetFirstOrDefaultAttribute<DatabaseGeneratedAttribute>() is DatabaseGeneratedAttribute dg)
                            {
                                if (dg.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)
                                    isInsert = false;
                            }
                        }
                    }
                }
            }

            if (format)
                _formatColumns.Add(columnName);

            return (this.GetColumnName(columnName), isInsert, isUpdate);
        }
        #endregion

        #region GetPrimaryKey
        /// <summary>
        /// 获取主键
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>List Tuple</returns>
        public List<(string key, string property)> GetPrimaryKey(Type type)
        {
            var result = new List<(string key, string property)>();
            var props = type.GetProperties();

            var hasColumnAttribute = props.Any(x => x.ContainsAttribute<CusKeyAttribute>());
            if (!hasColumnAttribute)
                hasColumnAttribute = props.Any(x => x.ContainsAttribute<SysKeyAttribute>());

            if (hasColumnAttribute)
            {
                var properties = props.Where(x => x.ContainsAttribute<CusKeyAttribute>()).ToList();
                if (properties.Count() == 0)
                    properties = props.Where(x => x.ContainsAttribute<SysKeyAttribute>()).ToList();

                foreach (var property in properties)
                {
                    var propertyName = property?.Name;
                    string keyName = null;

                    if (property?.GetFirstOrDefaultAttribute<CusKeyAttribute>() is CusKeyAttribute cka)
                    {
                        keyName = cka.Name ?? propertyName;
                        if (cka.Format)
                            _formatColumns.Add(keyName);
                    }
                    else if (property?.GetFirstOrDefaultAttribute<SysKeyAttribute>() is SysKeyAttribute ska)
                    {
                        keyName = propertyName;
                    }

                    result.Add((this.GetColumnName(keyName), propertyName));
                }
            }
            return result;
        }
        #endregion

        #region ToString
        /// <summary>
        /// 重写ToString方法
        /// </summary>
        /// <returns>string</returns>
        public override string ToString() => this.Sql.ToString();
        #endregion
        #endregion
    }
}