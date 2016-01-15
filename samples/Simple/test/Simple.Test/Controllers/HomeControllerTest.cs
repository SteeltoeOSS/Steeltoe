using Microsoft.AspNet.Mvc;
using Simple.Controllers;
using Simple.Model;
using Microsoft.Extensions.OptionsModel;
using Xunit;
using System;

namespace Simple.Test.Controllers
{
    public class HomeControllerTest
    {
        [Fact]
        public void ConfigServer_IOptionsNull()
        {
            HomeController controller = new HomeController(null);
            var result = controller.ConfigServer() as ViewResult;
            Assert.Equal("Not Available", result.ViewData["Bar"]);
            Assert.Equal("Not Available", result.ViewData["Foo"]);
            Assert.Equal("Not Available", result.ViewData["Info.Url"]);
            Assert.Equal("Not Available", result.ViewData["Info.Description"]);

        }

        [Fact]
        public void ConfigServer_NoDataFromConfigServer()
        {
            ConfigServerData data = new ConfigServerData();
            IOptions<ConfigServerData> options = new TestOptions<ConfigServerData>(data);
            
            HomeController controller = new HomeController(options);
            var result = controller.ConfigServer() as ViewResult;
            Assert.Equal("Not returned", result.ViewData["Bar"]);
            Assert.Equal("Not returned", result.ViewData["Foo"]);
            Assert.Equal("Not returned", result.ViewData["Info.Url"]);
            Assert.Equal("Not returned", result.ViewData["Info.Description"]);

        }
        [Fact]
        public void ConfigServer_GoodDataFromConfigServer()
        {
            ConfigServerData data = new ConfigServerData() { Bar = "Bar", Foo = "Foo" };
            data.Info = new Info() { Description = "Description", Url = "Url" };
            IOptions<ConfigServerData> options = new TestOptions<ConfigServerData>(data);

            HomeController controller = new HomeController(options);
            var result = controller.ConfigServer() as ViewResult;
            Assert.Equal("Bar", result.ViewData["Bar"]);
            Assert.Equal("Foo", result.ViewData["Foo"]);
            Assert.Equal("Url", result.ViewData["Info.Url"]);
            Assert.Equal("Description", result.ViewData["Info.Description"]);

        }
    }

    class TestOptions<T> : IOptions<T> where T : class, new()
    {
        public TestOptions(T value)
        {
            Value = value;
        }
        public T Value { get; set; }
 
    }
}
