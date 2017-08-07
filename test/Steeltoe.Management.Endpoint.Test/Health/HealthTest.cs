using Newtonsoft.Json;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Health.Test
{
    public class HealthTest : BaseTest
    {
        [Fact]
        public void Serialize()
        {
            Health health = new Health()
            {
                Status = HealthStatus.OUT_OF_SERVICE,
                Description = "Test",
                Details = new Dictionary<string, object>()
                {
                    {"item1", new HealtData()},
                    {"item2", "String" },
                    {"item3", false }
                }
            };
            var json = Serialize(health);
            Assert.Equal("{\"status\":\"OUT_OF_SERVICE\",\"description\":\"Test\",\"item1\":{\"StringProperty\":\"Testdata\",\"IntProperty\":100,\"BoolProperty\":true},\"item2\":\"String\",\"item3\":false}", json);
        }
        private string Serialize(Health result)
        {
            try
            {
                return JsonConvert.SerializeObject(result, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
            catch (Exception )
            {
                
            }
            return string.Empty;
        }


    }
    class HealtData
    {
        public string StringProperty { get; set; } = "Testdata";
        public int IntProperty { get; set; } = 100;
        public bool BoolProperty { get; set; } = true;
    }
}
