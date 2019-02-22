using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DalSoft.AspNetCore.WebApi.ErrorHandling
{
    internal class ProblemDetailsConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var detailResponse = (ProblemDetailsResponse) value;

            var problemDetailResponse = JObject.FromObject(new
            {
                detailResponse.Type,
                detailResponse.Title,
                detailResponse.Detail,
                detailResponse.Status,
                detailResponse.Instance,
                detailResponse.Errors,
                detailResponse.Debug
            }, serializer);

            if (detailResponse.Extensions != null)
                problemDetailResponse.Merge(JObject.FromObject(detailResponse.Extensions, serializer));
            
            problemDetailResponse.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ProblemDetailsResponse);
        }

        public override bool CanRead => false;
    }
}