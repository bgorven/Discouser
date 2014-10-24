using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace Discouser
{
    class Logger
    {
        internal async Task Log(string error, string info = "")
        {
            await Task.FromResult(0);
        }

        internal async Task Log(Exception ex, string info = "")
        {
            await Task.FromResult(0);
        }

        internal async Task Log(HttpResponseMessage result, string info = "")
        {
            await Task.FromResult(0);
        }
    }
}
