using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Services.Core
{
	public static class MetricExtensions
	{
		public static LogServiceMetricEntry MapMetric(this BaseMetric metric)
		{
            if(metric == null)
                throw new ArgumentNullException(nameof(metric));

            var result = new LogServiceMetricEntry()
            {
                MetricType = metric.GetType().Name,
                EnvironmentName = metric.EnvironmentName,
                MachineName = metric.MachineName,
                ComponentName = metric.ComponentName,
                InstanceName = metric.InstanceName,
                DateUtc = metric.DateUtc,
                Value = metric.Value
            };

            if(metric is AggregateMetric aggregate)
            {
                result.IsAggregate = true;
                result.AggregateBy = aggregate.AggregateBy;
                result.AggregatePeriod = aggregate.AggregatePeriod;
            }

            foreach(var kvp in MetricAccessorCache.Get(metric.GetType()))
			{
				var value = kvp.Value(metric);
				result.ExtraProperties.Add(kvp.Key, value?.ToString());
			}

			return result;
		}
	}


    public static class MetricAccessorCache
    {
        public static readonly ConcurrentDictionary<Type, Dictionary<string, Func<BaseMetric, object>>> cache 
            = new ConcurrentDictionary<Type, Dictionary<string, Func<BaseMetric, object>>>();

        public static Dictionary<string, Func<BaseMetric, object>> Get(Type type) => cache.GetOrAdd(type, BuildAccessor);

        static Dictionary<string, Func<BaseMetric, object>> BuildAccessor(Type t)
        {
            var baseProperties = typeof(BaseMetric)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Select(p => p.Name);

            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && !baseProperties.Contains(p.Name))
                .ToDictionary(
                    prop => prop.Name,
                    prop => CreateGetter(t, prop)
                );

            return props;
        }

        private static Func<BaseMetric, object> CreateGetter(Type t, PropertyInfo prop)
        {
            var param = Expression.Parameter(typeof(BaseMetric), "m");
            var cast = Expression.Convert(param, t);
            var property = Expression.Property(cast, prop);
            var convert = Expression.Convert(property, typeof(object));
            var lambda = Expression.Lambda<Func<BaseMetric, object>>(convert, param);
            return lambda.Compile();
        }
    }
}
