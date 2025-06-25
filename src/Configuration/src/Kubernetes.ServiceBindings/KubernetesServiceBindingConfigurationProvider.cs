// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Steeltoe.Configuration.Kubernetes.ServiceBindings;

internal sealed class KubernetesServiceBindingConfigurationProvider : PostProcessorConfigurationProvider, IDisposable
{
    public const string ProviderKey = "provider";
    public const string TypeKey = "type";
    public static readonly string FromKeyPrefix = ConfigurationPath.Combine("k8s", "bindings");
    public static readonly string ToKeyPrefix = ConfigurationPath.Combine("steeltoe", "service-bindings");

    private readonly IDisposable? _changeToken;
    private readonly KubernetesServiceBindingConfigurationSource _source;

    public KubernetesServiceBindingConfigurationProvider(KubernetesServiceBindingConfigurationSource source)
        : base(source)
    {
        _source = source;

        if (source.ReloadOnChange && _source.FileProvider != null)
        {
            _changeToken = ChangeToken.OnChange(() => _source.FileProvider.Watch("*"), () =>
            {
                Thread.Sleep(_source.ReloadDelay); // Default 250
                Load(true);
            });
        }
    }

    public override void Load()
    {
        Load(false);
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

            throw new DirectoryNotFoundException("A service binding root is required when the source is not optional.");
        }

        IDirectoryContents directory = _source.FileProvider.GetDirectoryContents("/");

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

        var bindings = new ServiceBindings(_source.FileProvider);

        foreach (ServiceBinding binding in bindings.Bindings)
        {
            AddBindingType(binding, data);
            AddBindingProvider(binding, data);

            foreach (KeyValuePair<string, string?> secretEntry in binding.Secrets)
            {
                AddBindingSecret(binding, secretEntry, data);
            }
        }

        Data = data;
        PostProcessConfiguration();
        OnReload();
    }

    public void Dispose()
    {
        _changeToken?.Dispose();
    }

    private void AddBindingType(ServiceBinding binding, Dictionary<string, string?> data)
    {
        string typeKey = ConfigurationPath.Combine(FromKeyPrefix, binding.Name, TypeKey);

        if (!_source.IgnoreKeyPredicate(typeKey))
        {
            data[typeKey] = binding.Type;
        }
    }

    private void AddBindingProvider(ServiceBinding binding, Dictionary<string, string?> data)
    {
        if (!string.IsNullOrEmpty(binding.Provider))
        {
            string providerKey = ConfigurationPath.Combine(FromKeyPrefix, binding.Name, ProviderKey);

            if (!_source.IgnoreKeyPredicate(providerKey))
            {
                data[providerKey] = binding.Provider;
            }
        }
    }

    private void AddBindingSecret(ServiceBinding binding, KeyValuePair<string, string?> secretEntry, Dictionary<string, string?> data)
    {
        string secretKey = ConfigurationPath.Combine(FromKeyPrefix, binding.Name, secretEntry.Key);

        if (!_source.IgnoreKeyPredicate(secretKey))
        {
            data[secretKey] = secretEntry.Value;
        }
    }

    internal sealed class ServiceBinding
    {
        private readonly Dictionary<string, string?> _secrets;

        public string Name { get; }
        public string Path { get; }
        public string? Provider { get; }
        public ReadOnlyDictionary<string, string?> Secrets => new(_secrets);
        public string Type { get; }

        // Creates a new Binding instance using the specified file system directory
        public ServiceBinding(string path, IFileProvider fileProvider)
            : this(System.IO.Path.GetFileName(path), path, CreateSecrets(path, fileProvider))
        {
        }

        // Creates a new Binding instance using the specified content
        private ServiceBinding(string name, string path, Dictionary<string, string?> secrets)
        {
            Name = name ?? throw new ArgumentException("Binding has no name and is not a valid binding");
            Path = path ?? throw new ArgumentException("Binding has no path and is not a valid binding");

            _secrets = [];

            string? type = null;
            string? provider = null;

            foreach ((string secretName, string? secretValue) in secrets)
            {
                switch (secretName)
                {
                    case TypeKey:
                        type = secretValue;
                        break;
                    case ProviderKey:
                        provider = secretValue;
                        break;
                    default:
                        _secrets.Add(secretName, secretValue);
                        break;
                }
            }

            Type = type ?? throw new ArgumentException($"{path} has no type and is not a valid binding");
            Provider = provider;
        }

        private static Dictionary<string, string?> CreateSecrets(string path, IFileProvider fileProvider)
        {
            return CreateFilePerEntry(path, fileProvider);
        }

        private static Dictionary<string, string?> CreateFilePerEntry(string path, IFileProvider fileProvider)
        {
            try
            {
                IDirectoryContents directoryContents = fileProvider.GetDirectoryContents(path);
                var result = new Dictionary<string, string?>();

                foreach (IFileInfo fileInfo in directoryContents.Where(element => !element.IsDirectory))
                {
                    if (fileInfo.Exists)
                    {
                        string fileContents = ReadContentsAsString(fileInfo);
                        result.Add(fileInfo.Name, fileContents);
                    }
                }

                return result;
            }
            catch (Exception)
            {
                // Log
                throw new ArgumentException($"Path '{path}' is invalid");
            }
        }

        private static string ReadContentsAsString(IFileInfo fileInfo)
        {
            using Stream stream = fileInfo.CreateReadStream();
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd().Trim();
        }
    }

    internal sealed class ServiceBindings
    {
        public List<ServiceBinding> Bindings { get; } = [];

        public ServiceBindings(IFileProvider? fileProvider)
        {
            if (fileProvider != null)
            {
                IDirectoryContents directoryContents = fileProvider.GetDirectoryContents("/");

                foreach (IFileInfo fileInfo in directoryContents.Where(element => element is { Exists: true, IsDirectory: true }))
                {
                    Bindings.Add(new ServiceBinding(fileInfo.Name, fileProvider));
                }
            }
        }
    }
}
