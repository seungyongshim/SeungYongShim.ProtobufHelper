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
        public ProtoKnownTypes() : this(assembly =>
        {
            var name = assembly.GetName().Name;
            return Regex.IsMatch(name, @"^Google.*") ||
                   Regex.IsMatch(name, @"^Grpc.*") ||
                   Regex.IsMatch(name, @"^xunit.*") ||
                   Regex.IsMatch(name, @"^Microsoft.*") ||
                   Regex.IsMatch(name, @"^System.*") ||
                   Regex.IsMatch(name, @"^OpenTelemetry.*");
        })
        { }

        public ProtoKnownTypes(Func<Assembly, bool> excludeFunc)
            : this(from assembly in AppDomain.CurrentDomain.GetAssemblies()
                   let name = assembly.GetName().Name
                   where !excludeFunc(assembly)
                   select assembly)
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
                select (Key: $"type.googleapis.com/{GetDescriptorFullName(t)}",
                        Value: typeof(Any).GetMethod("Unpack").MakeGenericMethod(t));

            var anyUnpackersDic = anyUnpackers.Select(x => (x.Key, Func: fun((Any a) => x.Value?.Invoke(a, null) as IMessage)))
                                              .ToDictionary(x => x.Key, x => x.Func);

            AnyUnpackersDic = anyUnpackersDic;
        }

        internal string GetDescriptorFullName(Type t) =>
            (t.GetProperty("Descriptor")
              .GetGetMethod()?
              .Invoke(null, null) as MessageDescriptor)
              .FullName;

        public IMessage Unpack(Any any) => AnyUnpackersDic[any.TypeUrl](any);

        internal Dictionary<string, Func<Any, IMessage>> AnyUnpackersDic { get; }
        internal TypeRegistry Registry { get; }
        public JsonParser JsonParser { get; }
        public JsonFormatter JsonFormatter { get; }
    }
}
