using System;
using System.Threading.Tasks;

namespace GlassLCU.API
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    internal sealed class ParameterAttribute : Attribute
    {
        public enum Position
        {
            Query,
            Path,
            Header,
            Body
        }

        public string Name { get; }
        public Position InPosition { get; }

        public ParameterAttribute(string name, string inPosition)
        {
            this.Name = name;
            this.InPosition = (Position)Enum.Parse(typeof(Position), inPosition, true);
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class EndpointAttribute : Attribute
    {
        public string URL { get; }

        public EndpointAttribute(string url)
        {
            this.URL = url;
        }
    }

    public interface ISender
    {
        Task<T> Request<T>(string method, string path, object body = null);
        Task Request(string method, string path, object body = null);
    }

    internal static class GenerationUtils
    {
        public static ISender Sender { get; set; }
    }
}
