using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DalSoft.AspNetCore.WebApi.ErrorHandling
{
    public class ProblemDetailsActionResultFactory
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProblemDetailsActionResultFactory(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public ActionResult Problem<T>(T type)
        {
            return Problem(type.ToString());
        }

        public ActionResult Problem<T>(T type, ModelStateDictionary modelState)
        {
            return Problem(type.ToString(), modelState);
        }

        public ActionResult Problem<T>(T type, IDictionary<string, object> extensions)
        {
            return Problem(type.ToString(), null, extensions);
        }

        public ActionResult Problem<T>(T type, ModelStateDictionary modelState, IDictionary<string, object> extensions)
        {
            return Problem(type.ToString(), modelState, extensions);
        }

        public ActionResult Problem(string type)
        {
            return Problem(type, null, null);
        }

        public ActionResult Problem(string type, ModelStateDictionary modelState)
        {
            return Problem(type, modelState, null);
        }

        public ActionResult Problem(string type, IDictionary<string, object> extensions)
        {
            return Problem(type, null, extensions);
        }

        public ActionResult Problem(string type, ModelStateDictionary modelState, IDictionary<string, object> extensions)
        {
            var problemDetailResponse = ProblemDetailsFactory.ResponseFor(type, modelState, extensions);

            problemDetailResponse.SetInstance(_httpContextAccessor.HttpContext.Request);

            return new ObjectResult(problemDetailResponse)
            {
                StatusCode = problemDetailResponse.Status?.ParseStatus() ?? 400
            };
        }
    }
}
