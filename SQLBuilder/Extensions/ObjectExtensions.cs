﻿#region License
/***
* Copyright © 2018-2021, 张强 (943620963@qq.com).
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
using System.ComponentModel;

namespace SQLBuilder.Extensions
{
    /// <summary>
    /// object扩展类
    /// </summary>
    public static class ObjectExtensions
    {
        #region Like
        /// <summary>
        /// LIKE
        /// </summary>
        /// <param name="this">扩展对象自身</param>
        /// <param name="value">包含的字符串</param>
        /// <returns>bool</returns>
        public static bool Like(this object @this, string value) => true;
        #endregion

        #region NotLike
        /// <summary>
        /// NOT LIKE
        /// </summary>
        /// <param name="this">扩展对象自身</param>
        /// <param name="value">包含的字符串</param>
        /// <returns>bool</returns>
        public static bool NotLike(this object @this, string value) => true;
        #endregion

        #region In
        /// <summary>
        /// IN
        /// </summary>
        /// <typeparam name="T">IN数组里面的数据类型</typeparam>
        /// <param name="this">扩展对象自身</param>
        /// <param name="array">IN数组</param>
        /// <returns>bool</returns>
        public static bool In<T>(this object @this, params T[] array) => true;
        #endregion

        #region NotIn
        /// <summary>
        /// NOT IN
        /// </summary>
        /// <typeparam name="T">NOT IN数组里面的数据类型</typeparam>
        /// <param name="this">扩展对象自身</param>
        /// <param name="array">NOT IN数组</param>
        /// <returns>bool</returns>
        public static bool NotIn<T>(this object @this, params T[] array) => true;
        #endregion

        #region Count
        /// <summary>
        /// 聚合函数Count
        /// </summary>
        /// <param name="this"></param>
        /// <returns></returns>
        public static T Count<T>(this object @this) => default;
        #endregion

        #region Sum
        /// <summary>
        /// 聚合函数Sum
        /// </summary>
        /// <param name="this"></param>
        /// <returns></returns>
        public static T Sum<T>(this object @this) => default;
        #endregion

        #region Avg
        /// <summary>
        /// 聚合函数Avg
        /// </summary>
        /// <param name="this"></param>
        /// <returns></returns>
        public static T Avg<T>(this object @this) => default;
        #endregion

        #region Max
        /// <summary>
        /// 聚合函数Max
        /// </summary>
        /// <param name="this"></param>
        /// <returns></returns>
        public static T Max<T>(this object @this) => default;
        #endregion

        #region Min
        /// <summary>
        /// 聚合函数Min
        /// </summary>
        /// <param name="this"></param>
        /// <returns></returns>
        public static T Min<T>(this object @this) => default;
        #endregion

        #region ToSafeValue
        /// <summary>
        /// 转换为安全类型的值
        /// </summary>
        /// <param name="this">object对象</param>
        /// <param name="type">type</param>
        /// <returns>object</returns>
        public static object ToSafeValue(this object @this, Type type)
        {
            return @this == null ? null : Convert.ChangeType(@this, type.GetCoreType());
        }
        #endregion

        #region IsNull
        /// <summary>
        /// 是否为空
        /// </summary>
        /// <param name="this">object对象</param>
        /// <returns>bool</returns>
        public static bool IsNull(this object @this)
        {
            return @this == null || @this == DBNull.Value;
        }
        #endregion

        #region IsNotNull
        /// <summary>
        /// 是否为空
        /// </summary>
        /// <param name="this">object对象</param>
        /// <returns>bool</returns>
        public static bool IsNotNull(this object @this)
        {
            return !@this.IsNull();
        }
        #endregion

        #region To
        /// <summary>
        /// To
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="this"></param>
        /// <returns></returns>
        public static T To<T>(this object @this)
        {
            if (@this != null)
            {
                var targetType = typeof(T);

                if (@this.GetType() == targetType)
                    return (T)@this;

                var converter = TypeDescriptor.GetConverter(@this);
                if (converter != null)
                {
                    if (converter.CanConvertTo(targetType))
                        return (T)converter.ConvertTo(@this, targetType);
                }

                converter = TypeDescriptor.GetConverter(targetType);
                if (converter != null)
                {
                    if (converter.CanConvertFrom(@this.GetType()))
                        return (T)converter.ConvertFrom(@this);
                }

                if (@this == DBNull.Value)
                    return (T)(object)null;
            }

            return (T)@this;
        }
        #endregion
    }
}
