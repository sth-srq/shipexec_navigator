using PSI.Sox.Wcf;
using PSI.Sox.Wcf.Administration;
using PSI.Sox.Data;
using System;

namespace ShipExecNavigator.BusinessLogic.RequestGeneration
{
    // -------------------------------------------------------------------------
    // Company-level entity fetchers
    // These populate properties that exist directly on PSI.Sox.Company.
    // -------------------------------------------------------------------------

    public class AdapterDefinitionFetcher : EntityFetcher<GetAdapterDefinitionsRequest, GetAdapterDefinitionsResponse>
    {
        public AdapterDefinitionFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetAdapterDefinitions") { }

        public override GetAdapterDefinitionsRequest ConfigureRequest(GetAdapterDefinitionsRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class AdapterRegistrationFetcher : EntityFetcher<GetAdapterRegistrationsRequest, GetAdapterRegistrationsResponse>
    {
        public AdapterRegistrationFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetAdapterRegistrations") { }

        public override GetAdapterRegistrationsRequest ConfigureRequest(GetAdapterRegistrationsRequest request)
        {
            request.CompanyId = CompanyId;
            request.SearchCriteria = GetEmptySearchCriteria();
            return request;
        }
    }

    public class AdapterRegistrationDetailFetcher : EntityFetcher<GetAdapterRegistrationRequest, GetAdapterRegistrationResponse>
    {
        private readonly int _adapterRegistrationId;

        public AdapterRegistrationDetailFetcher(string adminUrl, Guid companyId, int adapterRegistrationId, string jwt)
            : base(adminUrl, companyId, jwt, "GetAdapterRegistration")
        {
            _adapterRegistrationId = adapterRegistrationId;
        }

        public override GetAdapterRegistrationRequest ConfigureRequest(GetAdapterRegistrationRequest request)
        {
            request.CompanyId = CompanyId;
            request.AdapterRegistrationId = _adapterRegistrationId;
            return request;
        }
    }

    public class CarrierRouteFetcher : EntityFetcher<GetCarrierRoutesRequest, GetCarrierRoutesResponse>
    {
        public CarrierRouteFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetCarrierRoutes") { }

        public override GetCarrierRoutesRequest ConfigureRequest(GetCarrierRoutesRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class CarrierRouteDetailFetcher : EntityFetcher<GetCarrierRouteRequest, GetCarrierRouteResponse>
    {
        private readonly int _id;

        public CarrierRouteDetailFetcher(string adminUrl, Guid companyId, int id, string jwt)
            : base(adminUrl, companyId, jwt, "GetCarrierRoute")
        {
            _id = id;
        }

        public override GetCarrierRouteRequest ConfigureRequest(GetCarrierRouteRequest request)
        {
            request.CompanyId = CompanyId;
            request.Id = _id;
            return request;
        }
    }

    public class ClientFetcher : EntityFetcher<GetCompanyClientsRequest, GetClientsResponse>
    {
        public ClientFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetCompanyClients") { }

        public override GetCompanyClientsRequest ConfigureRequest(GetCompanyClientsRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class ClientDetailFetcher : EntityFetcher<GetClientRequest, GetClientResponse>
    {
        private readonly int _clientId;

        public ClientDetailFetcher(string adminUrl, Guid companyId, int clientId, string jwt)
            : base(adminUrl, companyId, jwt, "GetClient")
        {
            _clientId = clientId;
        }

        public override GetClientRequest ConfigureRequest(GetClientRequest request)
        {
            request.CompanyId = CompanyId;
            request.ClientId = _clientId;
            return request;
        }
    }

    public class DataConfigurationMappingFetcher : EntityFetcher<GetCompanyDataConfigurationMappingRequest, GetCompanyDataConfigurationMappingResponse>
    {
        public DataConfigurationMappingFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetCompanyDataConfigurationMapping") { }

        public override GetCompanyDataConfigurationMappingRequest ConfigureRequest(GetCompanyDataConfigurationMappingRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class DataConfigurationMappingDetailFetcher : EntityFetcher<GetDataConfigurationMappingRequest, GetDataConfigurationMappingResponse>
    {
        private readonly int _id;

        public DataConfigurationMappingDetailFetcher(string adminUrl, Guid companyId, int id, string jwt)
            : base(adminUrl, companyId, jwt, "GetDataConfigurationMapping")
        {
            _id = id;
        }

        public override GetDataConfigurationMappingRequest ConfigureRequest(GetDataConfigurationMappingRequest request)
        {
            request.CompanyId = CompanyId;
            request.Id = _id;
            return request;
        }
    }

    public class DocumentConfigurationFetcher : EntityFetcher<GetCompanyDocumentConfigurationsRequest, GetDocumentConfigurationsResponse>
    {
        public DocumentConfigurationFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetCompanyDocumentConfigurations") { }

        public override GetCompanyDocumentConfigurationsRequest ConfigureRequest(GetCompanyDocumentConfigurationsRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class DocumentConfigurationDetailFetcher : EntityFetcher<GetDocumentConfigurationRequest, GetDocumentConfigurationResponse>
    {
        private readonly int _documentConfigurationId;

        public DocumentConfigurationDetailFetcher(string adminUrl, Guid companyId, int documentConfigurationId, string jwt)
            : base(adminUrl, companyId, jwt, "GetDocumentConfiguration")
        {
            _documentConfigurationId = documentConfigurationId;
        }

        public override GetDocumentConfigurationRequest ConfigureRequest(GetDocumentConfigurationRequest request)
        {
            request.CompanyId = CompanyId;
            request.DocumentConfigurationId = _documentConfigurationId;
            return request;
        }
    }

    public class MachineFetcher : EntityFetcher<GetCompanyMachinesRequest, GetMachinesResponse>
    {
        public MachineFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetCompanyMachines") { }

        public override GetCompanyMachinesRequest ConfigureRequest(GetCompanyMachinesRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class MachineDetailFetcher : EntityFetcher<GetMachineRequest, GetMachineResponse>
    {
        private readonly int _machineId;

        public MachineDetailFetcher(string adminUrl, Guid companyId, int machineId, string jwt)
            : base(adminUrl, companyId, jwt, "GetMachine")
        {
            _machineId = machineId;
        }

        public override GetMachineRequest ConfigureRequest(GetMachineRequest request)
        {
            request.CompanyId = CompanyId;
            request.MachineId = _machineId;
            return request;
        }
    }

    public class PrinterConfigurationFetcher : EntityFetcher<GetCompanyPrinterConfigurationsRequest, GetPrinterConfigurationsResponse>
    {
        public PrinterConfigurationFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetCompanyPrinterConfigurations") { }

        public override GetCompanyPrinterConfigurationsRequest ConfigureRequest(GetCompanyPrinterConfigurationsRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class PrinterConfigurationDetailFetcher : EntityFetcher<GetPrinterConfigurationRequest, GetPrinterConfigurationResponse>
    {
        private readonly int _id;

        public PrinterConfigurationDetailFetcher(string adminUrl, Guid companyId, int id, string jwt)
            : base(adminUrl, companyId, jwt, "GetPrinterConfiguration")
        {
            _id = id;
        }

        public override GetPrinterConfigurationRequest ConfigureRequest(GetPrinterConfigurationRequest request)
        {
            request.CompanyId = CompanyId;
            request.Id = _id;
            return request;
        }
    }

    public class PrinterDefinitionFetcher : EntityFetcher<GetCompanyPrinterDefinitionsRequest, GetPrinterDefinitionsResponse>
    {
        public PrinterDefinitionFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetCompanyPrinterDefinitions") { }

        public override GetCompanyPrinterDefinitionsRequest ConfigureRequest(GetCompanyPrinterDefinitionsRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class PrinterDefinitionDetailFetcher : EntityFetcher<GetPrinterDefinitionRequest, GetPrinterDefinitionResponse>
    {
        private readonly int _printerDefinitionId;

        public PrinterDefinitionDetailFetcher(string adminUrl, Guid companyId, int printerDefinitionId, string jwt)
            : base(adminUrl, companyId, jwt, "GetPrinterDefinition")
        {
            _printerDefinitionId = printerDefinitionId;
        }

        public override GetPrinterDefinitionRequest ConfigureRequest(GetPrinterDefinitionRequest request)
        {
            request.CompanyId = CompanyId;
            request.PrinterDefinitionId = _printerDefinitionId;
            return request;
        }
    }

    public class ProfileFetcher : EntityFetcher<GetCompanyProfilesRequest, GetProfilesResponse>
    {
        public ProfileFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetCompanyProfiles") { }

        public override GetCompanyProfilesRequest ConfigureRequest(GetCompanyProfilesRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class ProfileDetailFetcher : EntityFetcher<GetProfileRequest, GetProfileResponse>
    {
        private readonly int _profileId;

        public ProfileDetailFetcher(string adminUrl, Guid companyId, int profileId, string jwt)
            : base(adminUrl, companyId, jwt, "GetProfile")
        {
            _profileId = profileId;
        }

        public override GetProfileRequest ConfigureRequest(GetProfileRequest request)
        {
            request.CompanyId = CompanyId;
            request.ProfileId = _profileId;
            return request;
        }
    }

    public class ScaleConfigurationFetcher : EntityFetcher<GetCompanyScaleConfigurationsRequest, GetScaleConfigurationsResponse>
    {
        public ScaleConfigurationFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetCompanyScaleConfigurations") { }

        public override GetCompanyScaleConfigurationsRequest ConfigureRequest(GetCompanyScaleConfigurationsRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class ScheduleFetcher : EntityFetcher<GetCompanySchedulesRequest, GetSchedulesResponse>
    {
        public ScheduleFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetCompanySchedules") { }

        public override GetCompanySchedulesRequest ConfigureRequest(GetCompanySchedulesRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class ShipperFetcher : EntityFetcher<GetShippersRequest, GetShippersResponse>
    {
        public ShipperFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetShippers") { }

        public override GetShippersRequest ConfigureRequest(GetShippersRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class SourceConfigurationFetcher : EntityFetcher<GetCompanySourceConfigurationsRequest, GetCompanySourceConfigurationsResponse>
    {
        public SourceConfigurationFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetCompanySourceConfigurations") { }

        public override GetCompanySourceConfigurationsRequest ConfigureRequest(GetCompanySourceConfigurationsRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    // -------------------------------------------------------------------------
    // Additional PSI.Sox.Wcf entity fetchers
    // These entities are not direct Company properties but are available at the
    // company scope and follow the same Get pattern.
    // -------------------------------------------------------------------------

    public class BoxTypeFetcher : EntityFetcher<GetCompanyBoxTypesRequest, GetBoxTypesResponse>
    {
        public BoxTypeFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetCompanyBoxTypes") { }

        public override GetCompanyBoxTypesRequest ConfigureRequest(GetCompanyBoxTypesRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class DatabaseDefinitionFetcher : EntityFetcher<GetDatabaseDefinitionsRequest, GetDatabaseDefinitionsResponse>
    {
        public DatabaseDefinitionFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetDatabaseDefinitions") { }

        public override GetDatabaseDefinitionsRequest ConfigureRequest(GetDatabaseDefinitionsRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class DistributionListFetcher : EntityFetcher<GetCompanyDistributionListsRequest, GetDistributionListsResponse>
    {
        public DistributionListFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetCompanyDistributionLists") { }

        public override GetCompanyDistributionListsRequest ConfigureRequest(GetCompanyDistributionListsRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class HazmatContentFetcher : EntityFetcher<GetCompanyHazmatContentsRequest, GetHazmatContentsResponse>
    {
        public HazmatContentFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetCompanyHazmatContents") { }

        public override GetCompanyHazmatContentsRequest ConfigureRequest(GetCompanyHazmatContentsRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class NotificationFetcher : EntityFetcher<GetCompanyNotificationsRequest, GetNotificationsResponse>
    {
        public NotificationFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetCompanyNotifications") { }

        public override GetCompanyNotificationsRequest ConfigureRequest(GetCompanyNotificationsRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class ReportFetcher : EntityFetcher<GetReportsRequest, GetReportsResponse>
    {
        public ReportFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetReports") { }

        public override GetReportsRequest ConfigureRequest(GetReportsRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class ServerBusinessRuleFetcher : EntityFetcher<GetServerBusinessRulesRequest, GetServerBusinessRulesResponse>
    {
        public ServerBusinessRuleFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetServerBusinessRules") { }

        public override GetServerBusinessRulesRequest ConfigureRequest(GetServerBusinessRulesRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class TemplateFetcher : EntityFetcher<GetCompanyTemplatesRequest, GetTemplatesResponse>
    {
        public TemplateFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetCompanyTemplates") { }

        public override GetCompanyTemplatesRequest ConfigureRequest(GetCompanyTemplatesRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    public class ValidationFetcher : EntityFetcher<GetCompanyValidationsRequest, GetValidationsResponse>
    {
        public ValidationFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetCompanyValidations") { }

        public override GetCompanyValidationsRequest ConfigureRequest(GetCompanyValidationsRequest request)
        {
            request.CompanyId = CompanyId;
            return request;
        }
    }

    // -------------------------------------------------------------------------
    // Site fetcher (company-scoped) — retrieves the list of sites for a company
    // -------------------------------------------------------------------------

    public class SiteFetcher : EntityFetcher<GetSitesRequest, GetSitesResponse>
    {
        public SiteFetcher(string adminUrl, Guid companyId, string jwt)
            : base(adminUrl, companyId, jwt, "GetSites") { }

        public override GetSitesRequest ConfigureRequest(GetSitesRequest request)
        {
            request.CompanyId = CompanyId;
            request.SearchCriteria = GetEmptySearchCriteria();
            return request;
        }
    }

    // -------------------------------------------------------------------------
    // Site-level entity fetchers
    // These populate properties that exist directly on PSI.Sox.Site.
    // -------------------------------------------------------------------------

    public class SiteProfileFetcher : SiteEntityFetcher<GetSiteProfilesRequest, GetProfilesResponse>
    {
        public SiteProfileFetcher(string adminUrl, Guid companyId, Guid siteId, string jwt)
            : base(adminUrl, companyId, siteId, jwt, "GetSiteProfiles") { }

        public override GetSiteProfilesRequest ConfigureRequest(GetSiteProfilesRequest request)
        {
            request.CompanyId = CompanyId;
            request.SiteId = SiteId;
            return request;
        }
    }

    public class SiteShipperFetcher : SiteEntityFetcher<GetSiteShippersRequest, GetShippersResponse>
    {
        public SiteShipperFetcher(string adminUrl, Guid companyId, Guid siteId, string jwt)
            : base(adminUrl, companyId, siteId, jwt, "GetSiteShippers") { }

        public override GetSiteShippersRequest ConfigureRequest(GetSiteShippersRequest request)
        {
            request.CompanyId = CompanyId;
            request.SiteId = SiteId;
            return request;
        }
    }

    public class SiteClientFetcher : SiteEntityFetcher<GetSiteClientsRequest, GetClientsResponse>
    {
        public SiteClientFetcher(string adminUrl, Guid companyId, Guid siteId, string jwt)
            : base(adminUrl, companyId, siteId, jwt, "GetSiteClients") { }

        public override GetSiteClientsRequest ConfigureRequest(GetSiteClientsRequest request)
        {
            request.CompanyId = CompanyId;
            request.SiteId = SiteId;
            return request;
        }
    }

    public class SiteMachineFetcher : SiteEntityFetcher<GetSiteMachinesRequest, GetMachinesResponse>
    {
        public SiteMachineFetcher(string adminUrl, Guid companyId, Guid siteId, string jwt)
            : base(adminUrl, companyId, siteId, jwt, "GetSiteMachines") { }

        public override GetSiteMachinesRequest ConfigureRequest(GetSiteMachinesRequest request)
        {
            request.CompanyId = CompanyId;
            request.SiteId = SiteId;
            return request;
        }
    }

    public class SitePrinterConfigurationFetcher : SiteEntityFetcher<GetSitePrinterConfigurationsRequest, GetPrinterConfigurationsResponse>
    {
        public SitePrinterConfigurationFetcher(string adminUrl, Guid companyId, Guid siteId, string jwt)
            : base(adminUrl, companyId, siteId, jwt, "GetSitePrinterConfigurations") { }

        public override GetSitePrinterConfigurationsRequest ConfigureRequest(GetSitePrinterConfigurationsRequest request)
        {
            request.CompanyId = CompanyId;
            request.SiteId = SiteId;
            return request;
        }
    }

    public class SitePrinterDefinitionFetcher : SiteEntityFetcher<GetSitePrinterDefinitionsRequest, GetPrinterDefinitionsResponse>
    {
        public SitePrinterDefinitionFetcher(string adminUrl, Guid companyId, Guid siteId, string jwt)
            : base(adminUrl, companyId, siteId, jwt, "GetSitePrinterDefinitions") { }

        public override GetSitePrinterDefinitionsRequest ConfigureRequest(GetSitePrinterDefinitionsRequest request)
        {
            request.CompanyId = CompanyId;
            request.SiteId = SiteId;
            return request;
        }
    }

    public class SiteScaleDefinitionFetcher : SiteEntityFetcher<GetSiteScaleDefinitionsRequest, GetScaleDefinitionsResponse>
    {
        public SiteScaleDefinitionFetcher(string adminUrl, Guid companyId, Guid siteId, string jwt)
            : base(adminUrl, companyId, siteId, jwt, "GetSiteScaleDefinitions") { }

        public override GetSiteScaleDefinitionsRequest ConfigureRequest(GetSiteScaleDefinitionsRequest request)
        {
            request.CompanyId = CompanyId;
            request.SiteId = SiteId;
            return request;
        }
    }

    public class SiteScheduleFetcher : SiteEntityFetcher<GetSiteSchedulesRequest, GetSchedulesResponse>
    {
        public SiteScheduleFetcher(string adminUrl, Guid companyId, Guid siteId, string jwt)
            : base(adminUrl, companyId, siteId, jwt, "GetSiteSchedules") { }

        public override GetSiteSchedulesRequest ConfigureRequest(GetSiteSchedulesRequest request)
        {
            request.CompanyId = CompanyId;
            request.SiteId = SiteId;
            return request;
        }
    }

    public class SiteSourceConfigurationFetcher : SiteEntityFetcher<GetSiteSourceConfigurationsRequest, GetSiteSourceConfigurationsResponse>
    {
        public SiteSourceConfigurationFetcher(string adminUrl, Guid companyId, Guid siteId, string jwt)
            : base(adminUrl, companyId, siteId, jwt, "GetSiteSourceConfigurations") { }

        public override GetSiteSourceConfigurationsRequest ConfigureRequest(GetSiteSourceConfigurationsRequest request)
        {
            request.CompanyId = CompanyId;
            request.SiteId = SiteId;
            return request;
        }
    }

    public class SiteDataConfigurationMappingFetcher : SiteEntityFetcher<GetSiteDataConfigurationMappingRequest, GetSiteDataConfigurationsMappingResponse>
    {
        public SiteDataConfigurationMappingFetcher(string adminUrl, Guid companyId, Guid siteId, string jwt)
            : base(adminUrl, companyId, siteId, jwt, "GetSiteDataConfigurationMapping") { }

        public override GetSiteDataConfigurationMappingRequest ConfigureRequest(GetSiteDataConfigurationMappingRequest request)
        {
            request.CompanyId = CompanyId;
            request.SiteId = SiteId;
            return request;
        }
    }
}
