using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace DalSoft.AspNetCore.WebApi.ErrorHandling
{
    public static class ServiceCollectionExtensions
    {
        public static ProblemDetailFactoryConfiguration AddProblemDetailFactory(this IServiceCollection services, Uri baseUri)
        {
            var problemDetailFactory = new ProblemDetailsFactory(baseUri);
            
            problemDetailFactory.AddOrUpdateProblemDetails<DefaultProblemTypes>();

            services.AddHttpContextAccessor();
            services.AddSingleton(problemDetailFactory);
            services.AddSingleton<ProblemDetailsActionResultFactory>();

            return new ProblemDetailFactoryConfiguration(services, problemDetailFactory);
        }

        public class ProblemDetailFactoryConfiguration
        {
            private readonly IServiceCollection _services;
            private readonly ProblemDetailsFactory _problemDetailFactory;

            public ProblemDetailFactoryConfiguration(IServiceCollection services, ProblemDetailsFactory problemDetailFactory)
            {
                _services = services;
                _problemDetailFactory = problemDetailFactory;
            }

            public ProblemDetailFactoryConfiguration AddOrUpdateProblemDetails(IEnumerable<ProblemDetails> problemDetails)
            {
                _problemDetailFactory.AddOrUpdateProblemDetails(problemDetails);

                return this;
            }

            public ProblemDetailFactoryConfiguration AddOrUpdateProblemDetails<T>() where T : Enum
            {
                _problemDetailFactory.AddOrUpdateProblemDetails<T>();

                return this;
            }

            public ProblemDetailFactoryConfiguration ConfigureInvalidModelStateResponseFactory()
            {
                _services.PostConfigure<ApiBehaviorOptions>(options =>
                {
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var problemDetailsActionResultFactory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsActionResultFactory>();

                        var extensions = new Dictionary<string, object> // new { Balance = 30, Accounts =  new[] {"/account/12345", "/account/67890" } })
                        {
                            { "Balance", 30 },
                            { "Accounts", new[] {"/account/12345", "/account/67890" } }
                        };

                        return problemDetailsActionResultFactory.Problem(DefaultProblemTypes.ValidationFailed, context.ModelState, extensions);
                    };
                });

                return this;
            }

        }
    }
}
