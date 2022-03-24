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
using Newtonsoft.Json;
using SQLBuilder.Enums;
using SQLBuilder.LoadBalancer;
using SQLBuilder.Repositories;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace SQLBuilder.Extensions
{
    /// <summary>
    /// ContainerBuilder扩展类
    /// </summary>
    public static class ContainerBuilderExtensions
    {
        #region RegisterRepository
        /// <summary>
        /// 注入泛型仓储
        /// </summary>
        /// <typeparam name="T">仓储类型</typeparam>
        /// <param name="this">autofac容器</param>
        /// <param name="sqlIntercept">sql拦截委托</param>
        /// <param name="isEnableFormat">是否启用对表名和列名格式化，默认：否</param>
        /// <param name="autoDispose">非事务的情况下，数据库连接是否自动释放，默认：是</param>
        /// <param name="countSyntax">分页计数语法，默认：COUNT(*)</param>
        /// <param name="lifeTime">生命周期，默认：Transient，不建议Singleton</param>
        /// <returns></returns>
        public static ContainerBuilder RegisterRepository<T>(
            this ContainerBuilder @this,
            Func<string, object, string> sqlIntercept = null,
            bool isEnableFormat = false,
            bool autoDispose = true,
            string countSyntax = "COUNT(*)",
            ServiceLifetime lifeTime = ServiceLifetime.Transient)
            where T : class, IRepository, new()
        {
            T TFactory(IComponentContext x) => new()
            {
                AutoDispose = autoDispose,
                SqlIntercept = sqlIntercept,
                IsEnableFormat = isEnableFormat,
                CountSyntax = countSyntax,
                LoadBalancer = x.Resolve<ILoadBalancer>()
            };

            IRepository IRepositoryFactory(IComponentContext x) => new T
            {
                AutoDispose = autoDispose,
                SqlIntercept = sqlIntercept,
                IsEnableFormat = isEnableFormat,
                CountSyntax = countSyntax,
                LoadBalancer = x.Resolve<ILoadBalancer>()
            };

            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.Register(TFactory).SingleInstance();
                    @this.Register(IRepositoryFactory).SingleInstance();
                    break;

                case ServiceLifetime.Scoped:
                    @this.Register(TFactory).InstancePerLifetimeScope();
                    @this.Register(IRepositoryFactory).InstancePerLifetimeScope();
                    break;

                case ServiceLifetime.Transient:
                    @this.Register(TFactory).InstancePerDependency();
                    @this.Register(IRepositoryFactory).InstancePerDependency();
                    break;

                default:
                    break;
            }

            return @this;
        }
        #endregion

        #region RegisterAllRepository
        /// <summary>
        /// 按需注入所有程序依赖的数据库仓储 <para>注意：仓储没有初始化MasterConnectionString和SlaveConnectionStrings</para>
        /// </summary>
        /// <param name="this">autofac容器</param>
        /// <param name="sqlIntercept">sql拦截委托</param>
        /// <param name="isEnableFormat">是否启用对表名和列名格式化，默认：否</param>
        /// <param name="autoDispose">非事务的情况下，数据库连接是否自动释放，默认：是</param>
        /// <param name="countSyntax">分页计数语法，默认：COUNT(*)</param>
        /// <param name="connectionStringName">连接字符串配置name</param>
        /// <param name="lifeTime">生命周期，默认：Transient，不建议Singleton</param>
        /// <returns></returns>
        public static ContainerBuilder RegisterAllRepository(
            this ContainerBuilder @this,
            Func<string, object, string> sqlIntercept = null,
            bool isEnableFormat = false,
            bool autoDispose = true,
            string countSyntax = "COUNT(*)",
            string connectionStringName = "ConnectionStrings",
            ServiceLifetime lifeTime = ServiceLifetime.Transient)
        {
            //数据库配置
            var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName]?.ConnectionString;
            if (connectionString.IsNullOrEmpty())
                connectionString = ConfigurationManager.AppSettings[connectionStringName];

            var configs = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(connectionString);

            //注入所有数据库
            if (configs.IsNotNull() && configs.Values.IsNotNull() && configs.Values.Any(x => x.IsNotNullOrEmpty()))
            {
                var databaseTypes = configs.Values.Where(x => x.IsNotNullOrEmpty()).Select(x => x[0].ToLower()).Distinct();
                foreach (var databaseType in databaseTypes)
                {
                    //SqlServer
                    if (databaseType.EqualIgnoreCase("SqlServer"))
                        @this.RegisterRepository<SqlRepository>(sqlIntercept, isEnableFormat, autoDispose, countSyntax, lifeTime);

                    //MySql
                    if (databaseType.EqualIgnoreCase("MySql"))
                        @this.RegisterRepository<MySqlRepository>(sqlIntercept, isEnableFormat, autoDispose, countSyntax, lifeTime);

                    //Oracle
                    if (databaseType.EqualIgnoreCase("Oracle"))
                        @this.RegisterRepository<OracleRepository>(sqlIntercept, isEnableFormat, autoDispose, countSyntax, lifeTime);

                    //Sqlite
                    if (databaseType.EqualIgnoreCase("Sqlite"))
                        @this.RegisterRepository<SqliteRepository>(sqlIntercept, isEnableFormat, autoDispose, countSyntax, lifeTime);

                    //PostgreSql
                    if (databaseType.EqualIgnoreCase("PostgreSql"))
                        @this.RegisterRepository<NpgsqlRepository>(sqlIntercept, isEnableFormat, autoDispose, countSyntax, lifeTime);
                }
            }

            return @this;
        }
        #endregion

        #region GetConnectionInformation
        /// <summary>
        /// 获取数据库连接信息
        /// </summary>
        /// <param name="key">数据库标识键值</param>
        /// <param name="defaultName">默认数据库名称</param>
        /// <param name="connectionStringName">连接字符串配置name</param>
        /// <returns></returns>
        public static (
            DatabaseType databaseType,
            string masterConnectionString,
            (string connectionString, int weight)[] SlaveConnectionStrings)
            GetConnectionInformation(
            string key,
            string defaultName,
            string connectionStringName)
        {
            //数据库标识键值
            key = key.IsNullOrEmpty() ? defaultName : key;

            //数据库配置
            var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName]?.ConnectionString;
            if (connectionString.IsNullOrEmpty())
                connectionString = ConfigurationManager.AppSettings[connectionStringName];

            var allConfigs = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(connectionString);

            //数据库配置
            var configs = allConfigs[key];

            //数据库类型
            var databaseType = (DatabaseType)Enum.Parse(typeof(DatabaseType), configs[0]);

            //从库连接集合
            var slaveConnectionStrings = new List<(string connectionString, int weight)>();
            if (configs.Count > 2)
            {
                for (int i = 2; i < configs.Count; i++)
                {
                    if (configs[i].IsNullOrEmpty() || !configs[i].Contains(";"))
                        continue;

                    var slaveConnectionStringArray = configs[i].Split(';');
                    var slaveConnectionString = string.Join(";", slaveConnectionStringArray.Where(x => x.IsNotNullOrEmpty() && !x.StartsWithIgnoreCase("weight")));
                    var weight = int.Parse(slaveConnectionStringArray.FirstOrDefault(x => x.StartsWithIgnoreCase("weight"))?.Split('=')[1] ?? "1");
                    slaveConnectionStrings.Add((slaveConnectionString, weight));
                }
            }

            return (databaseType, configs[1], slaveConnectionStrings.ToArray());
        }
        #endregion

        #region CreateRepositoryFactory
        /// <summary>
        /// 创建IRepository委托，依赖AddAllRepository注入不同类型仓储
        /// </summary>
        /// <param name="provider">autofac组件上下文</param>
        /// <param name="defaultName">默认数据库名称</param>
        /// <param name="connectionStringName">连接字符串配置name</param>
        /// <returns></returns>
        public static Func<string, IRepository> CreateRepositoryFactory(
            this IComponentContext provider,
            string defaultName,
            string connectionStringName = "ConnectionStrings")
        {
            //此处需要重新解析下IComponentContext，否则报异常：
            //This resolve operation has already ended. When registering components using lambdas, the IComponentContext 'c' parameter to the lambda cannot be stored. Instead, either resolve IComponentContext again from 'c', or resolve a Func<> based factory to
            provider = provider.Resolve<IComponentContext>();

            return key =>
            {
                //获取数据库连接信息
                var (databaseType, masterConnectionStrings, slaveConnectionStrings) =
                    GetConnectionInformation(key, defaultName, connectionStringName);

                //获取对应数据库类型的仓储
                IRepository repository = databaseType switch
                {
                    DatabaseType.SqlServer => provider.Resolve<SqlRepository>(),
                    DatabaseType.MySql => provider.Resolve<MySqlRepository>(),
                    DatabaseType.Oracle => provider.Resolve<OracleRepository>(),
                    DatabaseType.Sqlite => provider.Resolve<SqliteRepository>(),
                    DatabaseType.PostgreSql => provider.Resolve<NpgsqlRepository>(),
                    _ => throw new ArgumentException($"Invalid database type `{databaseType}`.")
                };

                repository.MasterConnectionString = masterConnectionStrings;
                repository.SlaveConnectionStrings = slaveConnectionStrings;

                return repository;
            };
        }
        #endregion

        #region RegisterSqlBuilder
        /// <summary>
        /// SQLBuilder仓储注入扩展
        /// <para>注意：若要启用读写分离，则需要注入ILoadBalancer服务；</para>
        /// </summary>
        /// <param name="this">autofac容器</param>
        /// <param name="defaultName">默认数据库名称</param>
        /// <param name="sqlIntercept">sql拦截委托</param>
        /// <param name="isEnableFormat">是否启用对表名和列名格式化，默认：否</param>
        /// <param name="autoDispose">非事务的情况下，数据库连接是否自动释放，默认：是</param>
        /// <param name="countSyntax">分页计数语法，默认：COUNT(*)</param>
        /// <param name="connectionStringName">连接字符串配置name</param>
        /// <param name="isInjectLoadBalancer">是否注入从库负载均衡，默认注入单例权重轮询方式(WeightRoundRobinLoadBalancer)，可以设置为false实现自定义方式</param>
        /// <param name="lifeTime">生命周期，默认：Transient，不建议Singleton</param>
        /// <returns></returns>
        /// <remarks>
        ///     <code>
        ///     //appSettings
        ///     &lt;add key="ConnectionStrings" value="{'Base':['SqlServer','Server=.;Database=TestDb;Uid=test;Pwd=123;']}" /&gt;
        ///     
        ///     //Controller获取方法
        ///     private readonly IRepository _repository;
        ///     public WeatherForecastController(Func&lt;string, IRepository&gt; handler)
        ///     {
        ///         _repository = handler("Sqlserver");
        ///     }
        ///     </code>
        /// </remarks>
        public static ContainerBuilder RegisterSqlBuilder(
            this ContainerBuilder @this,
            string defaultName,
            Func<string, object, string> sqlIntercept = null,
            bool isEnableFormat = false,
            bool autoDispose = true,
            string countSyntax = "COUNT(*)",
            string connectionStringName = "ConnectionStrings",
            bool isInjectLoadBalancer = true,
            ServiceLifetime lifeTime = ServiceLifetime.Transient)
        {
            //注入负载均衡
            if (isInjectLoadBalancer)
                @this.RegisterType<WeightRoundRobinLoadBalancer>().As<ILoadBalancer>().SingleInstance();

            //按需注入所有依赖的仓储
            @this.RegisterAllRepository(sqlIntercept, isEnableFormat, autoDispose, countSyntax, connectionStringName, lifeTime);

            //根据生命周期类型注入服务
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.Register(x => x.CreateRepositoryFactory(defaultName, connectionStringName)).SingleInstance();
                    break;

                case ServiceLifetime.Transient:
                    @this.Register(x => x.CreateRepositoryFactory(defaultName, connectionStringName)).InstancePerDependency();
                    break;

                case ServiceLifetime.Scoped:
                    @this.Register(x => x.CreateRepositoryFactory(defaultName, connectionStringName)).InstancePerLifetimeScope();
                    break;

                default:
                    break;
            }

            return @this;
        }
        #endregion
    }
}
