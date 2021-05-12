using System;

namespace SeungYongShim.ProtobufHelper
{
    public static class FuntionalHelper
    {
        public static Func<T1, R> fun<T1, R>(Func<T1, R> f) => f;
    }
}
