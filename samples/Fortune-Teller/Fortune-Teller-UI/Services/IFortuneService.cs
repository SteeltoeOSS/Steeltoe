using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fortune_Teller_UI.Services
{
    public interface IFortuneService
    {
        Task<string> RandomFortuneAsync();
    }
}
