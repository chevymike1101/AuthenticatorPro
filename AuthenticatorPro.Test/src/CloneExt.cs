using Newtonsoft.Json;

namespace AuthenticatorPro.Test
{
    public static class CloneExt
    {
        public static T Clone<T>(this T obj)
        {
            if (obj == null)
            {
                return default;
            }

            var json = JsonConvert.SerializeObject(obj);
            return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace
            });
        }
    }
}