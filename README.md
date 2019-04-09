# RepositoryHelpers

Extensions for HttpClient and Custom Repository based on dapper

###### This is the component, works on .NET Core and.NET Framework

**NuGet**

|Name|Nuget|Code Quality|Build
| ------------------- | :------------------: |:------------------: |------------------: |
|RepositoryHelpers|[![NuGet](https://buildstats.info/nuget/RepositoryHelpers)](https://www.nuget.org/packages/RepositoryHelpers/)|[![Codacy Badge](https://api.codacy.com/project/badge/Grade/ea9b954b18e942d4800825dccd6ef77c)](https://app.codacy.com/app/TBertuzzi/RepositoryHelpers?utm_source=github.com&utm_medium=referral&utm_content=TBertuzzi/RepositoryHelpers&utm_campaign=Badge_Grade_Dashboard)|[![Build status](https://ci.appveyor.com/api/projects/status/github/TBertuzzi/RepositoryHelpers?branch=master&svg=true)](https://ci.appveyor.com/project/ThiagoBertuzzi/repositoryhelpers)|

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

Attributes:

```csharp
[DapperIgnore]
public string InternalControl { get; set; }
[PrimaryKey]
public int MyCustomId { get; set; }
[PrimaryKey]
[IdentityIgnore]
public int MyBdIdIndentity { get; set; }

``````
Get Data:

To get results just use the Get method. can be syncronous or asynchronous

```csharp
//Get All Users
var usersAsync = await Repository.GetAsync();
var users = Repository.Get();

//Get User by Id
var userAsync = await Repository.GetByIdAsync(1);
var user = Repository.GetById(1);

//Get by CustomQuery
var customQuery = "Select name from user where login = @userLogin";
var parameters = new Dictionary<string, object> { { "userLogin", "bertuzzi" } };
var resultASync = await Repository.GetAsync(customQuery, parameters);
var result = Repository.Get(customQuery, parameters);
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

If using ADO it is possible to use transaction

```csharp

Repository.BeginTransaction();
Repository.CommitTransaction();
Repository.RollbackTransaction();

```


DapperIgnore : if you want some property of your object to be ignored by Dapper, when inserting or updating, just use the attribute.
PrimaryKey : Define your primary key. It is used for queries, updates, and deletes.
IdentityIgnore: Determines that the field has identity, autoincrement ... Warns the repository to ignore it that the database will manage the field

*TIP Create a BaseRepository to declare the connection only once :

```csharp
public class BaseRepository<T> 
    {
        protected readonly CustomRepository<T> Repository;

        protected BaseRepository()
        {
           var connection = new Connection()
        {
           Database = RepositoryHelpers.Utils.DataBaseType.SqlServer, 
           ConnectionString = "Your string"
        };

            Repository = new CustomRepository<T>(connection);
        }
    }
```

**LiteDB Extensions**

coming soon ..

**HttpClient Extensions**

Extensions to make using HttpClient easy.

* GetAsync<T> : Gets the return of a Get Rest and converts to the object or collection of pre-defined objects.
You can use only the path of the rest method, or pass a parameter dictionary. In case the url has parameters.

```csharp
 public static async Task<ServiceResponse<T>> GetAsync<T>(this HttpClient httpClient, string address);
 public static async Task<ServiceResponse<T>> GetAsync<T>(this HttpClient httpClient, string address,
        Dictionary<string, string> values);
```


* PostAsync<T> : Use post service methods rest asynchronously and return objects if necessary. 

```csharp
 public static async Task<HttpResponseMessage> PostAsync(this HttpClient httpClient,string address, object dto);
 public static async Task<ServiceResponse<T>> PostAsync<T>(this HttpClient httpClient, string address, object dto);
```

* ServiceResponse<T> : Object that facilitates the return of requests Rest. It returns the Http code of the request, already converted object and the contents in case of errors.

```csharp
public class ServiceResponse<T>
{
  public HttpStatusCode StatusCode { get; private set; }

  public T Value { get; set; }

  public string Content { get; set; }

  public Exception Error { get; set; }
}
```

Example of use :

```csharp
public async Task<List<Model.Todo>> GetTodos()
 {
    try
    {

        //GetAsync Return with Object
        var response = await _httpClient.GetAsync<List<Model.Todo>>("todos");
           
        if (response.StatusCode == HttpStatusCode.OK)
        {
              return response.Value;
        }
        else
        {
            throw new Exception(
                   $"HttpStatusCode: {response.StatusCode.ToString()} Message: {response.Content}");
        }
    }
    catch (Exception ex)
    {
        throw new Exception(ex.Message);
    }
 }
```

Samples coming soon ..
