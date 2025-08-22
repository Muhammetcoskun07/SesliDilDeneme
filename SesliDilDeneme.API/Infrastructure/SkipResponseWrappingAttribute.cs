using System;

namespace SesliDilDeneme.API.Infrastructure
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class SkipResponseWrappingAttribute : Attribute { }
}
