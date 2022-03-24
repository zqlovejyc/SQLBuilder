<p></p>

<p align="center">
<img src="https://zqlovejyc.gitee.io/zqutils-js/Images/SQL.png" height="80"/>
</p>

<div align="center">

[![star](https://gitee.com/zqlovejyc/SQLBuilder/badge/star.svg)](https://gitee.com/zqlovejyc/SQLBuilder/stargazers) [![fork](https://gitee.com/zqlovejyc/SQLBuilder/badge/fork.svg)](https://gitee.com/zqlovejyc/SQLBuilder/members) [![GitHub stars](https://img.shields.io/github/stars/zqlovejyc/SQLBuilder?logo=github)](https://github.com/zqlovejyc/SQLBuilder/stargazers) [![GitHub forks](https://img.shields.io/github/forks/zqlovejyc/SQLBuilder?logo=github)](https://github.com/zqlovejyc/SQLBuilder/network) [![GitHub license](https://img.shields.io/badge/license-Apache2-yellow)](https://github.com/zqlovejyc/SQLBuilder/blob/master/LICENSE) [![nuget](https://img.shields.io/nuget/v/Zq.SQLBuilder.svg?cacheSeconds=10800)](https://www.nuget.org/packages//Zq.SQLBuilder)

</div>

<div align="left">

.NET Framework4.5版本Expression表达式转换为SQL语句，支持SqlServer、MySql、Oracle、Sqlite、PostgreSql；基于Dapper实现了不同数据库对应的数据仓储Repository；

</div>

## 🌭 开源地址

- Gitee：[https://gitee.com/zqlovejyc/SQLBuilder](https://gitee.com/zqlovejyc/SQLBuilder)
- GitHub：[https://github.com/zqlovejyc/SQLBuilder](https://github.com/zqlovejyc/SQLBuilder)
- NuGet：[https://www.nuget.org/packages/Zq.SQLBuilder](https://www.nuget.org/packages/Zq.SQLBuilder)
- MyGet：[https://www.myget.org/feed/zq-myget/package/nuget/Zq.SQLBuilder](https://www.myget.org/feed/zq-myget/package/nuget/Zq.SQLBuilder)

## 🚀 快速入门

- #### ➕ 新增

```csharp
//新增
await _repository.InsertAsync(entity);

//批量新增
await _repository.InsertAsync(entities);

//新增
await SqlBuilder
        .Insert<MsdBoxEntity>(() =>
            entity)
        .ExecuteAsync(
            _repository);

//批量新增
await SqlBuilder
        .Insert<MsdBoxEntity>(() =>
            new[]
            {
                new UserInfo { Name = "张三", Sex = 2 },
                new UserInfo { Name = "张三", Sex = 2 }
            })
        .ExecuteAsync(
            _repository);

```

- #### 🗑 删除

```csharp
//删除
await _repository.DeleteAsync(entity);

//批量删除
await _repository.DeleteAsync(entitties);

//条件删除
await _repository.DeleteAsync<MsdBoxEntity>(x => x.Id == "1");

//删除
await SqlBuilder
        .Delete<MsdBoxEntity>()
        .Where(x =>
            x.Id == "1")
        .ExecuteAsync(
            _repository);

//主键删除
await SqlBuilder
        .Delete<MsdBoxEntity>()
        .WithKey("1")
        .ExecuteAsync(
            _repository);
```

- #### ✏ 更新

```csharp
//更新
await _repository.UpdateAsync(entity);

//批量更新
await _repository.UpdateAsync(entities);

//条件更新
await _repository.UpdateAsync<MsdBoxEntity>(x => x.Id == "1", () => entity);

//更新
await SqlBuilder
        .Update<MsdBoxEntity>(() =>
            entity,
            DatabaseType.MySql,
            isEnableFormat:true)
        .Where(x =>
            x.Id == "1")
        .ExecuteAsync(
            _repository);
```
- #### 🔍 查询

```csharp
//简单查询
await _repository.FindListAsync<MsdBoxEntity>(x => x.Id == "1");

//连接查询
await SqlBuilder
        .Select<UserInfo, UserInfo, Account, Student, Class, City, Country>((u, t, a, s, d, e, f) =>
            new { u.Id, UId = t.Id, a.Name, StudentName = s.Name, ClassName = d.Name, e.CityName, CountryName = f.Name })
        .Join<UserInfo>((x, t) =>
            x.Id == t.Id) //注意此处单表多次Join所以要指明具体表别名，否则都会读取第一个表别名
        .Join<Account>((x, y) =>
            x.Id == y.UserId)
        .LeftJoin<Account, Student>((x, y) =>
            x.Id == y.AccountId)
        .RightJoin<Student, Class>((x, y) =>
            x.Id == y.UserId)
        .InnerJoin<Class, City>((x, y) =>
            x.CityId == y.Id)
        .FullJoin<City, Country>((x, y) =>
            x.CountryId == y.Id)
        .Where(x =>
            x.Id != null)
        .ToListAsync(
            _repository);

//分页查询
var condition = LinqExtensions
                    .True<UserInfo, Account>()
                    .And((x, y) => 
                        x.Id == y.UserId)
                    .WhereIf(
                        !name.IsNullOrEmpty(), 
                        (x, y) => name.EndsWith("∞")
                        ? x.Name.Contains(name.Trim('∞'))
                        : x.Name == name);
var hasWhere = false;
await SqlBuilder
        .Select<UserInfo, Account>(
            (u, a) => new { u.Id, UserName = "u.Name" })
        .InnerJoin<Account>(
            condition)
        .WhereIf(
            !name.IsNullOrEmpty(),
            x => x.Email != null && 
            (!name.EndsWith("∞") ? x.Name.Contains(name.TrimEnd('∞', '*')) : x.Name == name),
            ref hasWhere)
        .WhereIf(
            !email.IsNullOrEmpty(),
            x => x.Email == email,
            ref hasWhere)
        .ToPageAsync(
            _repository.UseMasterOrSlave(false),
            input.OrderField,
            input.Ascending,
            input.PageSize,
            input.PageIndex);

//仓储分页查询
await _repository.FindListAsync(condition, input.OrderField, input.Ascending, input.PageSize, input.PageIndex);

//高级查询
Func<string[], string> @delegate = x => $"ks.{x[0]}{x[1]}{x[2]} WITH(NOLOCK)";

await SqlBuilder
        .Select<UserInfo, Account, Student, Class, City, Country>((u, a, s, d, e, f) =>
            new { u, a.Name, StudentName = s.Name, ClassName = d.Name, e.CityName, CountryName = f.Name },
            tableNameFunc: @delegate)
        .Join<Account>((x, y) =>
            x.Id == y.UserId,
            @delegate)
        .LeftJoin<Account, Student>((x, y) =>
            x.Id == y.AccountId,
            @delegate)
        .RightJoin<Class, Student>((x, y) =>
            y.Id == x.UserId,
            @delegate)
        .InnerJoin<Class, City>((x, y) =>
            x.CityId == y.Id,
            @delegate)
        .FullJoin<City, Country>((x, y) =>
            x.CountryId == y.Id,
            @delegate)
        .Where(u =>
            u.Id != null)
        .ToListAsync(
            _repository);

```

- #### 🎫 队列
```csharp
//预提交队列
_repository.AddQueue(async repo =>
    await repo.UpdateAsync<UserEntity>(
        x => x.Id == "1",
        () => new
        {
            Name = "test"
        }) > 0);

_repository.AddQueue(async repo =>
    await repo.DeleteAsync<UserEntity>(x =>
        x.Enabled == 1) > 0);

//统一提交队列，默认开启事务
var res = await _repository.SaveQueueAsync();
```
### 🌌 IOC注入

根据config配置自动注入不同类型数据仓储，支持一主多从配置

```csharp
//注入SQLBuilder仓储
var builder = new ContainerBuilder();
builder.AddSqlBuilder("Base", (sql, parameter) =>
{
    //写入文本日志
    if (WebHostEnvironment.IsDevelopment())
    {
        if (parameter is DynamicParameters dynamicParameters)
            _logger.LogInformation($@"SQL语句：{sql}  参数：{dynamicParameters
                .ParameterNames?
                .ToDictionary(k => k, v => dynamicParameters.Get<object>(v))
                .ToJson()}");
        else if (parameter is OracleDynamicParameters oracleDynamicParameters)
            _logger.LogInformation($@"SQL语句：{sql} 参数：{oracleDynamicParameters
                .OracleParameters
                .ToDictionary(k => k.ParameterName, v => v.Value)
                .ToJson()}");
        else
            _logger.LogInformation($"SQL语句：{sql}  参数：{parameter.ToJson()}");
    }

    //返回null，不对原始sql进行任何更改，此处可以修改待执行的sql语句
    return null;
});
```

### ⚙ 数据库配置

```csharp
//appSettings
<add key="ConnectionStrings" value="{'Base':['SqlServer','Server=.;Database=TestDb;Uid=test;Pwd=123;'],'OracleDb':['Oracle','数据库连接字符串'],'MySqlDb':['MySql','数据库连接字符串'],'SqliteDb':['Sqlite','数据库连接字符串'],'PgsqlDb':['PostgreSql','数据库连接字符串']}" />

//connectionStrings
<add name="" connectionString="{'Base':['SqlServer','Server=.;Database=TestDb;Uid=test;Pwd=123;'],'OracleDb':['Oracle','数据库连接字符串'],'MySqlDb':['MySql','数据库连接字符串'],'SqliteDb':['Sqlite','数据库连接字符串'],'PgsqlDb':['PostgreSql','数据库连接字符串']}"/>
```

### 📰 事务

```csharp
//方式一
IRepository trans = null;
try
{
    //开启事务
    trans = _repository.BeginTransaction();

    //数据库写操作
    await trans.InsertAsync(entity);

    //提交事务
    trans.Commit();
}
catch (Exception)
{
    //回滚事务
    trans?.Rollback();
    throw;
}

//方式二
var res = await _repository.ExecuteTransactionAsync(async trans =>
{
    var retval = (await trans.InsertAsync(entity)) > 0;

    if (input.Action.EqualIgnoreCase(UnitAction.InDryBox))
        code = await _unitInfoService.InDryBoxAsync(dryBoxInput);
    else
        code = await _unitInfoService.OutDryBoxAsync(dryBoxInput);

    return code == ErrorCode.Successful && retval;
});
```

### 🎣 读写分离

```csharp
//方式一
_repository.Master = false;

//方式二
_repository.UseMasterOrSlave(master)
```

## 🧪 测试文档

- 单元测试 [https://github.com/zqlovejyc/SQLBuilder/tree/master/SQLBuilder.UnitTest](https://github.com/zqlovejyc/SQLBuilder/tree/master/SQLBuilder.UnitTest)


## 🍻 贡献代码

`SQLBuilder` 遵循 `Apache-2.0` 开源协议，欢迎大家提交 `PR` 或 `Issue`。
