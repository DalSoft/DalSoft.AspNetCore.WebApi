using System;
using System.Net;

namespace DalSoft.AspNetCore.WebApi.ErrorHandling
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ProblemDetailsAttribute : Attribute 
    {
        public ProblemDetailsAttribute(string title, HttpStatusCode status) : this(title, status, null, null) { }

        public ProblemDetailsAttribute(string title, HttpStatusCode status, string detail) : this(title, status, detail, null) { }
        
        public ProblemDetailsAttribute(string title, HttpStatusCode status, string detail, string instance)
        {
            Title = title;
            Detail = detail;
            Status = status;
            Instance = instance;
        }
        
        public string Title { get; }
        
        public string Detail { get; }
        
        public HttpStatusCode Status { get; }
        
        public string Instance { get; }
    }
}