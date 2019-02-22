using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DalSoft.AspNetCore.WebApi.ErrorHandling
{
    public class ProblemDetailsFactory
    {
        private static readonly ConcurrentDictionary<string, ProblemDetails> ProblemDetails = new ConcurrentDictionary<string, ProblemDetails>();
        
        internal Uri BaseUri { get; }
        
        internal ProblemDetailsFactory(Uri baseUri)
        {
            BaseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
        }

        //TODO Func on first iteration overload

        public void AddOrUpdateProblemDetails(IEnumerable<ProblemDetails> problemDetails)
        {
            foreach (var problemDetail in problemDetails)
            {
                ValidateProblemDetail(problemDetail);
                problemDetail.Type = FormatType(problemDetail.Type);
                ProblemDetails.AddOrUpdate(problemDetail.Type, problemDetail, (key, oldValue) => problemDetail);
            }
        }

        public void AddOrUpdateProblemDetails<T>() where T : Enum
        {
            foreach (Enum problemDetailEnum in Enum.GetValues(typeof(T)))
            {
                var (title, detail, status, instance) = GetProblemDetailFromAttribute(problemDetailEnum);
                
                var problemDetail = new ProblemDetails
                {
                    Type = FormatType(problemDetailEnum.ToString()),
                    Title = title,
                    Detail = detail,
                    Status = status.ParseStatus(),
                    Instance = instance
                };

                ValidateProblemDetail(problemDetail);

                ProblemDetails.AddOrUpdate(problemDetailEnum.ToString(), problemDetail, (key, oldValue) => problemDetail);
            }
        }

        public void RemoveProblemDetail(string type)
        {
            ProblemDetails.TryRemove(type, out var problemDetail);
        }

        public static bool Contains(string type)
        {
            return ProblemDetails.ContainsKey(type);
        }
        
        public static ProblemDetailsException ExceptionFor<T>(T type) where T : Enum
        {
            return ExceptionFor(type.ToString());
        }

        public static ProblemDetailsException ExceptionFor<T>(T type,  IDictionary<string, object> extensions) where T : Enum
        {
            return ExceptionFor(type.ToString(), extensions);
        }
        
        public static ProblemDetailsException ExceptionFor<T>(T type, ModelStateDictionary modelStateDictionary) where T : Enum
        {
            return ExceptionFor(type.ToString(), modelStateDictionary);
        }

        public static ProblemDetailsException ExceptionFor<T>(T type, ModelStateDictionary modelStateDictionary, IDictionary<string, object> extensions) where T : Enum
        {
            return ExceptionFor(type.ToString(), modelStateDictionary, extensions);
        }

        public static ProblemDetailsException ExceptionFor(string type)
        {
            return new ProblemDetailsException(type);
        }
        
        public static ProblemDetailsException ExceptionFor(string type, IDictionary<string, object> extensions)
        {
            return new ProblemDetailsException(type, extensions);
        }

        public static ProblemDetailsException ExceptionFor(string type, ModelStateDictionary modelStateDictionary)
        {
            return new ProblemDetailsException(type, modelStateDictionary);
        }

        public static ProblemDetailsException ExceptionFor(string type, ModelStateDictionary modelStateDictionary, IDictionary<string, object> extensions)
        {
            return new ProblemDetailsException(type, modelStateDictionary, extensions);
        }

        public static ProblemDetailsResponse ResponseFor<T>(T type)
        {
            return ResponseFor(type.ToString());
        }

        public static ProblemDetailsResponse ResponseFor<T>(T type, ModelStateDictionary modelState)
        {
            return ResponseFor(type.ToString(), modelState);
        }

        public static ProblemDetailsResponse ResponseFor<T>(T type, IDictionary<string, object> extensions)
        {
            return ResponseFor(type.ToString(), null, extensions);
        }

        public static ProblemDetailsResponse ResponseFor<T>(T type, ModelStateDictionary modelState, IDictionary<string, object> extensions)
        {
            return ResponseFor(type.ToString(), modelState, extensions);
        }

        public static ProblemDetailsResponse ResponseFor(string type)
        {
            return ResponseFor(type, null, null);
        }

        public static ProblemDetailsResponse ResponseFor(string type, ModelStateDictionary modelState)
        {
            return ResponseFor(type, modelState, null);
        }

        public static ProblemDetailsResponse ResponseFor(string type, IDictionary<string, object> extensions)
        {
            return ResponseFor(type, null, extensions);
        }
        
        public static ProblemDetailsResponse ResponseFor(string type, ModelStateDictionary modelState, IDictionary<string, object> extensions)
        {
            var problemDetailsItem = ProblemDetails.SingleOrDefault(_ => _.Key == type);
           
            if (problemDetailsItem.Equals(default(KeyValuePair<string,ProblemDetails>)))
                throw new KeyNotFoundException($"Problem Detail {type} not found.");

            var problemDetails = new ProblemDetails
            {
                Status = problemDetailsItem.Value.Status,
                Type = problemDetailsItem.Value.Type,
                Title = problemDetailsItem.Value.Title,
                Detail = problemDetailsItem.Value.Detail,
                Instance = problemDetailsItem.Value.Instance
            };
            
            foreach (var item in extensions ?? new Dictionary<string, object>())
                problemDetails.Extensions.Add(item);
            
            return new ProblemDetailsResponse(problemDetails, modelState);
        }

        private static (string title, string detail, HttpStatusCode status, string instance) GetProblemDetailFromAttribute(Enum @enum)
        {
            var problemDetailAttributes = (ProblemDetailsAttribute[]) (@enum.GetType().GetField(@enum.ToString())
                .GetCustomAttributes(typeof(ProblemDetailsAttribute), false));

            if (problemDetailAttributes.Length == 0)
                throw new InvalidOperationException($"{nameof(ProblemDetailsAttribute)} is missing.");

            if (problemDetailAttributes.Length > 1)
                throw new InvalidOperationException($"More than one {nameof(ProblemDetailsAttribute)} specified.");

            var problemDetailAttribute = problemDetailAttributes[0];

            return (problemDetailAttribute.Title, problemDetailAttribute.Detail, problemDetailAttribute.Status, problemDetailAttribute.Instance);
        }

        private string FormatType(string type)
        {
            type = string.Concat(type.Select((_, i) => i > 0 && char.IsUpper(_) ? "_" + _.ToString() : _.ToString())).ToLower(); //snake case

            return (BaseUri.IsAbsoluteUri ? new Uri(BaseUri, type) : new Uri(BaseUri + "/" + type, UriKind.Relative)).ToString();
        }

        private static void ValidateProblemDetail(ProblemDetails problemDetail)
        {
            if (string.IsNullOrEmpty(problemDetail.Type)) 
                throw new ArgumentException($"{nameof(problemDetail.Type)} cannot be empty.", nameof(problemDetail.Type));
                
            if (string.IsNullOrEmpty(problemDetail.Title)) 
                throw new ArgumentException($"{nameof(problemDetail.Title)} cannot be empty.", nameof(problemDetail.Title));
        }
    }
}