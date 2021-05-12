using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using static SeungYongShim.ProtobufHelper.FuntionalHelper;
using Any = Google.Protobuf.WellKnownTypes.Any;

namespace SeungYongShim.ProtobufHelper
{
    public class ProtoKnownTypes
    {
        public ProtoKnownTypes()
            : this(from assembly in AppDomain.CurrentDomain.GetAssemblies()
                   let name = assembly.GetName().Name
                   where !Regex.IsMatch(name, @"^Google.*")
                   where !Regex.IsMatch(name, @"^Grpc.*")
                   where !Regex.IsMatch(name, @"^xunit.*")
                   where !Regex.IsMatch(name, @"^Microsoft.*")
                   where !Regex.IsMatch(name, @"^System.*")
                   where !Regex.IsMatch(name, @"^OpenTelemetry.*")
                   select assembly)
        {

        }


        public ProtoKnownTypes(IEnumerable<Type> types)
            : this(from t in types
                   select Assembly.GetAssembly(t))
        {

        }

        public ProtoKnownTypes(IEnumerable<Assembly> assemblies)
        {
            var messageTypes = (
                from assembly in assemblies
                from type in assembly.GetTypes()
                where typeof(IMessage).IsAssignableFrom(type)
                where type.IsInterface is false
                select type).Distinct().ToList();

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

        internal Dictionary<string, Func<Any, IMessage>> AnyUnpackersDic { get; }
        internal TypeRegistry Registry { get; }
        public JsonParser JsonParser { get; }
        public JsonFormatter JsonFormatter { get; }
    }
}
