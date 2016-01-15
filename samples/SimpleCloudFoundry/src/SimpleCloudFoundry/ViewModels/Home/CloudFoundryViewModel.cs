using Spring.Extensions.Configuration.CloudFoundry;


namespace SimpleCloudFoundry.ViewModels.Home
{
    public class CloudFoundryViewModel
    {
        public CloudFoundryViewModel(CloudFoundryApplicationOptions appOptions, CloudFoundryServicesOptions servOptions)
        {
            CloudFoundryServices = servOptions;
            CloudFoundryApplication = appOptions;
        }
        public CloudFoundryServicesOptions CloudFoundryServices { get;}
        public CloudFoundryApplicationOptions CloudFoundryApplication { get;}
    }
}
