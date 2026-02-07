using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Services
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class OrderPoolMappingAttribute : Attribute
    {
        public string DestinationProperty { get; set; }

        public OrderPoolMappingAttribute(string destinationProperty)
        {
            DestinationProperty = destinationProperty;
        }
    }
}
