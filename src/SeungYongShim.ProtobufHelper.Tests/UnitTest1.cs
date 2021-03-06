using System;
using System.Text.Json;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace SeungYongShim.ProtobufHelper.Tests
{
    public class KnownType
    {
        [Fact]
        public void InsertType()
        {
            var knownTypes = new ProtoKnownTypes();

            var @event = new Event
            {
                TraceId = "abcdefg",
                Body = Any.Pack(from x in new SampleDto { ID = "abcdefg" }
                                select x.Body.Add(new[] { "Hello", "World" }))
            };

            var message = JsonFormatter.ToDiagnosticString(Any.Pack(@event));
            var any = JsonSerializer.Deserialize<AnyJson>(message);
            var a = any.ToAny();

            var parsedEvent = a.Unpack<Event>();
            var parsedEvent2 = knownTypes.Unpack(a) as Event;
            var parsedSample = parsedEvent.Body.Unpack<SampleDto>();
            var parsedSample2 = knownTypes.Unpack(parsedEvent2.Body);
            parsedEvent.Should().Be(parsedEvent2);
            parsedSample.Should().Be(parsedSample2);
        }

        [Fact]
        public void NotInsertType()
        {
            var knownTypes = new ProtoKnownTypes();

            var @event = new KafkaCommand
            {
                TraceId = "abcdefg",
                Body = Any.Pack(from x in new SampleDto { ID = "abcdefg" }
                                select x.Body.Add(new[] { "Hello", "World" }))
            };

            var message = JsonFormatter.ToDiagnosticString(Any.Pack(@event));
            var any = JsonSerializer.Deserialize<AnyJson>(message);
            var a = any.ToAny();

            var parsedEvent = a.Unpack<KafkaCommand>();
            var parsedEvent2 = knownTypes.Unpack(a) as KafkaCommand;
            var parsedSample = parsedEvent.Body.Unpack<SampleDto>();
            var parsedSample2 = knownTypes.Unpack(parsedEvent2.Body);
            parsedEvent.Should().Be(parsedEvent2);
            parsedSample.Should().Be(parsedSample2);
        }
    }
}
