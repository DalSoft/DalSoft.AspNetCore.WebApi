using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DalSoft.AspNetCore.WebApi.ErrorHandling
{
    public class ProblemDetailsException : Exception
    {
        public ProblemDetailsResponse ProblemDetailsResponse { get; set; }
       
        public ProblemDetailsException(string type) : this(type, null, null, null) { }
        
        public ProblemDetailsException(string type, Exception innerException) : this(type, null, null, innerException) { }
        
        public ProblemDetailsException(string type, IDictionary<string, object> extensions, Exception innerException) : this(type, null, extensions, innerException){ }
        
        public ProblemDetailsException(string type, IDictionary<string, object> extensions) : this(type, null, extensions, null){ }
        
        public ProblemDetailsException(string type, ModelStateDictionary modelStateDictionary, IDictionary<string, object> extensions) : this(type, modelStateDictionary, extensions, null){ }
        
        public ProblemDetailsException(string type, ModelStateDictionary modelStateDictionary) : this(type, modelStateDictionary, null, null){ }
        
        public ProblemDetailsException(string type, ModelStateDictionary modelStateDictionary, IDictionary<string, object> extensions, Exception innerException) : base(ProblemDetailsFactory.ResponseFor(type).Title, innerException)
        {
            ProblemDetailsResponse = ProblemDetailsFactory.ResponseFor(type, modelStateDictionary, extensions);
        }
    }

    public class ProblemDetailsException<T> : ProblemDetailsException where T : Enum  
    {
        public ProblemDetailsException(T type) : base(type.ToString(), null, null, null) { }
        
        public ProblemDetailsException(T type, Exception innerException) : base(type.ToString(), null, null, innerException) { }
        
        public ProblemDetailsException(T type, IDictionary<string, object> extensions, Exception innerException) : base(type.ToString(), null, extensions, innerException){ }
        
        public ProblemDetailsException(T type, IDictionary<string, object> extensions) : base(type.ToString(), null, extensions, null){ }
        
        public ProblemDetailsException(T type, ModelStateDictionary modelStateDictionary) : base(type.ToString(), modelStateDictionary, null, null){ }

        public ProblemDetailsException(T type, ModelStateDictionary modelStateDictionary, IDictionary<string, object> extensions) : base(type.ToString(), modelStateDictionary, extensions, null){ }

        public ProblemDetailsException(T type, ModelStateDictionary modelStateDictionary, IDictionary<string, object> extensions, Exception innerException) : base(type.ToString(), modelStateDictionary, extensions, innerException)  { }
    }
}