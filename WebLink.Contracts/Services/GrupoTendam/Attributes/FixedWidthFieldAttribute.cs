using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Services
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class FixedWidthFieldAttribute : Attribute
    {
        public int Start { get; }
        public int Length { get; }

        public FixedWidthFieldAttribute(int start, int length)
        {
            Start = start;
            Length = length;
        }
    }
}
