// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Extensions;
using System;
using System.Collections.Generic;

namespace Steeltoe.Stream
{
    public abstract class AbstractTest
    {
        protected virtual ServiceCollection CreateStreamsContainerWithDefaultBindings(List<string> serachDirectories, params string[] properties)
        {
            var container = CreateStreamsContainer(serachDirectories, properties);
            container.AddDefaultBindings();
            return container;
        }

        protected virtual ServiceCollection CreateStreamsContainerWithDefaultBindings(params string[] properties)
        {
            return CreateStreamsContainerWithDefaultBindings(null, properties);
        }

        protected virtual ServiceCollection CreateStreamsContainer(params string[] properties)
        {
            return CreateStreamsContainer(null, properties);
        }

        protected virtual ServiceCollection CreateStreamsContainer(List<string> serachDirectories, params string[] properties)
        {
            var configuration = CreateTestConfiguration(properties);
            var container = new ServiceCollection();
            container.AddOptions();
            container.AddLogging((b) => b.AddDebug());

            container.AddSingleton<IConfiguration>(configuration);
            container.AddSingleton<IApplicationContext, GenericApplicationContext>();
            container.AddStreamConfiguration(configuration);
            container.AddCoreServices();
            container.AddIntegrationServices(configuration);
            container.AddStreamCoreServices(configuration);
            if (serachDirectories == null || serachDirectories.Count == 0)
            {
                container.AddBinderServices(configuration);
            }
            else
            {
                var registry = new DefaultBinderTypeRegistry(serachDirectories, false);
                container.AddSingleton<IBinderTypeRegistry>(registry);
                container.AddBinderServices(registry, configuration);
            }

            return container;
        }

        protected virtual ServiceCollection CreateStreamsContainerWithBinding(List<string> serachDirectories, Type bindingType, params string[] properties)
        {
            var collection = CreateStreamsContainer(serachDirectories, properties);
            collection.AddStreamBindings(bindingType);
            return collection;
        }

        protected virtual ServiceCollection CreateStreamsContainerWithIProcessorBinding(List<string> serachDirectories, params string[] properties)
        {
            var collection = CreateStreamsContainer(serachDirectories, properties);
            collection.AddProcessorStreamBinding();
            return collection;
        }

        protected virtual ServiceCollection CreateStreamsContainerWithISinkBinding(List<string> serachDirectories, params string[] properties)
        {
            var collection = CreateStreamsContainer(serachDirectories, properties);
            collection.AddSinkStreamBinding();
            return collection;
        }

        protected virtual ServiceCollection CreateStreamsContainerWithISourceBinding(List<string> serachDirectories, params string[] properties)
        {
            var collection = CreateStreamsContainer(serachDirectories, properties);
            collection.AddSourceStreamBinding();
            return collection;
        }

        protected virtual IConfiguration CreateTestConfiguration(params string[] properties)
        {
            var keyValuePairs = ParseProperties(properties);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(keyValuePairs);
            return configurationBuilder.Build();
        }

        protected virtual List<string> GetSearchDirectories(params string[] names)
        {
            var results = new List<string>();

            var currentDirectory = Environment.CurrentDirectory;

            foreach (var name in names)
            {
                var dir = currentDirectory.Replace("Base.Test", name);
                results.Add(dir);
            }

            return results;
        }

        protected virtual List<KeyValuePair<string, string>> ParseProperties(string[] properties)
        {
            var result = new List<KeyValuePair<string, string>>();
            if (properties == null)
            {
                return result;
            }

            foreach (var prop in properties)
            {
                var split = prop.Split("=");
                split[0] = split[0].Replace('.', ':');
                result.Add(new KeyValuePair<string, string>(split[0], split[1]));
            }

            return result;
        }

        protected ConsumerOptions GetConsumerOptions(string bindingName)
        {
            var options = new ConsumerOptions();
            options.PostProcess(bindingName);
            return options;
        }

        protected ProducerOptions GetProducerOptions(string bindingName)
        {
            var options = new ProducerOptions();
            options.PostProcess(bindingName);
            return options;
        }
    }
}
