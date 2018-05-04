using Autofac;
using GZipTest.Composition;

namespace GZipTest
{
    public class Program
    {
        static void Main(string[] args)
        {
            var container = ContainerConfig.Configure();

            using (var scope = container.BeginLifetimeScope())
            {
                var application = scope.Resolve<IGZipTestApplication>();
                application.Run(args);
            }
        }
    }
}
