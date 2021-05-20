using System;
using System.Collections.Generic;
using System.IO;
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

        public static IEnumerable<Assembly> GetDtoAssemblies() =>
            from filename in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*Dto*.dll", SearchOption.AllDirectories)
            select Assembly.LoadFrom(filename);

        public ProtoKnownTypes(Func<Assembly, bool> excludeFunc)
            : this(from assemblies in new[] { AppDomain.CurrentDomain.GetAssemblies(),
                                              GetDtoAssemblies() }
                   from assembly in assemblies
                   let name = assembly?.GetName().Name
                   where !excludeFunc(assembly)
                   select assembly)
        {
        }

        public ProtoKnownTypes(IEnumerable<Assembly> assemblies)
        {
            var messageTypes = (
                from assembly in assemblies.Distinct()
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

        internal static IEnumerable<Assembly> GetAllAssemblies()
        {
            var list = new List<string>();
            var stack = new Stack<Assembly>();

            foreach (var item in AppDomain.CurrentDomain.GetAssemblies())
            {
                stack.Push(item);
            }
            
            do
            {
                var asm = stack.Pop();

                yield return asm;

                foreach (var reference in asm.GetReferencedAssemblies())
                {
                    if (!list.Contains(reference.FullName))
                    {
                        stack.Push(Assembly.Load(reference));
                        list.Add(reference.FullName);
                    }
                }
            }
            while (stack.Count > 0);
        }
    }
}
