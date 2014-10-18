using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace Discouser
{
    class Logger
    {
        internal async Task Log(Exception ex, string Info)
        {
            await Task.FromResult(0);
        }

        internal async Task Log(HttpResponseMessage result)
        {
            await Task.FromResult(0);
        }
    }
}
