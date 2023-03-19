using System;

namespace HalApplicationBuilder.ReArchTo関数型.Runtime
{
    internal interface IInstanceConvertingContext
    {
        object CreateInstance(string typeName);
    }
}

