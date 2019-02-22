using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;

namespace DalSoft.AspNetCore.WebApi.ErrorHandling
{
    [JsonConverter(typeof(ProblemDetailsConverter))]
    public class ProblemDetailsResponse
    {
        public string Type { get; set; }
        
        public string Title { get; set; }
        
        public string Detail { get; set; }
        
        public HttpStatusCode? Status { get; set; }
        
        public string Instance { get; set; }
        
        public IDictionary<string, string[]> Errors { get; set; }

        /// <summary>Debug info only set when ASPNETCORE_ENVIRONMENT is set to "Development"</summary>
        public dynamic Debug { get; set; }
        
        internal IDictionary<string, object> Extensions { get; set; }

        [JsonConstructor]
        public ProblemDetailsResponse()
        {
            
        }
        
        public ProblemDetailsResponse(ProblemDetails problemDetail, ModelStateDictionary modelStateDictionary)
        {
            // TODO: consider using ValidationProblemDetails once the Extensions issue is fixed (It was too much to port in one go anyhow).
            
            Type = problemDetail.Type;
            Title = problemDetail.Title;
            Detail = problemDetail.Detail;
            Status = problemDetail.Status == null ? default(HttpStatusCode?) : (HttpStatusCode)problemDetail.Status;
            Instance = problemDetail.Instance;
            
            modelStateDictionary = modelStateDictionary ?? new ModelStateDictionary();
            Errors = modelStateDictionary.Any() ?  new Dictionary<string, string[]>() : null;

            foreach (var modelState in modelStateDictionary)
            {
                if (modelState.Value.ValidationState == ModelValidationState.Invalid) 
                    Errors?.Add(modelState.Key,  modelState.Value.Errors.Select(modelError => modelError.ErrorMessage).ToArray());
            }
            
            Extensions = problemDetail.Extensions; //TODO use JsonExtensionData... I can't use JsonExtensionData because of this issue https://github.com/JamesNK/Newtonsoft.Json/issues/1998
        }

        internal void SetInstance(HttpRequest request)
        {
            if (string.IsNullOrWhiteSpace(Instance))
                Instance = request.GetEncodedUrl();
        }
    }

    public static class ProblemDetailResponseExtensions
    {
        public static int ParseStatus(this HttpStatusCode status)
        {
            return (int)status;
        }
    }
}
