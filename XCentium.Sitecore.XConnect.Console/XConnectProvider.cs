using Sitecore.XConnect.Client;
using Sitecore.XConnect.Client.WebApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sitecore.Xdb.Common.Web;
using Sitecore.XConnect.Schema;

using Sitecore.XConnect.Collection.Model;
using Microsoft.Extensions.Configuration;

namespace Sitecore.XConnect.Streaming
{
    public class XConnectProvider
    {
        //private readonly static string xConnectUrl = "https://sc9.xconnect/";
        //private readonly static string xConnectCertificateThumbprint = "StoreName=My;StoreLocation=LocalMachine;FindType=FindByThumbprint;FindValue=677E7CCCF8091DB63F8B2C07B5205ACA330336A0";
        private readonly IConfiguration _config;

        public XConnectProvider(IConfiguration config)
        {
            _config = config;
        }

        public async Task<XConnectClient> CreateXConnectClient()
        {
            var xConnectClientConfig = await CreateXConnectClientConfiguration();

            return new XConnectClient(xConnectClientConfig);
        }

        private async Task<XConnectClientConfiguration> CreateXConnectClientConfiguration()
        {
            var xConnectUri = _config.GetValue<string>("xconnect:uri");
            var xConnectCertificateConfig = _config.GetSection("xconnect:certificate");

            //var certificateOptions = CertificateWebRequestHandlerModifierOptions.Parse(xConnectCertificateThumbprint);
            var certificateModifier = new CertificateWebRequestHandlerModifier(xConnectCertificateConfig);

            var clientModifiers = new List<IHttpClientModifier>();
            var timeoutClientModifier = new TimeoutHttpClientModifier(new TimeSpan(0, 0, 60));
            clientModifiers.Add(timeoutClientModifier);

            var xConnectConfigurationClient = new ConfigurationWebApiClient(new Uri(xConnectUri + "configuration"), clientModifiers, new[] { certificateModifier });
            var xConnectCollectionClient = new CollectionWebApiClient(new Uri(xConnectUri + "odata"), clientModifiers, new[] { certificateModifier });
            var xConnectSearchClient = new SearchWebApiClient(new Uri(xConnectUri + "odata"), clientModifiers, new[] { certificateModifier });
  
            var xConnectClientConfig = new XConnectClientConfiguration(new XdbRuntimeModel(CollectionModel.Model), xConnectCollectionClient, xConnectSearchClient, xConnectConfigurationClient);

            //try
            //{
             await xConnectClientConfig.InitializeAsync();
            //}
            //catch (XdbModelConflictException ce)
            //{
            //    System.Console.WriteLine("ERROR:" + ce.Message);
            //}

            return xConnectClientConfig;
        }
    }
}
