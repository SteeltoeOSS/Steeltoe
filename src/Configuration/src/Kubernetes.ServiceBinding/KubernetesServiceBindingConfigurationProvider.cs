// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding;

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
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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

            foreach (KeyValuePair<string, string> secretEntry in binding.Secrets)
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

    private void AddBindingType(ServiceBinding binding, Dictionary<string, string> data)
    {
        string typeKey = ConfigurationPath.Combine(FromKeyPrefix, binding.Name, TypeKey);

        if (!_source.IgnoreKeyPredicate(typeKey))
        {
            data[typeKey] = binding.Type;
        }
    }

    private void AddBindingProvider(ServiceBinding binding, Dictionary<string, string> data)
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

    private void AddBindingSecret(ServiceBinding binding, KeyValuePair<string, string> secretEntry, Dictionary<string, string> data)
    {
        string secretKey = ConfigurationPath.Combine(FromKeyPrefix, binding.Name, secretEntry.Key);

        if (!_source.IgnoreKeyPredicate(secretKey))
        {
            data[secretKey] = secretEntry.Value;
        }
    }

    internal sealed class ServiceBinding
    {
        private readonly Dictionary<string, string> _secrets;

        public string Name { get; }
        public string Path { get; }
        public string? Provider { get; }
        public IDictionary<string, string> Secrets => new ReadOnlyDictionary<string, string>(_secrets);
        public string Type { get; }

        // Creates a new Binding instance using the specified file system directory
        public ServiceBinding(string path)
            : this(System.IO.Path.GetFileName(path), path, CreateSecrets(path))
        {
        }

        // Creates a new Binding instance using the specified content
        private ServiceBinding(string name, string path, IDictionary<string, string> secrets)
        {
            Name = name ?? throw new ArgumentException("Binding has no name and is not a valid binding");
            Path = path ?? throw new ArgumentException("Binding has no path and is not a valid binding");

            _secrets = new Dictionary<string, string>();

            string? type = null;
            string? provider = null;

            foreach ((string secretName, string secretValue) in secrets)
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

        private static IDictionary<string, string> CreateSecrets(string path)
        {
            return CreateFilePerEntry(path);
        }

        private static IDictionary<string, string> CreateFilePerEntry(string path)
        {
            try
            {
                IEnumerable<string> files = Directory.EnumerateFiles(path);
                var result = new Dictionary<string, string>();

                foreach (string file in files)
                {
                    FileInfo fileInfo = new(file);

                    if (fileInfo.Exists && !IsHidden(file))
                    {
                        result.Add(fileInfo.Name, ReadContentsAsString(file));
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
                byte[] bytes = File.ReadAllBytes(file);
                return Encoding.UTF8.GetString(bytes).Trim();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Unable to read file {file}", e);
            }
        }
    }

    internal sealed class ServiceBindings
    {
        public IList<ServiceBinding> Bindings { get; }

        public ServiceBindings(IFileProvider? fileProvider)
        {
            Bindings = new List<ServiceBinding>();

            if (fileProvider == null)
            {
                return;
            }

            IDirectoryContents contents = fileProvider.GetDirectoryContents("/");

            foreach (IFileInfo element in contents.Where(element => element?.Exists == true))
            {
                Bindings.Add(new ServiceBinding(element.PhysicalPath));
            }
        }
    }
}
