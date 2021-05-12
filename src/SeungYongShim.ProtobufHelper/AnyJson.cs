using System.Text.Json.Serialization;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace SeungYongShim.ProtobufHelper
{
    public class AnyJson
    {
        [JsonPropertyName("@type")]
        public string Type { get; set; }

        [JsonPropertyName("@value")]
        public string Value { get; set; }

        public Any ToAny() => new Any
        {
            TypeUrl = Type,
            Value = ByteString.FromBase64(Value)
        };
    }
}
