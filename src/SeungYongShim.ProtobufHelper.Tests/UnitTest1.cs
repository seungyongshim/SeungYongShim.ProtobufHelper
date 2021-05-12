using System;
using System.Text.Json;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace SeungYongShim.ProtobufHelper.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var knownTypes = new KnownTypes(new[] { typeof(KafkaEvent) });

            var @event = new KafkaEvent
            {
                TraceId = "abcdefg",
                Body = Any.Pack(from x in new Sample { ID = "abcdefg" }
                                select x.Body.Add(new[] { "Hello", "World" }))
            };

            var message = JsonFormatter.ToDiagnosticString(Any.Pack(@event));

            var any = JsonSerializer.Deserialize<AnyJson>(message);

            var a = any.ToAny();

            var parsedEvent = a.Unpack<KafkaEvent>();

            var parsedEvent2 = knownTypes.Unpack(a) as KafkaEvent;

            var parsedSample = parsedEvent.Body.Unpack<Sample>();

            var parsedSample2 = knownTypes.Unpack(parsedEvent2.Body);

            parsedEvent.Should().Be(parsedEvent2);

            parsedSample.Should().Be(parsedSample2);


        }
    }
}
