using System.Net;

namespace DalSoft.AspNetCore.WebApi.ErrorHandling
{
    public enum DefaultProblemTypes
    {
        [ProblemDetails(title:"Something unexpected happened, please try again.", status:HttpStatusCode.InternalServerError)]
        InternalServerError,

        [ProblemDetails(title:"Your request parameters didn't validate.", status:HttpStatusCode.BadRequest)]
        ValidationFailed
    }
}