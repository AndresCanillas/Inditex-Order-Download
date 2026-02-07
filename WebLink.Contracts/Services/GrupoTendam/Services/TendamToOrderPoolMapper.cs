using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using WebLink.Contracts.Models;


namespace WebLink.Contracts.Services
{
    public static class TendamToOrderPoolMapper
    {
        public static OrderPool MapToOrderPool(TendamMapping source)
        {
            if(source == null)
                throw new ArgumentNullException(nameof(source));

            var destination = new OrderPool
            {
                CreationDate = DateTime.Now
            };

            var extraDataDict = new Dictionary<string, object>();

            var sourceProperties = typeof(TendamMapping).GetProperties();
            var destinationType = typeof(OrderPool);

            foreach(var sourceProp in sourceProperties)
            {
                var mappingAttr = sourceProp.GetCustomAttribute<OrderPoolMappingAttribute>();
                if(mappingAttr == null)
                    continue;

                var sourceValue = sourceProp.GetValue(source);

                // Saltar valores nulos o vacíos
                if(sourceValue == null || (sourceValue is string str && string.IsNullOrWhiteSpace(str)))
                    sourceValue = " ";

                if(mappingAttr.DestinationProperty == "ExtraData")
                {
                    // Agregar al diccionario para ExtraData
                    extraDataDict[sourceProp.Name] = sourceValue;
                }
                else
                {
                    // Mapeo directo a propiedad del OrderPool
                    var destProp = destinationType.GetProperty(mappingAttr.DestinationProperty);
                    if(destProp != null && destProp.CanWrite)
                    {
                        SetPropertyValue(destProp, destination, sourceValue);
                    }
                }
            }

            // Serializar ExtraData a JSON usando Newtonsoft.Json
            if(extraDataDict.Count > 0)
            {
                destination.ExtraData = JsonConvert.SerializeObject(extraDataDict, Formatting.None);
            }

            return destination;
        }

        public static List<OrderPool> MapToOrderPoolList(IEnumerable<TendamMapping> sources)
        {
            return sources?.Select(MapToOrderPool).ToList() ?? new List<OrderPool>();
        }

        public static TendamMapping MapToTendamMapping(OrderPool source)
        {
            if(source == null)
                throw new ArgumentNullException(nameof(source));

            var destination = new TendamMapping();

            // Deserializar ExtraData usando Newtonsoft.Json
            JObject extraData = null;
            if(!string.IsNullOrWhiteSpace(source.ExtraData))
            {
                try
                {
                    extraData = JsonConvert.DeserializeObject<JObject>(source.ExtraData);
                }
                catch
                {
                    // Si falla la deserialización, continuar sin ExtraData
                }
            }

            var destinationProperties = typeof(TendamMapping).GetProperties();
            var sourceType = typeof(OrderPool);

            foreach(var destProp in destinationProperties)
            {
                var mappingAttr = destProp.GetCustomAttribute<OrderPoolMappingAttribute>();
                if(mappingAttr == null)
                    continue;

                if(mappingAttr.DestinationProperty == "ExtraData")
                {
                    // Obtener valor desde ExtraData JSON
                    if(extraData != null && extraData.TryGetValue(destProp.Name, out var jToken))
                    {
                        SetPropertyValueFromJToken(destProp, destination, jToken);
                    }
                }
                else
                {
                    // Mapeo directo desde OrderPool
                    var sourceProp = sourceType.GetProperty(mappingAttr.DestinationProperty);
                    if(sourceProp != null && sourceProp.CanRead)
                    {
                        var sourceValue = sourceProp.GetValue(source);
                        if(sourceValue != null)
                        {
                            SetPropertyValue(destProp, destination, sourceValue);
                        }
                    }
                }
            }

            return destination;
        }

        public static List<TendamMapping> MapToTendamMappingList(IEnumerable<OrderPool> sources)
        {
            return sources?.Select(MapToTendamMapping).ToList() ?? new List<TendamMapping>();
        }

        private static void SetPropertyValue(PropertyInfo destProp, object destination, object value)
        {
            try
            {
                var targetType = Nullable.GetUnderlyingType(destProp.PropertyType) ?? destProp.PropertyType;

                if(targetType == typeof(int))
                {
                    if(int.TryParse(value?.ToString()?.Trim(), out var intValue))
                        destProp.SetValue(destination, intValue);
                }
                else if(targetType == typeof(DateTime))
                {
                    if(DateTime.TryParse(value?.ToString()?.Trim(), out var dateValue))
                        destProp.SetValue(destination, dateValue);
                }
                else if(targetType == typeof(string))
                {
                    destProp.SetValue(destination, value?.ToString()?.Trim());
                }
                else
                {
                    destProp.SetValue(destination, Convert.ChangeType(value, targetType));
                }
            }
            catch
            {
                // Ignorar errores de conversión
            }
        }

        private static void SetPropertyValueFromJToken(PropertyInfo destProp, object destination, JToken jToken)
        {
            try
            {
                var targetType = destProp.PropertyType;

                if(targetType == typeof(string))
                {
                    if(jToken.Type == JTokenType.String)
                    {
                        destProp.SetValue(destination, jToken.Value<string>()?.Trim());
                    }
                    else
                    {
                        destProp.SetValue(destination, jToken.ToString().Trim());
                    }
                }
                else if(targetType == typeof(int) || targetType == typeof(int?))
                {
                    if(jToken.Type == JTokenType.Integer)
                    {
                        destProp.SetValue(destination, jToken.Value<int>());
                    }
                    else if(int.TryParse(jToken.ToString(), out var parsedInt))
                    {
                        destProp.SetValue(destination, parsedInt);
                    }
                }
                else if(targetType == typeof(DateTime) || targetType == typeof(DateTime?))
                {
                    if(jToken.Type == JTokenType.Date)
                    {
                        destProp.SetValue(destination, jToken.Value<DateTime>());
                    }
                    else if(jToken.Type == JTokenType.String && DateTime.TryParse(jToken.Value<string>(), out var dateValue))
                    {
                        destProp.SetValue(destination, dateValue);
                    }
                }
            }
            catch
            {
                // Ignorar errores de conversión
            }
        }
    }
}