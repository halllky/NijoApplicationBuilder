using System;

namespace HalApplicationBuilder.Runtime
{
    internal interface IInstanceConvertingContext
    {
        object CreateInstance(string typeName);
    }
}

