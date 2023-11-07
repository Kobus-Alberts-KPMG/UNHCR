using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace UNHCR.Geolocation.Infrastructure
{
    public interface IDataverseHttpClient
    {
        Task<HttpResponseMessage> GetRequestAsync(string uri);
        Task<HttpResponseMessage> PostRequestAsync(string uri, StringContent bodyString);
    }
}
