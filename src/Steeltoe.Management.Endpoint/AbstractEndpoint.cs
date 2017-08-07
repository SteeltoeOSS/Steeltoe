using System;


namespace Steeltoe.Management.Endpoint
{
    public abstract class AbstractEndpoint : IEndpoint
    {
        protected IEndpointOptions options;

        public AbstractEndpoint(IEndpointOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this.options = options;
        }
        public virtual string Id { get { return options.Id; } }

        public virtual bool Enabled { get { return options.Enabled; } }

        public virtual bool Sensitive { get { return options.Sensitive; } }

        public virtual IEndpointOptions Options { get { return options; } }

        public string Path { get { return options.Path; } }
    }

    public abstract class AbstractEndpoint<TResult> : AbstractEndpoint, IEndpoint<TResult>
    {
        public AbstractEndpoint(IEndpointOptions options) : base(options)
        {
        }

        public virtual TResult Invoke()
        {
            return default(TResult);
        }
    }

    public abstract class AbstractEndpoint<TResult, TRequest> : AbstractEndpoint, IEndpoint<TResult, TRequest>
    {
        public AbstractEndpoint(IEndpointOptions options) : base(options)
        {
        }

        public virtual TResult Invoke(TRequest arg)
        {
            return default(TResult);
        }
    }
}
