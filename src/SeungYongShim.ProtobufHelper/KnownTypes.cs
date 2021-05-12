using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using static SeungYongShim.ProtobufHelper.FuntionalHelper;
using Any = Google.Protobuf.WellKnownTypes.Any;

namespace SeungYongShim.ProtobufHelper
{
    public class KnownTypes
    {
        public KnownTypes(IEnumerable<Type> types)
        {
            var messageTypes =
                (from t in types
                 let assembly = Assembly.GetAssembly(t)
                 from type in assembly.GetTypes()
                 where typeof(IMessage).IsAssignableFrom(type)
                 where type.IsInterface is false
                 select type).ToList();

            var descriptorAll =
                from type in messageTypes
                select type.GetProperty("Descriptor")
                           .GetGetMethod()?
                           .Invoke(null, null) as MessageDescriptor;

            Registry = TypeRegistry.FromMessages(descriptorAll);
            JsonParser = new JsonParser(JsonParser.Settings.Default.WithTypeRegistry(Registry));
            JsonFormatter = new JsonFormatter(JsonFormatter.Settings.Default.WithTypeRegistry(Registry));

            var anyUnpackers =
                from t in messageTypes
                select (Key: $"type.googleapis.com/{t.Name}", Value: typeof(Any).GetMethod("Unpack")
                                                                                .MakeGenericMethod(t));

            var anyUnpackersDic = anyUnpackers.Select(x => (x.Key, Func: fun((Any a) => x.Value?.Invoke(a, null) as IMessage)))
                                              .ToDictionary(x => x.Key, x => x.Func);

            AnyUnpackersDic = anyUnpackersDic;
        }

        public IMessage Unpack(Any any) => AnyUnpackersDic[any.TypeUrl](any);

        public Dictionary<string, Func<Any, IMessage>> AnyUnpackersDic { get; }
        public TypeRegistry Registry { get; private set; }
        public JsonParser JsonParser { get; }
        public JsonFormatter JsonFormatter { get; }
    }
}
