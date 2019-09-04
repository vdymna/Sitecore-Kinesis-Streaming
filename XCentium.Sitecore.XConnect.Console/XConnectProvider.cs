using Sitecore.XConnect.Client;
using Sitecore.XConnect.Client.WebApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Sitecore.Xdb.Common.Web;
using Sitecore.XConnect.Schema;

using Sitecore.XConnect.Collection.Model;

namespace Sitecore.XConnect.Streaming
{
    public class XConnectProvider
    {
        private const string XConnectUrl = "https://sc9.xconnect/";
        private const string XConnectCertificate = "StoreName=My;StoreLocation=LocalMachine;FindType=FindByThumbprint;FindValue=677E7CCCF8091DB63F8B2C07B5205ACA330336A0";

        public async Task<XConnectClient> CreateXConnectClient()
        {
            var xConnectClientConfiguration = await CreateXConnectClientConfiguration();

            return new XConnectClient(xConnectClientConfiguration);
        }

        private async Task<XConnectClientConfiguration> CreateXConnectClientConfiguration()
        {
            var uri = new Uri(XConnectUrl);

            var certificateOptions = CertificateWebRequestHandlerModifierOptions.Parse(XConnectCertificate);

            var certificateModifier = new CertificateWebRequestHandlerModifier(certificateOptions);

            var clientModifiers = new List<IHttpClientModifier>();
            var timeoutClientModifier = new TimeoutHttpClientModifier(new TimeSpan(0, 0, 60));
            clientModifiers.Add(timeoutClientModifier);


            var xConnectConfigurationClient = new ConfigurationWebApiClient(new Uri(uri + "configuration"), clientModifiers, new[] { certificateModifier });
            var xConnectCollectionClient = new CollectionWebApiClient(new Uri(uri + "odata"), clientModifiers, new[] { certificateModifier });
            var xConnectSearchClient = new SearchWebApiClient(new Uri(uri + "odata"), clientModifiers, new[] { certificateModifier });

            
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
