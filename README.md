# RepositoryHelpers

Extensions for HttpClient and Custom Repository based on dapper

###### This is the component, works on .NET Core and.NET Framework

**NuGet**

|Name|Info|
| ------------------- | :------------------: |
|RepositoryHelpers|[![NuGet](https://img.shields.io/badge/nuget-1.0.0-blue.svg)](https://www.nuget.org/packages/RepositoryHelpers/)|

**Platform Support**

RepositoryHelpers is a .NET Standard 2.0 library.

**Database/Dapper Extensions**

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
