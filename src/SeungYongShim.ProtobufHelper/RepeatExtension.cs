using System;
using Google.Protobuf;

namespace SeungYongShim.ProtobufHelper
{
    public static class RepeatExtension
    {
        public static T Select<T>(this T t, Action<T> func) where T : IMessage
        {
            func(t);
            return t;
        }
    }
}
