using Microsoft.Extensions.DependencyInjection;

namespace Service.Contracts
{
    public static class FactoryExtensions
    {
        public static void AddServices(this IServiceCollection services, IFactory factory)
        {
            factory.ForEach((service, implementation, isSingleton, instance) =>
            {
                if(isSingleton)
                {
                    if(instance != null)
                    {
                        services.AddSingleton(service, instance);
                    }
                    else if(service.IsGenericType)
                    {
                        services.AddSingleton(service, implementation);
                    }
                    else
                    {
                        services.AddSingleton(service, (sp) => factory.GetInstance(service));
                    }
                }
                else
                {
                    if(service.IsGenericType)
                    {
                        services.AddSingleton(service, implementation);
                    }
                    else
                    {
                        services.AddTransient(service, (sp) => factory.GetInstance(service));
                    }
                }
            });
        }
    }
}
