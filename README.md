# RepositoryHelpers

Extensions for HttpClient and Custom Repository based on dapper

###### This is the component, works on .NET Core and.NET Framework

**Info**

|Code Quality|Build|Nuget|Contributors|
| ------------------- | ------------------- | ------------------- | ------------------- |
|[![Codacy Badge](https://api.codacy.com/project/badge/Grade/ea9b954b18e942d4800825dccd6ef77c)](https://app.codacy.com/app/TBertuzzi/RepositoryHelpers?utm_source=github.com&utm_medium=referral&utm_content=TBertuzzi/RepositoryHelpers&utm_campaign=Badge_Grade_Dashboard)|[![Build status](https://ci.appveyor.com/api/projects/status/github/TBertuzzi/RepositoryHelpers?branch=master&svg=true)](https://ci.appveyor.com/project/ThiagoBertuzzi/repositoryhelpers)|[![NuGet](https://buildstats.info/nuget/RepositoryHelpers)](https://www.nuget.org/packages/RepositoryHelpers/)|[![GitHub contributors](https://img.shields.io/github/contributors/TBertuzzi/RepositoryHelpers.svg)](https://github.com/TBertuzzi/RepositoryHelpers/graphs/contributors)|


**Build History**

[![Build history](https://buildstats.info/appveyor/chart/ThiagoBertuzzi/repositoryhelpers?buildCount=7)](https://ci.appveyor.com/project/ThiagoBertuzzi/repositoryhelpers/history)

**Platform Support**

RepositoryHelpers is a .NET Standard 2.0 library.

**Database/Dapper Extensions**

Use the connection class to define the type of database and connection string

```csharp
 var connection = new Connection()
 {
     Database = RepositoryHelpers.Utils.DataBaseType.SqlServer, //RepositoryHelpers.Utils.DataBaseType.Oracle
     ConnectionString = "Your string"
 };
```

Create a CustomRepository of the type of object you want to return

```csharp
  var Repository = new CustomRepository<User>(conecction);
``````

Mapping with Attributes:

```csharp
[DapperIgnore] // Property will be ignored in select, insert and update
public string InternalControl { get; set; }
[PrimaryKey] // Primary key
public int MyCustomId { get; set; }
[PrimaryKey]
[IdentityIgnore] //Primary key ignoring Identity
public int MyBdIdIndentity { get; set; }

//You can optionally map the name of the Database table that refers to the entity
[Table("Product")] 
public class Products
{
    public int Id { get; set; }
}

``````

Mapping with FluentMapper:

Install and use the [**Dapper.FluentMap.Dommel**](https://github.com/henkmollema/Dapper-FluentMap#dommel) package to map your entities by creating the specific classes inherited from *DommelEntityMap*:

```csharp
public class ProductMap : DommelEntityMap<Product>
{
    public ProductMap()
    {
        Map(p => p.Id).IsKey().IsIdentity();
        Map(p => p.Category).Ignore();
    }
}

```

You can define the name of the table that will be mapped

```csharp
public class ProductMap : DommelEntityMap<Product>
{
    public ProductMap()
    {
        ToTable("Product");
        Map(p => p.Id).IsKey().IsIdentity();
        Map(p => p.Category).Ignore();
    }
}

```


After that, you must configure Dapper.FluentMap.Dommel in RepositoryHelpers:

```csharp
Mapper.Initialize(c =>
{
    c.AddMap(new ProductMap());
});

```

Get Data:

To get results just use the Get method. can be syncronous or asynchronous

```csharp
//Get All Users
var usersAsync = await Repository.GetAsync();
var users = Repository.Get();

//Get User by Id
var userAsync = await Repository.GetByIdAsync(1);
var user = Repository.GetById(1);

//Get by CustomQuery with parameters
var customQuery = "Select name from user where login = @userLogin";
var parameters = new Dictionary<string, object> { { "userLogin", "bertuzzi" } };
var resultASync = await Repository.GetAsync(customQuery, parameters);
var result = Repository.Get(customQuery, parameters);

//Get by CustomQuery without parameters
var customQuery = "Select * from user";
var resultASync = await Repository.GetAsync(customQuery);
var result = Repository.Get(customQuery);

//Get by multi-mapping custom query with 2 input types
var customQuery = "Select * from user inner join category on user.categoryId = category.Id where login = @userLogin";
var user = Repository.Get<User, Category, User>(
    customQuery,
    map: (user, category) => 
    {
        user.Category = category;
        return user;
    });

//Get by multi-mapping custom query with an arbitrary number of input types
var customQuery = "Select * from user inner join category on user.categoryId = category.Id where login = @userLogin";
var user = Repository.Get(
    customQuery,
    new[] { typeof(User), typeof(Category) },
    map: (types) => 
    {
        var user = (types[0] as User);
        user.Category = (types[1] as Category);
        return user;
    });
```

Insert Data :

user identity parameter to return the id if your insert needs

```csharp
Repository.Insert(NewUser, true);
```

Update data

```csharp
Repository.Update(updateUser);
Repository.UpdateAsync(updateUser);
```

Delete data

```csharp
Repository.Delete(1);
Repository.DeleteAsync(1);
```

You can use ADO if you need

```csharp
//Return DataSet
var customQuery = "Select name from user where login = @userLogin";
var parameters = new Dictionary<string, object> { { "userLogin", "bertuzzi" } };
Repository.GetDataSet(customQuery, parameters);

//ExecuteQuery
Repository.ExecuteQueryAsync();
Repository.ExecuteQuery();

//ExecuteScalar
Repository.ExecuteScalarAsync();
Repository.ExecuteScalar();

//ExecuteProcedure
Repository.ExecuteProcedureAsync();
Repository.ExecuteProcedure();
```

CustomTransaction is possible to use transaction

```csharp

CustomTransaction customTransaction = new CustomTransaction(YourConnection);

customTransaction.BeginTransaction();
customTransaction.CommitTransaction();
customTransaction.RollbackTransaction();

//Sample
Repository.ExecuteQuery("yourquery", parameters, customTransaction);


```


DapperIgnore : if you want some property of your object to be ignored by Dapper, when inserting or updating, just use the attribute.
PrimaryKey : Define your primary key. It is used for queries, updates, and deletes.
IdentityIgnore: Determines that the field has identity, autoincrement ... Warns the repository to ignore it that the database will manage the field

*TIP Create a ConnectionHelper for BaseRepository and BaseTransaction to declare the connection only once :

```csharp

 public sealed class ConnectionHelper
    {
        static ConnectionHelper _instance;
        public static ConnectionHelper Instance
        {
            get { return _instance ?? (_instance = new ConnectionHelper()); }
        }
        private ConnectionHelper() 
        {
            Connection = new Connection()
            {
                Database = RepositoryHelpers.Utils.DataBaseType.SqlServer,
                ConnectionString = "YourString"
            };
        }
        public Connection Connection { get; }
    }
    
 public class BaseRepository<T>
    {
        protected readonly CustomRepository<T> Repository;

        protected BaseRepository()
        {
            Repository = new CustomRepository<T>(ConnectionHelper.Instance.Connection);
        }
    }
    
     public class BaseTransaction : CustomTransaction
    {
        public BaseTransaction() :
             base(ConnectionHelper.Instance.Connection)
        {
           
        }
    }
    
```

**LiteDB Extensions**

coming soon ..

**HttpClient Extensions**

Extensions to make using HttpClient easy.

To enable and use Follow the doc : https://github.com/TBertuzzi/HttpExtension


Samples coming soon ..

Special Thanks to project contributors

* [André Secco](https://github.com/andreluizsecco/) 

Special Thanks users who reported bugs and helped improve the package :

* Thiago Vieira
* Luis Paulo Souza
* Alexandre Harich

The RepositoryHelpers was developed by [Thiago Bertuzzi](http://bertuzzi.com.br) under the [MIT license](LICENSE).
