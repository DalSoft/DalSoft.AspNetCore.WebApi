# DalSoft.AspNetCore.WebApi

A small helper package to help you with WebApi on ASP.NET Core. It's mainly for myself at the moment to avoid writing the same code 
over and over again. Hopefully it might help you too :wink:

As you would expect each namespace divides up the helper functionality. If and when this package gets unwieldy I'll split it up into smaller packages.

### Installing

#### .NET CLI
```bash
> dotnet add package DalSoft.AspNetCore.WebApi
```

#### NuGet
```bash
PM> Install-Package DalSoft.AspNetCore.WebApi
```
## ErrorHandling

The ErrorHandling namespace aims to give you a consistent pattern to retrieve and return error responses. It extends the great work the ASP.NET Core team has done to [implement  Problem Details RFC 7807](https://devblogs.microsoft.com/aspnet/asp-net-core-2-1-web-apis/#problem-details) by providing centralized error handling.

### ProblemDetails

Enums decorated with the ProblemDetailsAttribute are used to describe known errors (problem types](https://tools.ietf.org/html/rfc7807)) that will be returned to clients using your API.

The Enum's named constants are used for the problem type (and for lookup). In the example below this would be NotOnMonday. The properties of the ProblemDetailsAttribute are used to create a new [ASP.NET Core ProblemDetails object](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.problemdetails?view=aspnetcore-2.2).

```cs
public enum DeveloperProblemsTypes
{
    [ProblemDetails(
        title:"Sorry we're shut on Mondays.", 
        details: "We want developers to have a happy Monday :)",
        status:HttpStatusCode.BadRequest
    )]
    NotOnMonday,
    
    [ProblemDetails(
        title:"Sorry we're shut on Fridays.", 
        details: "Developers have their slack time on Fridays.")",
        status:HttpStatusCode.NotAcceptable
    )]
    NotOnFriday
}
```

#### Registering Problem Types

Now we have created our problem typess using the `ProblemDetailsAttribute` it's time to tell DI about them, to do this add the following code to Startup.cs.

```cs
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddProblemDetailFactory(new Uri("/docs", UriKind.Relative))
        .AddOrUpdateProblemDetails<DeveloperProblemsTypes>()
        .ConfigureInvalidModelStateResponseFactory();
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
{
    app.UseProblemDetailExceptionHandler(env);
}
```

`AddProblemDetailFactory` Registers the ProblemDetailFactory and other required services. This will allow us to register, update and retrive our problem types. This extends what the ASP.NET Core team has done by allowing you to set the base URI for your documentation. The enum constant name is then appended (formatted as snake case) to the base URI on retrival giving us our [problem  type](https://tools.ietf.org/html/rfc7807#section-4) - in our example this would be /docs/not_on_monday, see usage for example of the responses.  

`AddOrUpdateProblemDetails` Registers problem types with the ConcurrentDictionary using an enum decorated with  ProblemDetailsAttributes. Notice the enum is a generic paramater this allows you to add multiple problem type enums ([enums were added as generic constraints in C# 7.3](https://blogs.msdn.microsoft.com/seteplia/2018/06/12/dissecting-new-generics-constraints-in-c-7-3/)).

AddOrUpdateProblemDetails also takes IEnumerable<ProblemDetails> which is useful for registering/updating problem types from a database. Problem types are registered/updated in the order specified. Registering the same problem type will *update it's details*. This means you can resgister your problems types intially as enums for easy lookup, and update the details from a database.  

`ConfigureInvalidModelStateResponseFactory` Optional get a validation problem type anytime a client calls a controller and ModelState is not Valid. This stops you having to have an if statement checking that ModelState IsValid. It's the equivalent of `Problem(DefaultProblemsDetails.ValidationFailed, ModelState)`. See DefaultProblemTypes for more details.

`UseProblemDetailExceptionHandler` Optional automatically respond with `DefaultProblemsDetails.InternalServerError` if an unhandled exception is thrown, and respond with a `ProblemDetailsResponse` if a `ProblemDetailsException` is thrown. See `ProblemDetailExceptionHandler` for more details.

#### Usage

Now we have registered the ProblemDetailFactory with DI and registered one or more problem types we are ready to use them in our controllers etc.

#### ProblemDetailsActionResultFactory

Injecting the ProblemDetailsActionResultFactory into our controller allows us to return a `ProblemDetailsResponse` from an controller's action.

```cs
[ApiController, Route("[Controller]")]
public class TestProblemDetailsController : ControllerBase
{
    private readonly ProblemDetailsActionResultFactory _problemDetailsActionResultFactory;
    
    public TestProblemDetailsControllerProblemDetailsActionResultFactory problemDetailsActionResultFactory)
    {
        _problemDetailsActionResultFactory = problemDetailsActionResultFactory;
    }
        
    [HttpGet]
    public ActionResult Get()
    {
       return _problemDetailsActionResultFactory.Problem(DeveloperProblemsDetails.NotOnMonday);
    }
}
```

This responds with a status code of 400 Bad Request and this response body:

```javascript
{
  "type": "/docs/not_on_monday",
  "title": "Sorry we're shut on Mondays.",
  "details:" "We want developers to have a happy Monday :)",
  "status": 400,
  "instance": "https://localhost/TestProblemDetails"
}
```

> Note the only difference between getting a ProblemDetailsResponse from `ProblemDetailsActionResultFactory Problem` vs `ProblemDetailsFactory ResponseFor` is that it wraps it in a ObjectResult, sets the status code and instance for you (if your didn't add it via the `ProblemDetailsAttribute`).

`Problem` has overloads that take ModelState and Extensions, and you can look up a regsistered problem type using a string rather than enum. Below is an example using a overload of the Problem method.

```cs
[ApiController, Route("[Controller]")]
public class TestProblemDetailsController : ControllerBase
.   private readonly ProblemDetailsActionResultFactory _problemDetailsActionResultFactory;

    public TestProblemDetailsControllerProblemDetailsActionResultFactory problemDetailsActionResultFactory)
    {
        _problemDetailsActionResultFactory = problemDetailsActionResultFactory;
    }
        
    [HttpGet]
    public ActionResult Get()
    {
       ModelState.AddModelError("email", "Email address is invalid");
       return _problemDetailsActionResultFactory.Problem("NotOnMonday", ModelState);
    }
}
```

This responds with a status code of 400 Bad Request and this response body:
```cs
{
  "type": "/docs/not_on_monday",
  "title": "Sorry we're shut on Mondays.",
  "details:""We want developers to have a happy Monday :)",
  "status": 400,
  "instance": "httpa://localhost/TestProblemDetails",
  "errors": {
    "email": [
      "Email address is invalid"
    ]
  }
}
```
> Note the status code returned is 400 because that what we set using the `ProblemDetailsAttribute`.


#### ProblemDetailsResponse

Why do we wrap ProblemDetails in a ProblemDetailsResponse rather than directly returning ASP.NET Core's ProblemDetails? Two reasons:

1. For unhandled exception (see ProblemDetailsExceptionHandler) thrown during development we want to be able to return exception details.

2. We want extensions (see ProblemDetailsResponse Extensions) to serialize using the same JsonSerializer you configured in Startup.cs, currently because of this [bug](https://github.com/JamesNK/Newtonsoft.Json/issues/1998) they don't.

#### ProblemDetailsResponse Extensions

[Section 3.2 of the Problem Details RFC](https://tools.ietf.org/html/rfc7807#section-3.2) says that "Problem type definitions MAY extend the problem details object with additional members."

Both the `ProblemDetailsActionResultFactory Problem` and `ProblemDetailsFactory ResponseFor` methods allow you to supply a Dictionary<stirng, object> representing the extension to be added the registered problem type.

Extensions generally contain contextual information so you need to pass your Dictionary<stirng, object> at runtime. Below shows how you would do this in a controller's action to return the example shown in the [Problem Detail RFC](https://tools.ietf.org/html/rfc7807#section-4.1).


```cs
[ApiController, Route("[Controller]")]
public class TestProblemDetailsController : ControllerBase
.   
    private readonly ProblemDetailsActionResultFactory _problemDetailsActionResultFactory;

    public TestProblemDetailsControllerProblemDetailsActionResultFactory problemDetailsActionResultFactory)
    {
        _problemDetailsActionResultFactory = problemDetailsActionResultFactory;
    }
        
    [HttpGet]
    public ActionResult Get()
    {
        var extensions = new Dictionary<string, object> 
        {
            { "Balance", 30 },
            { "Accounts", new[] {"/account/12345", "/account/67890" } }
        };
        
       return _problemDetailsActionResultFactory.Problem("NotOnMonday", extensions);
    }
}
```

This responds with a status code of 400 Bad Request and this response body:
```cs
{
  "type": "/docs/not_on_monday",
  "title": "Sorry we're shut on Mondays.",
  "details:""We want developers to have a happy Monday :)",
  "status": 400,
  "instance": "https://localhost/TestProblemDetails",
  "balance": 30,
  "accounts": [
    "/account/12345",
    "/account/67890"
  ]
}
```

> **WARNING: Use extreme caution when extending a problem type response, in particular responding with user suppiled data and exposing implementation internals in error messages. For more information see [section 5 of the Problem Details RFC](https://tools.ietf.org/html/rfc7807#section-5).**

#### Default Problem Types

When registering the ProblemDetailFactory by calling `AddProblemDetailFactory` in Startup.cs two problem types are registered by default.

`DefaultProblemTypes.ValidationFailed` Should be used to tell the client using your API that their request failed validation. This will happpen automatically if you call `ConfigureInvalidModelStateResponseFactory` in Startup.cs.

This responds with a status code of 400 Bad Request and this response body:

```javascript
{
  "type": "/docs/validation_failed",
  "title": "Your request parameters didn't validate.",
  "status": 400,
  "instance": "https://localhost/TestProblemDetails"
}
```

`DefaultProblemTypes.InternalServerError` problem type should be used to tell the client using your API that their request failed with an unexpected error. This will happpen automatically if you configure `ProblemDetailsExceptionHandler` in Startup.cs.

This responds with a status code of 500 internal server error and this response body:

```javascript
{
  "type": "/docs/internal_server_error",
  "title": "Something unexpected happened, please try again.",
  "status": 500,
  "instance": "https://localhost/TestProblemDetails"
}
```

> There is no reason not to use DefaultProblemTypes as you can change any of the problem details by calling `AddOrUpdateProblemDetails` in Startup.cs with your changes. By doing this you keep the benefits of automatically returning these types.

#### ProblemDetailsFactory

ProblemDetailsFactory has lower level methods for working with your registered problem types. For convenience some methods can be called statically.

The following methods are static:

`Contains` checks if a problem type is registered it takes a string representing the type and returns bool if it found.

`ResponseFor` creates a `ProblemDetailsResponse` from a registered problem type using the enum constant name supplied. Like `Problem` it has overloads that take ModelState, Extensions and you can look up a regsistered problem type using a string rather than enum. 

There are generally two reasons to use `ResponseFor` instead of returning a ViewResult using `Problem` 

1. You want to get a `ProblemDetailsResponse` outside of a controller.

2. You want to exceptionally change the details of the `ProblemDetailsResponse` - for example returning a different status code.

Below is an example of using `ResponseFor`.
```cs
[HttpGet, Route("TestProblemDetails")]
public ActionResult TestProblemDetails()
{
    // Change the status code for ValidationFailed just for this controllers action
    
    // Get new instance of a ProblemDetailsResponse
    var validationFailedProblemType = ProblemDetailsFactory.ResponseFor(DefaultProblemTypes.ValidationFailed);
    
    validationFailedProblemType.Status = HttpStatusCode.NotAcceptable;

    return new ObjectResult(validationFailedProblemType)
    {
        StatusCode = validationFailedProblemType.Status.Value.ParseStatus()
    };
}
```

`ExceptionFor` creates a `ProblemDetailsException` from a registered problem type using the enum constant name supplied. Like `ResponseFor` it has overloads that take ModelState, Extensions, and you can look up a regsistered problem type using a string rather than enum. Designed to work with the `ProblemDetailsExceptionHandler` see ProblemDetailsExceptionHandler for more details.

The following method requires an injected instance: 

`AddOrUpdateProblemDetails`allows you to register or update one or many ProblemDetails, by passing either an enumerable of ProblemDetails or by passing an enum decorated with ProblemDetailsAttributes. 

Registering or updating ProblemDetails should be thread safe as it uses a concurrent Dictionary. Provding a problem type that is already registered will perform an update. 

Below is an example of registering and updating registered Problem types.
```cs
 public class MyProblemDetailsUpdateService
    {
        private readonly ProblemDetailsFactory _problemDetailsFactory;

        public MyProblemDetailsUpdateService(ProblemDetailsFactory problemDetailsFactory)
        {
            _problemDetailsFactory = problemDetailsFactory;
        }

        public void PopulateMyProblemDetails()
        {
            // ... you could get the problem details from a DB or whatever

            // Register a list of problem types 
            var problemTypes1 = new List<ProblemDetails>
            {
                new ProblemDetails
                {
                    Type = "NotOnMonday",
                    Title = "Sorry we're shut on Mondays.",
                    Detail = "We want developers to have a happy Monday :)",
                    Status = HttpStatusCode.BadRequest.ParseStatus()
                }
            };

            _problemDetailsFactory.AddOrUpdateProblemDetails(problemTypes1);

            // Register problem Types using an enum
            _problemDetailsFactory.AddOrUpdateProblemDetails<DeveloperProblemTypes>();

            // Update the title of a previous registered problem type 
            var problemTypes2 = new List<ProblemDetails>
            {
                new ProblemDetails
                {
                    Type = "NotOnMonday",
                    Title = "We are closed Mondays.",
                }
            };

            _problemDetailsFactory.AddOrUpdateProblemDetails(problemTypes2);
        }
    }
```

> **WARNING: Use extreme caution when allowing end users to get or update problem types - in particular protect against [overposting](https://andrewlock.net/preventing-mass-assignment-or-over-posting-in-asp-net-core/), and ensure only Authorized users have access.**

### ProblemDetailsExceptionHandler

> ProblemDetailsExceptionHandler is biased to API's returning JSON, at the moment the ProblemDetailsExceptionHandler will return JSON only.

The ProblemDetailsExceptionHandler allows you to centralize API exceptions and handle exceptions at a macro level. It avoids bolierplate try catches in your services and controllers, just throw a ProblemDetailsException using a registered problem type.

To use the ProblemDetailsExceptionHandler just register your problem types and call UseProblemDetailExceptionHandler in Startup.cs.

```cs
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddProblemDetailFactory(new Uri("/docs", UriKind.Relative))
        .AddOrUpdateProblemDetails<DeveloperProblemTypes>()
        .ConfigureInvalidModelStateResponseFactory();
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
{
    app.UseProblemDetailExceptionHandler(env);
}
```

Now you can lookup and throw a registered problem type in any controller or service.

```cs
throw ProblemDetailsFactory.ExceptionFor(DeveloperProblemTypes.NotOnMonday);
```

This would repsond to a client using your API with a 400 Bad Request and this response body:
```javascript
{
  "type": "/docs/not_on_monday",
  "title": "Sorry we're shut on Mondays.",
  "details:""We want developers to have a happy Monday :)",
  "status": 400,
  "instance": "https://localhost/TestProblemDetails"
}
```

Any unhandled exceptions will return the `DefaultProblemTypes.InternalServerError` problem type.
```cs
throw new InvalidOperationException("I'm an unhandled exception");
```

This would repsond to a client using your API with a 500 Internal Server error and this response body:
```javascript
{
  "type": "/docs/internal_server_error",
  "title": "Something unexpected happened, please try again.",
  "status": 500,
  "instance": "https://localhost/TestProblemDetails"
}
```

> Where possible try to avoid throwing exceptions for known errors

When debugging it's useful to see the exception details of the exception that is thrown. If `ASPNETCORE_ENVIRONMENT `is set to `Development` exception details are added to `DefaultProblemTypes.InternalServerError` problem type.

```javascript
{
  "type": "/docs/internal_server_error",
  "title": "Something unexpected happened, please try again.",
  "status": 500,
  "instance": "https://localhost/TestProblemDetails",
  "debug": {
    "ClassName": "System.InvalidOperationException",
    "Message": "I'm an unhandled exception",
    "Data": null,
    "InnerException": null,
    "HelpURL": null,
    "StackTraceString": "   at ...",
    "RemoteStackTraceString": null,
    "RemoteStackIndex": 0,
    "ExceptionMethod": null,
    "HResult": -2146233079,
    "Source": "...",
    "WatsonBuckets": null
  }
}
```

> **Important ensure `ASPNETCORE_ENVIRONMENT `is NOT set to `Development` in production as you will be exposing implementation internals in the problem type response. In future versions you will be able to completely turn this feature off.**

## ConcurrentConfigurationProvider

> **WARNING: Use extreme caution when allowing end users to get or update configuration - in particular protect against [overposting](https://andrewlock.net/preventing-mass-assignment-or-over-posting-in-asp-net-core/), and ensure only Authorized users have access.**

ASP.NET Core comes with great support for where you get you configuration from:
* Azure Key Vault
* Command-line parameters
* Environment variables
* Files (INI, JSON, XML)
* Memory Configuration Provider	In-memory collections
* User secrets

However there is often a situation where you want to change config at runtime or load config from somewhere that isn't supported SQL Server, MongoDB etc.

The ConcurrentConfigurationProvider makes this possible by loading config from a [ConcurrentDictionary](https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2?view=netframework-4.7.2). You just populate the Dictionary via the AddOrUpdateConfiguration methods. You can populate the Dictionary from a database etc, you can update at runtime and get the updated config using  [IOptionsSnapshot](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.options.ioptionssnapshot-1?view=aspnetcore-2.2).

### Setup

In Startup.cs

```cs
private readonly IConfigurationRoot _configuration;
private readonly ConcurrentConfiguration<TestConfig> _concurrentConfiguration = new ConcurrentConfiguration<TestAppSettings>();

// 1. Add Configuration Source
public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
{
    var builder = new ConfigurationBuilder()
        // ...
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        //... Whatever other configuration sources you  want to use
        // Add concurrent configuration source
        .AddConcurrentConfiguration(_concurrentConfiguration);
        
    _configuration = builder.Build();
}

// 2. Add Concurrent Configuration and ASP.NET Options to DI
public void ConfigureServices(IServiceCollection services)
{
     services.AddConcurrentConfiguration(_concurrentConfiguration); // Add Concurrent cCnfiguration
    
     services.AddOptions(); // Enable options
     services.Configure<TestConfig>(_configuration.GetSection(nameof(TestAppSettings))); // Add configure Options
}

// 3. Populate Concurrent Configuration
public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
{
    // You could use _concurrentConfiguration too :)
    var concurrentConfiguration = app.ApplicationServices.GetService<ConcurrentConfiguration<TestAppSettings>>();  
    
    // Get the config you want populate from database etc ...
    
    // Populate config
    concurrentConfiguration.AddOrUpdateConfiguration(testConfig => testConfig.WebHostUrl, "https://localhost");
    
    // ...
}
```

### Usage

Now you have configured ConcurrentConfiguration in Startup.cs you can inject Options to your controller normally, these Options don't reflect any updates.
```cs
[ApiController, Route("[Controller]")]
public class ConfigController : Controller
{
    private readonly IOptions<TestAppSettings> _testConfig;

    public ConfigController(IOptions<TestAppSettings> testConfig)
    {
        _testConfig = testConfig;
    }
    
    // Action to get updated config
    [HttpGet, Route("TestConfig")]
    public ActionResult TestConfig()
    {
       return new ObjectResult(_testConfig?.Value);
    }
}
```

To get updated config inject [IOptionsSnapshot](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.options.ioptionssnapshot-1?view=aspnetcore-2.2). You can also inject ConcurrentConfiguration and update the config at runtime.

Putting it together you get:
```cs
[ApiController, Route("[Controller]")]
public class ConfigController : Controller
{
    private readonly ConcurrentConfiguration<TestAppSettings> _concurrentConfiguration;
    private readonly IOptionsSnapshot<TestAppSettings> _testConfig;

    public ConfigController(ConcurrentConfiguration<TestAppSettings> concurrentConfiguration, IOptionsSnapshot<TestAppSettings> testConfig)
    {
        _concurrentConfiguration = concurrentConfiguration;
        _testConfig = testConfig;
    }
    
    // Action to update the config at runtime using ConcurrentConfiguration
    [HttpPut, Route("TestConfig")]
    public ActionResult SetTestConfig()
    {
        _concurrentConfiguration.AddOrUpdateConfiguration(config => config.WebHostUrl, "https://dalsoft.co.uk"); // Update concurrent dictionary 
        
        // Update database etc ....
        
        return Ok();
    }
    
    // Action to get updated config using IOptionsSnapshot
    [HttpGet, Route("TestConfig")]
    public ActionResult TestConfig()
    {
       return new ObjectResult(_testConfig?.Value);
    }
}
```

### Summary

Setup ConcurrentConfiguration in Startup.cs, inject `ConcurrentConfiguration` and update config by calling `AddOrUpdateConfiguration`, finally you can get your updated config using `IOptionsSnapshot`.

> **WARNING: Use extreme caution when allowing end users to get or update configuration - in particular protect against [overposting](https://andrewlock.net/preventing-mass-assignment-or-over-posting-in-asp-net-core/), and ensure only Authorized users have access.**

## AppSettings

This is a *very simple* abstract class that lets you bind the inheriting class to appsetting.json using a static method. It's useful for test projects where you don't have DI setup but want an easy way to get settings from appsettings.json.

> **For your actual WebApi project you should use the [Options pattern](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-2.2) where possible.**

### Usage

Create a class inheriting from AppSettings<T> where T is your class.

```cs
public class TestAppSettings : AppSettings<TestAppSettings>
{
    public string WebHostUrl { get; set; }      
}
```

Add your appsettings.json
```js
// appsetting.json
{
  "ConnectionStrings": {
    "DbContext": "Server=(localdb)..."
  },
  "TestAppSettings": {
    "WebHostUrl": "https://localhost"
  }
}
```

Then bind your settings from appsettings.json by calling GetSettings().

```cs
TestAppSettings
  .GetSettings().WebHostUrl; // Returns "https://localhost"
```

> After the first call to GetSettings(), your settings are cached. GetSettings() supports overriding settings using [appsettings.{environmentName}.json convention](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.2#file-configuration-provider).



