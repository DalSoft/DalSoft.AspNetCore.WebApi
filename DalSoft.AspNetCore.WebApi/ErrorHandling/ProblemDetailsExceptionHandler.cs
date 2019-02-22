using System;
using System.Threading.Tasks;
using DalSoft.AspNetCore.WebApi.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DalSoft.AspNetCore.WebApi.ErrorHandling
{
    public static class ProblemDetailsExceptionHandler
    {
        private static Action<HttpContext, Exception> _unhandledException = (context, exception) => { };
        private static JsonSerializerSettings _serializerSettings;
        
        public static void UseProblemDetailExceptionHandler(this IApplicationBuilder app, IHostingEnvironment env)
        {
            UseProblemDetailExceptionHandler(app, env, (context, exception) => { });
        }
        
        public static void UseProblemDetailExceptionHandler(this IApplicationBuilder app, IHostingEnvironment env,  Action<HttpContext, Exception> unhandledException)
        {
            if (!ProblemDetailsFactory.Contains(DefaultProblemTypes.InternalServerError.ToString()))
                throw new InvalidOperationException("To use ProblemDetailExceptionHandler you need to call AddProblemDetailFactory in your Startup.");
            
            _serializerSettings = app.ApplicationServices.GetRequiredService<IOptions<MvcJsonOptions>>().Value.SerializerSettings;
            
            _unhandledException = unhandledException;
            
            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = context => Invoke(context, env)
            });
        }
        
        public static async Task Invoke(HttpContext context, IHostingEnvironment hostingEnvironment)
        {
            _unhandledException = _unhandledException ?? ((cxt, ex) => { });
            var errorHandler = context.Features.Get<IExceptionHandlerFeature>();

            if (errorHandler != null)
            {
                //Is it an exception that can be handled by the api calling client?
                if (errorHandler.Error is ProblemDetailsException problemDetailResponseException)
                {
                    var problemDetailResponse = problemDetailResponseException.ProblemDetailsResponse;
                    
                    problemDetailResponse.SetInstance(context.Request);
 
                    await WriteError(context, problemDetailResponse);

                    return; //We have an exception so lets end it there
                }

                _unhandledException(context, errorHandler.Error);

                var internalServerProblemDetailResponse = ProblemDetailsFactory.ResponseFor(DefaultProblemTypes.InternalServerError);

                internalServerProblemDetailResponse.SetInstance(context.Request);

                if (hostingEnvironment.IsDevelopment())
                    internalServerProblemDetailResponse.Debug = errorHandler.Error; //Output the exception only when ASPNETCORE_ENVIRONMENT is set to "Development"
                
                await WriteError(context, internalServerProblemDetailResponse);
            }
        }
        
        private static async Task WriteError(HttpContext context, ProblemDetailsResponse problemDetailsResponse)
        {
            context.Response.StatusCode =  problemDetailsResponse.Status?.ParseStatus() ?? 500;

            if (problemDetailsResponse.TrySerialize(_serializerSettings, out var result)) //TODO get JSON options from IoC rather than custom 
            {
                context.Response.ContentType = "application/json"; //TODO: at the moment only application/Json is supported. context.Request.Headers["Accept"].FirstOrDefault() ?? Serialization.ApplicationJson.MediaType.Value;
                await context.Response.WriteAsync(result).ConfigureAwait(false); 
            }
        }
    }
}