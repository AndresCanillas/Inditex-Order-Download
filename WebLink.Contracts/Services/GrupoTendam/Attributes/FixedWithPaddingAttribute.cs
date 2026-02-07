using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Services
{
    public enum PaddingDirection
    {
        Left,
        Right
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class FixedWidthPaddingAttribute : Attribute
    {
        public PaddingDirection Direction { get; set; }
        public char PaddingChar { get; set; }

        public FixedWidthPaddingAttribute(PaddingDirection direction = PaddingDirection.Right, char paddingChar = ' ')
        {
            Direction = direction;
            PaddingChar = paddingChar;
        }
    }
}
