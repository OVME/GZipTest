using System.Reflection;
using Autofac;

namespace GZipTest.Composition
{
    public static class ContainerConfig
    {
        public static IContainer Configure()
        {
            var builder = new ContainerBuilder();

            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly()).AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }
    }
}
