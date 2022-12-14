using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Minded.Extensions.Configuration;

namespace Minded.Framework.Mediator
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMediator(this MindedBuilder builder)
        {
            builder.Register(sc => sc.AddSingleton<IMediator>(service => new Mediator(service)));
        }
    }
}
