// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding;
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
internal class ServiceBindingConfigurationProvider : PostProcessorConfigurationProvider, IDisposable
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
{

    public static readonly string KubernetesBindingsPrefix = "k8s" + ConfigurationPath.KeyDelimiter + "bindings";

    // The key for the provider of binding
    public const string ProviderKey = "provider";

    // The key for the type of binding
    public const string TypeKey = "type";

    internal class ServiceBinding
    {
        public string Name { get; }

        public string Path { get; }

        public string? Provider { get; }

        public IDictionary<string, string> Secrets => new ReadOnlyDictionary<string, string>(_secrets);

        public string Type { get; }

        private readonly Dictionary<string, string> _secrets;

        // Creates a new Binding instance using the specified file system directory
        public ServiceBinding(string path)
            : this(new DirectoryInfo(path).Name, path, CreateSecrets(path))
        {
        }

        // Creates a new Binding instance using the specified content
        public ServiceBinding(string name, string path, IDictionary<string, string> secret)
        {
            Name = name ?? throw new ArgumentException("Binding has no name and is not a valid binding");
            Path = path ?? throw new ArgumentException("Binding has no path and is not a valid binding");

            _secrets = new Dictionary<string, string>();

            string? type = null;
            string? provider = null;
            foreach (var entry in secret)
            {
                switch (entry.Key)
                {
                    case TypeKey:
                        type = entry.Value;
                        break;
                    case ProviderKey:
                        provider = entry.Value;
                        break;
                    default:
                        _secrets.Add(entry.Key, entry.Value);
                        break;
                }
            }

            Type = type ?? throw new ArgumentException($"{path} has no type and is not a valid binding");
            Provider = provider;
        }


        private static IDictionary<string, string> CreateSecrets(string path)
        {
            return CreateFilePerEntry(path);
        }

        private static IDictionary<string, string> CreateFilePerEntry(string path)
        {
            var files = Directory.EnumerateFiles(path);
            var result = new Dictionary<string, string>();
            if (!files.Any())
            {
                return result;
            }

            foreach (var file in files)
            {
                FileInfo fileInfo = new(file);
                if (fileInfo.Exists && !IsHidden(file))
                {
                    result.Add(fileInfo.Name, ReadContentsAsString(file));
                }
            }

            return result;
        }

        private static bool IsHidden(string file)
        {
            try
            {
                return (File.GetAttributes(file) & FileAttributes.Hidden) != 0;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Unable to determine if file {file} is hidden", e);
            }
        }

        private static string ReadContentsAsString(string file)
        {
            try
            {
                var bytes = File.ReadAllBytes(file);
                return Encoding.UTF8.GetString(bytes).Trim();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Unable to read file {file}", e);
            }
        }
    }

    internal class K8ServiceBindings
    {
        public IList<ServiceBinding> Bindings { get; }

        public K8ServiceBindings(IFileProvider fileProvider)
        {
            Bindings = new List<ServiceBinding>();
            if (fileProvider == null)
            {
                return;
            }
            var contents = fileProvider.GetDirectoryContents("/");
            foreach (var element in contents)
            {
                if (element != null && element.Exists)
                {
                    Bindings.Add(new ServiceBinding(element.PhysicalPath));
                }
            }
        }
    }

    private readonly IDisposable? _changeTokenRegistration;

    private readonly ServiceBindingConfigurationSource _source;

    public ServiceBindingConfigurationProvider(ServiceBindingConfigurationSource source)
        : base(source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));

        if (source.ReloadOnChange && _source.FileProvider != null)
        {
            _changeTokenRegistration = ChangeToken.OnChange(
                () => _source.FileProvider.Watch("*"),
                () =>
                {
                    Thread.Sleep(_source.ReloadDelay); // Default 250
                    Load(reload: true);
                });
        }
    }

    public override void Load()
    {
        Load(reload: false);
    }

    private void Load(bool reload)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (_source.FileProvider == null)
        {
            // Always optional on reload
            if (_source.Optional || reload) 
            {
                Data = data;
                PostProcessConfiguration();
                OnReload();
                return;
            }

            throw new DirectoryNotFoundException("A service binding root is required when this source is not optional.");
        }

        var directory = _source.FileProvider.GetDirectoryContents("/");
        if (!directory.Exists)
        {
            // Always optional on reload
            if (_source.Optional || reload) 
            {
                Data = data;
                PostProcessConfiguration();
                OnReload();
                return;
            }
            throw new DirectoryNotFoundException("The service binding root does not exist and is not optional.");
        }
        else
        {
            var bindings = new K8ServiceBindings(_source.FileProvider);
            foreach (var binding in bindings.Bindings)
            {
                AddBindingType(binding, data);
                AddBindingProvider(binding, data);
                foreach (var secretEntry in binding.Secrets)
                {
                    AddBindingSecret(binding, secretEntry, data);
                }
            }
        }

        Data = data;
        PostProcessConfiguration();
        OnReload();
    }

    public void Dispose()
    {
        _changeTokenRegistration?.Dispose();
    }

    private void AddBindingType(ServiceBinding binding, Dictionary<string, string?> data)
    {
        var typeKey = KubernetesBindingsPrefix + ConfigurationPath.KeyDelimiter + binding.Name + ConfigurationPath.KeyDelimiter + TypeKey;
        if (!_source.IgnoreKeyPredicate(typeKey))
        {
            data[typeKey] = binding.Type;
        }
    }

    private void AddBindingProvider(ServiceBinding binding, Dictionary<string, string?> data)
    {
        if (!string.IsNullOrEmpty(binding.Provider))
        {
            var provKey = KubernetesBindingsPrefix + ConfigurationPath.KeyDelimiter + binding.Name + ConfigurationPath.KeyDelimiter + ProviderKey;
            if (!_source.IgnoreKeyPredicate(provKey))
            {
                data[provKey] = binding.Provider;
            }
        }
    }

    private void AddBindingSecret(ServiceBinding binding, KeyValuePair<string, string> secretEntry, Dictionary<string, string?> data)
    {
        var secretkey = KubernetesBindingsPrefix + ConfigurationPath.KeyDelimiter + binding.Name + ConfigurationPath.KeyDelimiter + secretEntry.Key;
        if (!_source.IgnoreKeyPredicate(secretkey))
        {
            data[secretkey] = secretEntry.Value;
        }
    }
}
