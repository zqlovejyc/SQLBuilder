#region License
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

using Autofac;
using Dapper;
using SQLBuilder.Diagnostics.Diagnostics;
using SQLBuilder.Extensions;
using SQLBuilder.Parameters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;

namespace SQLBuilder.Diagnostics.Extensions
{
    /// <summary>
    /// SqlBuilder日志诊断扩展类
    /// </summary>
    public static class SqlBuilderDiagnosticExtensions
    {
        /// <summary>
        /// 获取sql参数json
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private static string GetParameterJson(object parameters)
        {
            var parameterJson = string.Empty;
            if (parameters is DynamicParameters dynamicParameters)
                parameterJson = dynamicParameters
                    .ParameterNames?
                    .ToDictionary(k => k, v => dynamicParameters.Get<object>(v))
                    .ToJson();
            else if (parameters is OracleDynamicParameters oracleDynamicParameters)
                parameterJson = oracleDynamicParameters
                    .OracleParameters
                    .ToDictionary(k => k.ParameterName, v => v.Value)
                    .ToJson();
            else
                parameterJson = parameters.ToJson();

            return parameterJson;
        }

        /// <summary>
        /// 注入SqlBuilder日志诊断
        /// </summary>
        /// <param name="this"></param>
        /// <param name="executeBefore">执行前</param>
        /// <param name="executeAfter">执行后</param>
        /// <param name="executeError">执行异常</param>
        /// <param name="executeDispose">执行数据库连接释放</param>
        /// <param name="disposeError">数据库连接释放异常</param>
        /// <returns></returns>
        public static ContainerBuilder RegisterSqlBuilderDiagnostic(
            this ContainerBuilder @this,
            Action<SqlBuilderDiagnosticBeforeMessage> executeBefore = null,
            Action<SqlBuilderDiagnosticAfterMessage> executeAfter = null,
            Action<SqlBuilderDiagnosticErrorMessage> executeError = null,
            Action<SqlBuilderDiagnosticDisposeMessage> executeDispose = null,
            Action<SqlBuilderDiagnosticErrorMessage> disposeError = null)
        {
            var enableDiagnosticListener = bool.Parse(ConfigurationManager.AppSettings["SqlBuilder.EnableDiagnosticListener"] ?? "false");

            if (!enableDiagnosticListener)
                return @this;

            //日志诊断订阅
            DiagnosticListener.AllListeners.Subscribe(new SqlBuilderObserver<DiagnosticListener>(listener =>
            {
                //判断发布者的名字
                if (listener.Name != DiagnosticStrings.DiagnosticListenerName)
                    return;

                //获取订阅信息
                listener.Subscribe(new SqlBuilderObserver<KeyValuePair<string, dynamic>>(listenerData =>
                {
                    var now = DateTime.Now;

                    //执行前
                    if (listenerData.Key == DiagnosticStrings.BeforeExecute && executeBefore != null)
                        executeBefore(new SqlBuilderDiagnosticBeforeMessage
                        {
                            Sql = listenerData.Value.Sql,
                            ParameterJson = GetParameterJson(listenerData.Value.Parameters),
                            DatabaseType = listenerData.Value.DatabaseType,
                            DataSource = listenerData.Value.DataSource,
                            Timespan = listenerData.Value.Timestamp,
                            OperationId = listenerData.Value.OperationId,
                            ExecuteBefore = now
                        });

                    //执行后
                    if (listenerData.Key == DiagnosticStrings.AfterExecute && executeAfter != null)
                        executeAfter(new SqlBuilderDiagnosticAfterMessage
                        {
                            Sql = listenerData.Value.Sql,
                            ParameterJson = GetParameterJson(listenerData.Value.Parameters),
                            DataSource = listenerData.Value.DataSource,
                            OperationId = listenerData.Value.OperationId,
                            ElapsedMilliseconds = listenerData.Value.ElapsedMilliseconds,
                            ExecuteAfter = now
                        });

                    //执行异常
                    if (listenerData.Key == DiagnosticStrings.ErrorExecute && executeError != null)
                        executeError(new SqlBuilderDiagnosticErrorMessage
                        {
                            Exception = listenerData.Value.Exception,
                            OperationId = listenerData.Value.OperationId,
                            ElapsedMilliseconds = listenerData.Value.ElapsedMilliseconds,
                            ExecuteError = now
                        });

                    //资源释放
                    if (listenerData.Key == DiagnosticStrings.DisposeExecute && executeDispose != null)
                    {
                        var disposeData = listenerData.Value as object;
                        var dataType = disposeData.GetType();

                        var masterConnection = dataType.GetProperty("masterConnection").GetValue(disposeData) as DbConnection;
                        var salveConnection = dataType.GetProperty("salveConnection").GetValue(disposeData) as DbConnection;

                        executeDispose(new SqlBuilderDiagnosticDisposeMessage
                        {
                            MasterConnection = masterConnection,
                            SalveConnection = salveConnection
                        });
                    }

                    //资源释放异常
                    if (listenerData.Key == DiagnosticStrings.DisposeException && disposeError != null)
                    {
                        var disposeErrorData = listenerData.Value as object;

                        var exception = disposeErrorData.GetType().GetProperty("exception").GetValue(disposeErrorData) as Exception;

                        disposeError(new SqlBuilderDiagnosticErrorMessage
                        {
                            Exception = exception,
                            ExecuteError = now
                        });
                    }
                }));
            }));

            return @this;
        }
    }
}
