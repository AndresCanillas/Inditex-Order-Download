using System;

namespace WebLink.Contracts.Models.Print
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NoTrimAttribute : Attribute
    {
    }

}
