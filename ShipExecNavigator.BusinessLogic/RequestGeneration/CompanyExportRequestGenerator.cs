using PSI.Sox;
using PSI.Sox.Data;
using PSI.Sox.Wcf;
using PSI.Sox.Wcf.Administration;
using PSI.Sox.Wcf.Authentication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ShipExecNavigator.BusinessLogic.RequestGeneration
{
    public class CompanyExportRequestGenerator
    {
        private string _adminUrl { get; set; }
        private Guid CompanyGuid { get; set; }
        protected string _accessToken { get; set; }

        public CompanyExportRequestGenerator(string adminUrl, Guid companyId, string accessToken)
        {
            _adminUrl = adminUrl;
            CompanyGuid = companyId;
            _accessToken = accessToken;
        }

 


        public Company GetLatestCompanyAndWriteToFileWithIds(string filePath, string originalFileName, string modifiedFileName)
        {
            Company result = SaveCompanyToOutputFile(Path.Combine(filePath, originalFileName), CompanyGuid.ToString());
            //SaveCompanyToOutputFile(Path.Combine(filePath, modifiedFileName), CompanyGuid.ToString());
            return result;
        }

        public string GetLatestCompanyXmlString(string path, string companyName, HashSet<string>? loadedSections = null)
        {
            var outPath = String.IsNullOrEmpty(path) ? Path.GetTempPath() : path;
            string tempFile = Path.Combine(outPath, companyName + "_" + Guid.NewGuid() + "_" + DateTime.Now.ToString("yyyyMMddHHmmss_mmHHss") + ".xml");
            try
            {
                SaveCompanyToOutputFile(tempFile, CompanyGuid.ToString(), loadedSections);
                return File.ReadAllText(tempFile);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public Company SaveCompanyToOutputFile(string filePath, string companyGuid, HashSet<string>? loadedSections = null)
        {
            GetCompanyRequest getRequest = new GetCompanyRequest();
            getRequest.Id = Guid.Parse(companyGuid);

            Company result;
            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _adminUrl + "GetCompany"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                string json = JsonHelper.Serialize(getRequest);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();

                string content = httpResponse.Content.ReadAsStringAsync().Result;
                GetCompanyResponse response = JsonHelper.Deserialize<GetCompanyResponse>(content);
                result = response.Company;
            }

            if (loadedSections is null || loadedSections.Contains("Profiles"))
            {
                GetCompanyProfilesRequest profilesRequest = new GetCompanyProfilesRequest();
                profilesRequest.CompanyId = Guid.Parse(companyGuid);
                profilesRequest.SearchCriteria = new SearchCriteria
                {
                    WhereClauses = new List<WhereClause>(),
                    OrderByClauses = new List<OrderByClause>()
                };

                using (var httpClient = new HttpClient())
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _adminUrl + "GetCompanyProfiles"))
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                    string json = JsonHelper.Serialize(profilesRequest);
                    requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                    httpResponse.EnsureSuccessStatusCode();

                    string content = httpResponse.Content.ReadAsStringAsync().Result;
                    GetProfilesResponse profilesResponse = JsonHelper.Deserialize<GetProfilesResponse>(content);
                    result.Profiles = profilesResponse?.Profiles ?? new List<Profile>();
                }
            }

            GetSsoConfigurationRequest ssoRequest = new GetSsoConfigurationRequest();
            ssoRequest.CompanyId = Guid.Parse(companyGuid);
            ssoRequest.CompanySymbol = result.Symbol;

            //using (var httpClient = new HttpClient())
            //using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _adminUrl + "GetSsoConfiguration"))
            //{
            //    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            //    string json = JsonHelper.Serialize(ssoRequest);
            //    requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

            //    HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
            //    httpResponse.EnsureSuccessStatusCode();

            //    string content = httpResponse.Content.ReadAsStringAsync().Result;
            //    GetSsoConfigurationResponse ssoResponse = JsonHelper.Deserialize<GetSsoConfigurationResponse>(content);
            //    if (ssoResponse?.SsoConfiguration != null)
            //        result.SsoConfigurations = new List<SsoConfiguration> { ssoResponse.SsoConfiguration };
            //}

            if (loadedSections is null)
                PopulateCompany(ref result);
            else
                PopulateCompanySelective(ref result, loadedSections);

            XmlSerializer xsSubmit = new XmlSerializer(typeof(Company));
            string xml = "";

            using (StringWriter sww = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(sww))
            {
                xsSubmit.Serialize(writer, result);
                xml = sww.ToString();
            }

            XDocument doc = XDocument.Parse(xml);
            File.WriteAllText(filePath, doc.ToString());

            return result;
        }

        public void PopulateCompanySelective(ref Company company, HashSet<string> sections)
        {
            if (sections.Contains("Clients"))                   PopulateClients(ref company);
            if (sections.Contains("Shippers"))                  PopulateShippers(ref company);
            if (sections.Contains("AdapterRegistrations"))      PopulateAdapterRegistrations(ref company);
            if (sections.Contains("CarrierRoutes"))             PopulateCarrierRoutes(ref company);
            if (sections.Contains("AdapterRegistrations") || sections.Contains("CarrierRoutes"))
                PopulateAdapterDefinitionGuids(ref company);
            if (sections.Contains("DataConfigurationMappings")) PopulateDataConfigurationMappings(ref company);
            if (sections.Contains("DocumentConfigurations"))    PopulateDocumentConfigurations(ref company);
            if (sections.Contains("Machines"))                  PopulateMachines(ref company);
            if (sections.Contains("PrinterConfigurations"))     PopulatePrinterConfigurations(ref company);
            if (sections.Contains("PrinterDefinitions"))        PopulatePrinterDefinitions(ref company);
            if (sections.Contains("Profiles"))                  PopulateProfiles(ref company);
            if (sections.Contains("ScaleConfigurations"))       PopulateScaleConfigurations(ref company);
            if (sections.Contains("Schedules"))                 PopulateSchedules(ref company);
            if (sections.Contains("SourceConfigurations"))      PopulateSourceConfigurations(ref company);
            if (sections.Contains("Sites"))                     PopulateSites(ref company);
        }

        public void PopulateCompany(ref Company company)
        {
            PopulateClients(ref company);
            PopulateShippers(ref company);
            PopulateAdapterRegistrations(ref company);
            PopulateCarrierRoutes(ref company);
            PopulateAdapterDefinitionGuids(ref company);
            PopulateDataConfigurationMappings(ref company);
            PopulateDocumentConfigurations(ref company);
            PopulateMachines(ref company);
            PopulatePrinterConfigurations(ref company);
            PopulatePrinterDefinitions(ref company);
            PopulateProfiles(ref company);
            PopulateScaleConfigurations(ref company);
            PopulateSchedules(ref company);
            PopulateSourceConfigurations(ref company);
            PopulateSites(ref company);
        }

        public void PopulateClients(ref Company company)
        {
            ClientFetcher listFetcher = new ClientFetcher(_adminUrl, CompanyGuid, _accessToken);
            GetClientsResponse listResponse = listFetcher.Fetch();
            List<Client> shallow = listResponse?.Clients ?? new List<Client>();

            company.Clients = new List<Client>();
            foreach (Client item in shallow)
            {
                ClientDetailFetcher detailFetcher = new ClientDetailFetcher(_adminUrl, CompanyGuid, item.Id, _accessToken);
                GetClientResponse detailResponse = detailFetcher.Fetch();
                company.Clients.Add(detailResponse?.Client ?? item);
            }
        }

        public void PopulateShippers(ref Company company)
        {
            ShipperFetcher fetcher = new ShipperFetcher(_adminUrl, CompanyGuid, _accessToken);
            GetShippersResponse response = fetcher.Fetch();
            company.Shippers = response?.Shippers ?? new List<Shipper>();
        }

        public void PopulateAdapterRegistrations(ref Company company)
        {
            AdapterRegistrationFetcher listFetcher = new AdapterRegistrationFetcher(_adminUrl, CompanyGuid, _accessToken);
            GetAdapterRegistrationsResponse listResponse = listFetcher.Fetch();
            List<AdapterRegistration> shallow = listResponse?.AdapterRegistrations ?? new List<AdapterRegistration>();

            company.AdapterRegistrations = new List<AdapterRegistration>();
            foreach (AdapterRegistration item in shallow)
            {
                AdapterRegistrationDetailFetcher detailFetcher = new AdapterRegistrationDetailFetcher(_adminUrl, CompanyGuid, item.Id, _accessToken);
                GetAdapterRegistrationResponse detailResponse = detailFetcher.Fetch();
                if (detailResponse?.AdapterRegistration != null)
                    company.AdapterRegistrations.Add(detailResponse.AdapterRegistration);
                else
                    company.AdapterRegistrations.Add(item);
            }
        }

        public void PopulateAdapterDefinitionGuids(ref Company company)
        {
            AdapterDefinitionFetcher fetcher = new AdapterDefinitionFetcher(_adminUrl, CompanyGuid, _accessToken);
            GetAdapterDefinitionsResponse response = fetcher.Fetch();
            List<AdapterDefinition> definitions = response?.AdapterDefinitions ?? new List<AdapterDefinition>();

            foreach (AdapterRegistration registration in company.AdapterRegistrations)
                ApplyAdapterDefinitionGuid(registration, definitions);

            foreach (CarrierRoute route in company.CarrierRoutes)
                ApplyAdapterDefinitionGuid(route.AdapterRegistration, definitions);

            foreach (ServiceRoute route in company.ServiceRoutes ?? new List<ServiceRoute>())
                ApplyAdapterDefinitionGuid(route.AdapterRegistration, definitions);
        }

        private void ApplyAdapterDefinitionGuid(AdapterRegistration registration, List<AdapterDefinition> definitions)
        {
            if (registration?.AdapterDefinition == null)
                return;

            AdapterDefinition match = definitions.FirstOrDefault(d => d.Id == registration.AdapterDefinition.Id);
            if (match != null)
                registration.AdapterDefinition.Guid = match.Guid;
        }

        public void PopulateCarrierRoutes(ref Company company)
        {
            CarrierRouteFetcher listFetcher = new CarrierRouteFetcher(_adminUrl, CompanyGuid, _accessToken);
            GetCarrierRoutesResponse listResponse = listFetcher.Fetch();
            List<CarrierRoute> shallow = listResponse?.CarrierRoute ?? new List<CarrierRoute>();

            company.CarrierRoutes = new List<CarrierRoute>();
            foreach (CarrierRoute item in shallow)
            {
                CarrierRouteDetailFetcher detailFetcher = new CarrierRouteDetailFetcher(_adminUrl, CompanyGuid, item.Id, _accessToken);
                GetCarrierRouteResponse detailResponse = detailFetcher.Fetch();
                company.CarrierRoutes.Add(detailResponse?.CarrierRoute ?? item);
            }
        }

        public void PopulateDataConfigurationMappings(ref Company company)
        {
            DataConfigurationMappingFetcher listFetcher = new DataConfigurationMappingFetcher(_adminUrl, CompanyGuid, _accessToken);
            GetCompanyDataConfigurationMappingResponse listResponse = listFetcher.Fetch();
            List<DataConfigurationMapping> shallow = listResponse?.DataConfigurations ?? new List<DataConfigurationMapping>();

            company.DataConfigurationMappings = new List<DataConfigurationMapping>();
            foreach (DataConfigurationMapping item in shallow)
            {
                DataConfigurationMappingDetailFetcher detailFetcher = new DataConfigurationMappingDetailFetcher(_adminUrl, CompanyGuid, item.Id, _accessToken);
                GetDataConfigurationMappingResponse detailResponse = detailFetcher.Fetch();
                company.DataConfigurationMappings.Add(detailResponse?.DataConfigurations ?? item);
            }
        }

        public void PopulateDocumentConfigurations(ref Company company)
        {
            DocumentConfigurationFetcher listFetcher = new DocumentConfigurationFetcher(_adminUrl, CompanyGuid, _accessToken);
            GetDocumentConfigurationsResponse listResponse = listFetcher.Fetch();
            List<DocumentConfiguration> shallow = listResponse?.DocumentConfigurations ?? new List<DocumentConfiguration>();

            company.DocumentConfigurations = new List<DocumentConfiguration>();
            foreach (DocumentConfiguration item in shallow)
            {
                DocumentConfigurationDetailFetcher detailFetcher = new DocumentConfigurationDetailFetcher(_adminUrl, CompanyGuid, item.Id, _accessToken);
                GetDocumentConfigurationResponse detailResponse = detailFetcher.Fetch();
                company.DocumentConfigurations.Add(detailResponse?.DocumentConfiguration ?? item);
            }
        }

        public void PopulateMachines(ref Company company)
        {
            MachineFetcher listFetcher = new MachineFetcher(_adminUrl, CompanyGuid, _accessToken);
            GetMachinesResponse listResponse = listFetcher.Fetch();
            List<Machine> shallow = listResponse?.Machines ?? new List<Machine>();

            company.Machines = new List<Machine>();
            foreach (Machine item in shallow)
            {
                MachineDetailFetcher detailFetcher = new MachineDetailFetcher(_adminUrl, CompanyGuid, item.Id, _accessToken);
                GetMachineResponse detailResponse = detailFetcher.Fetch();
                company.Machines.Add(detailResponse?.Machine ?? item);
            }
        }

        public void PopulatePrinterConfigurations(ref Company company)
        {
            PrinterConfigurationFetcher listFetcher = new PrinterConfigurationFetcher(_adminUrl, CompanyGuid, _accessToken);
            GetPrinterConfigurationsResponse listResponse = listFetcher.Fetch();
            List<PrinterConfiguration> shallow = listResponse?.PrinterConfigurations ?? new List<PrinterConfiguration>();

            company.PrinterConfigurations = new List<PrinterConfiguration>();
            foreach (PrinterConfiguration item in shallow)
            {
                PrinterConfigurationDetailFetcher detailFetcher = new PrinterConfigurationDetailFetcher(_adminUrl, CompanyGuid, item.Id, _accessToken);
                GetPrinterConfigurationResponse detailResponse = detailFetcher.Fetch();
                company.PrinterConfigurations.Add(detailResponse?.PrinterConfiguration ?? item);
            }
        }

        public void PopulatePrinterDefinitions(ref Company company)
        {
            PrinterDefinitionFetcher listFetcher = new PrinterDefinitionFetcher(_adminUrl, CompanyGuid, _accessToken);
            GetPrinterDefinitionsResponse listResponse = listFetcher.Fetch();
            List<PrinterDefinition> shallow = listResponse?.PrinterDefinitions ?? new List<PrinterDefinition>();

            company.PrinterDefinitions = new List<PrinterDefinition>();
            foreach (PrinterDefinition item in shallow)
            {
                PrinterDefinitionDetailFetcher detailFetcher = new PrinterDefinitionDetailFetcher(_adminUrl, CompanyGuid, item.Id, _accessToken);
                GetPrinterDefinitionResponse detailResponse = detailFetcher.Fetch();
                company.PrinterDefinitions.Add(detailResponse?.PrinterDefinition ?? item);
            }
        }

        public void PopulateProfiles(ref Company company)
        {
            ProfileFetcher listFetcher = new ProfileFetcher(_adminUrl, CompanyGuid, _accessToken);
            GetProfilesResponse listResponse = listFetcher.Fetch();
            List<Profile> shallow = listResponse?.Profiles ?? new List<Profile>();

            company.Profiles = new List<Profile>();
            foreach (Profile item in shallow)
            {
                ProfileDetailFetcher detailFetcher = new ProfileDetailFetcher(_adminUrl, CompanyGuid, item.Id, _accessToken);
                GetProfileResponse detailResponse = detailFetcher.Fetch();
                Profile profile = detailResponse?.Profile ?? item;
                StripProfileShippersToIds(profile);
                company.Profiles.Add(profile);
            }
        }

        public void PopulateScaleConfigurations(ref Company company)
        {
            ScaleConfigurationFetcher fetcher = new ScaleConfigurationFetcher(_adminUrl, CompanyGuid, _accessToken);
            GetScaleConfigurationsResponse response = fetcher.Fetch();
            company.ScaleConfigurations = response?.ScaleConfigurations ?? new List<ScaleConfiguration>();
        }

        public void PopulateSchedules(ref Company company)
        {
            ScheduleFetcher fetcher = new ScheduleFetcher(_adminUrl, CompanyGuid, _accessToken);
            GetSchedulesResponse response = fetcher.Fetch();
            company.Schedules = response?.Schedules ?? new List<Schedule>();
        }

        public void PopulateSourceConfigurations(ref Company company)
        {
            SourceConfigurationFetcher fetcher = new SourceConfigurationFetcher(_adminUrl, CompanyGuid, _accessToken);
            GetCompanySourceConfigurationsResponse response = fetcher.Fetch();
            company.SourceConfigurations = response?.SourceConfigurations ?? new List<SourceConfiguration>();
        }

        public void PopulateSites(ref Company company)
        {
            SiteFetcher fetcher = new SiteFetcher(_adminUrl, company.Id, _accessToken);
            GetSitesResponse response = fetcher.Fetch();
            company.Sites = response?.Sites ?? new List<Site>();

            for (int i = 0; i < company.Sites.Count; i++)
            {
                Site site = company.Sites[i];
                PopulateSite(ref site);
                site.CompanyId = company.Id;

            }
        }

        public void PopulateSite(ref Site site)
        {
            PopulateSiteProfiles(ref site);
            PopulateSiteShippers(ref site);
            PopulateSiteClients(ref site);
            PopulateSiteMachines(ref site);
            PopulateSitePrinterConfigurations(ref site);
            PopulateSitePrinterDefinitions(ref site);
            PopulateSiteScaleDefinitions(ref site);
            PopulateSiteSchedules(ref site);
            PopulateSiteSourceConfigurations(ref site);
            PopulateSiteDataConfigurationMappings(ref site);
        }

        public void PopulateSiteProfiles(ref Site site)
        {
            SiteProfileFetcher listFetcher = new SiteProfileFetcher(_adminUrl, CompanyGuid, site.Id, _accessToken);
            GetProfilesResponse listResponse = listFetcher.Fetch();
            List<Profile> shallow = listResponse?.Profiles ?? new List<Profile>();

            site.Profiles = new List<Profile>();
            foreach (Profile item in shallow)
            {
                ProfileDetailFetcher detailFetcher = new ProfileDetailFetcher(_adminUrl, CompanyGuid, item.Id, _accessToken);
                GetProfileResponse detailResponse = detailFetcher.Fetch();
                Profile profile = detailResponse?.Profile ?? item;
                StripProfileShippersToIds(profile);
                site.Profiles.Add(profile);
            }
        }

        public void PopulateSiteShippers(ref Site site)
        {
            SiteShipperFetcher fetcher = new SiteShipperFetcher(_adminUrl, CompanyGuid, site.Id, _accessToken);
            GetShippersResponse response = fetcher.Fetch();
            site.Shippers = response?.Shippers ?? new List<Shipper>();
        }

        public void PopulateSiteClients(ref Site site)
        {
            SiteClientFetcher listFetcher = new SiteClientFetcher(_adminUrl, CompanyGuid, site.Id, _accessToken);
            GetClientsResponse listResponse = listFetcher.Fetch();
            List<Client> shallow = listResponse?.Clients ?? new List<Client>();

            site.Clients = new List<Client>();
            foreach (Client item in shallow)
            {
                ClientDetailFetcher detailFetcher = new ClientDetailFetcher(_adminUrl, CompanyGuid, item.Id, _accessToken);
                GetClientResponse detailResponse = detailFetcher.Fetch();
                site.Clients.Add(detailResponse?.Client ?? item);
            }
        }

        public void PopulateSiteMachines(ref Site site)
        {
            SiteMachineFetcher listFetcher = new SiteMachineFetcher(_adminUrl, CompanyGuid, site.Id, _accessToken);
            GetMachinesResponse listResponse = listFetcher.Fetch();
            List<Machine> shallow = listResponse?.Machines ?? new List<Machine>();

            site.Machines = new List<Machine>();
            foreach (Machine item in shallow)
            {
                MachineDetailFetcher detailFetcher = new MachineDetailFetcher(_adminUrl, CompanyGuid, item.Id, _accessToken);
                GetMachineResponse detailResponse = detailFetcher.Fetch();
                site.Machines.Add(detailResponse?.Machine ?? item);
            }
        }

        public void PopulateSitePrinterConfigurations(ref Site site)
        {
            SitePrinterConfigurationFetcher listFetcher = new SitePrinterConfigurationFetcher(_adminUrl, CompanyGuid, site.Id, _accessToken);
            GetPrinterConfigurationsResponse listResponse = listFetcher.Fetch();
            List<PrinterConfiguration> shallow = listResponse?.PrinterConfigurations ?? new List<PrinterConfiguration>();

            site.PrinterConfigurations = new List<PrinterConfiguration>();
            foreach (PrinterConfiguration item in shallow)
            {
                PrinterConfigurationDetailFetcher detailFetcher = new PrinterConfigurationDetailFetcher(_adminUrl, CompanyGuid, item.Id, _accessToken);
                GetPrinterConfigurationResponse detailResponse = detailFetcher.Fetch();
                site.PrinterConfigurations.Add(detailResponse?.PrinterConfiguration ?? item);
            }
        }

        public void PopulateSitePrinterDefinitions(ref Site site)
        {
            SitePrinterDefinitionFetcher listFetcher = new SitePrinterDefinitionFetcher(_adminUrl, CompanyGuid, site.Id, _accessToken);
            GetPrinterDefinitionsResponse listResponse = listFetcher.Fetch();
            List<PrinterDefinition> shallow = listResponse?.PrinterDefinitions ?? new List<PrinterDefinition>();

            site.PrinterDefinitions = new List<PrinterDefinition>();
            foreach (PrinterDefinition item in shallow)
            {
                PrinterDefinitionDetailFetcher detailFetcher = new PrinterDefinitionDetailFetcher(_adminUrl, CompanyGuid, item.Id, _accessToken);
                GetPrinterDefinitionResponse detailResponse = detailFetcher.Fetch();
                site.PrinterDefinitions.Add(detailResponse?.PrinterDefinition ?? item);
            }
        }

        public void PopulateSiteScaleDefinitions(ref Site site)
        {
            SiteScaleDefinitionFetcher fetcher = new SiteScaleDefinitionFetcher(_adminUrl, CompanyGuid, site.Id, _accessToken);
            GetScaleDefinitionsResponse response = fetcher.Fetch();
            site.ScaleDefinitions = response?.ScaleDefinitions ?? new List<ScaleDefinition>();
        }

        public void PopulateSiteSchedules(ref Site site)
        {
            SiteScheduleFetcher fetcher = new SiteScheduleFetcher(_adminUrl, CompanyGuid, site.Id, _accessToken);
            GetSchedulesResponse response = fetcher.Fetch();
            site.Schedules = response?.Schedules ?? new List<Schedule>();
        }

        public void PopulateSiteSourceConfigurations(ref Site site)
        {
            SiteSourceConfigurationFetcher fetcher = new SiteSourceConfigurationFetcher(_adminUrl, CompanyGuid, site.Id, _accessToken);
            GetSiteSourceConfigurationsResponse response = fetcher.Fetch();
            site.SourceConfigurations = response?.SourceConfigurations ?? new List<SourceConfiguration>();
        }

        /// <summary>
        /// Replaces the full Shipper objects on a Profile with lightweight stubs
        /// containing only the Id. The full shipper data lives at the Company level;
        /// profiles only need to reference shippers by Id.
        /// </summary>
        private static void StripProfileShippersToIds(Profile profile)
        {
            if (profile?.Shippers == null || profile.Shippers.Count == 0)
                return;

            profile.Shippers = profile.Shippers
                .Select(s => new Shipper { Id = s.Id })
                .ToList();
        }

        public void PopulateSiteDataConfigurationMappings(ref Site site)
        {
            SiteDataConfigurationMappingFetcher listFetcher = new SiteDataConfigurationMappingFetcher(_adminUrl, CompanyGuid, site.Id, _accessToken);
            GetSiteDataConfigurationsMappingResponse listResponse = listFetcher.Fetch();
            List<DataConfigurationMapping> shallow = listResponse?.DataConfigurations ?? new List<DataConfigurationMapping>();

            site.DataConfigurationMappings = new List<DataConfigurationMapping>();
            foreach (DataConfigurationMapping item in shallow)
            {
                DataConfigurationMappingDetailFetcher detailFetcher = new DataConfigurationMappingDetailFetcher(_adminUrl, CompanyGuid, item.Id, _accessToken);
                GetDataConfigurationMappingResponse detailResponse = detailFetcher.Fetch();
                site.DataConfigurationMappings.Add(detailResponse?.DataConfigurations ?? item);
            }
        }
    }
}
