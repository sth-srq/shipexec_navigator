using PSI.Sox;
using PSI.Sox.Wcf;
using PSI.Sox.Wcf.Administration;
using Microsoft.Extensions.Logging;
using ShipExecNavigator.BusinessLogic.EntityComparison;
using ShipExecNavigator.BusinessLogic.Logging;
using ShipExecNavigator.BusinessLogic.RequestGeneration;
using ShipExecNavigator.BusinessLogic.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ShipExecNavigator.BusinessLogic.CompanyBuilder
{
    /// <summary>
    /// Orchestrates entity-level diff detection and API request generation for a single company.
    /// <para>
    /// <see cref="CompanyBuilderManager"/> sits between <see cref="AppManager"/> and the
    /// individual per-entity request generators (e.g. <c>ShipperRequestGenerator</c>).
    /// Its two primary responsibilities are:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <term>Variance detection</term>
    ///     <description>
    ///       <see cref="GetVariances"/> compares two fully-hydrated <c>Company</c> object graphs
    ///       (before / after editing) across all entity collections — Shippers, Clients,
    ///       Profiles, Sites (including nested site-level entities), AdapterRegistrations,
    ///       CarrierRoutes, DataConfigurationMappings, DocumentConfigurations, Machines,
    ///       PrinterConfigurations, PrinterDefinitions, ScaleConfigurations, Schedules,
    ///       SourceConfigurations, and top-level company properties.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>Request generation &amp; application</term>
    ///     <description>
    ///       <see cref="GetRequests"/> converts each <see cref="Variance"/> into a typed
    ///       HTTP request payload (<see cref="RequestBaseWithURL"/>).
    ///       <see cref="ApplyRequests"/> executes those requests against the live
    ///       Management Studio API and returns per-request success/failure responses.
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// One <see cref="CompanyBuilderManager"/> instance is created per diff/apply cycle inside
    /// <see cref="AppManager.GetVariancesAndRequests"/> and <see cref="AppManager.ApplyChanges"/>.
    /// It is not reused across requests.
    /// </para>
    /// </summary>
    public class CompanyBuilderManager
    {
        private readonly ILogger<CompanyBuilderManager> _logger = LoggerProvider.CreateLogger<CompanyBuilderManager>();

        // ── Per-entity request generators ────────────────────────────────────────
        // Each generator encapsulates the CRUD endpoints for one PSI.Sox entity type.
        private ClientRequestGenerator _clientRequestGenerator { get; set; }
        private ShipperRequestGenerator _shipperRequestGenerator { get; set; }
        private AdapterRegistrationRequestGenerator _adapterRegistrationRequestGenerator { get; set; }
        private CarrierRouteRequestGenerator _carrierRouteRequestGenerator { get; set; }
        private DataConfigurationMappingRequestGenerator _dataConfigurationMappingRequestGenerator { get; set; }
        private DocumentConfigurationRequestGenerator _documentConfigurationRequestGenerator { get; set; }
        private MachineRequestGenerator _machineRequestGenerator { get; set; }
        private PrinterConfigurationRequestGenerator _printerConfigurationRequestGenerator { get; set; }
        private PrinterDefinitionRequestGenerator _printerDefinitionRequestGenerator { get; set; }
        private ProfileRequestGenerator _profileRequestGenerator { get; set; }
        private ScaleConfigurationRequestGenerator _scaleConfigurationRequestGenerator { get; set; }
        private ScheduleRequestGenerator _scheduleRequestGenerator { get; set; }
        private SourceConfigurationRequestGenerator _sourceConfigurationRequestGenerator { get; set; }
        private SiteRequestGenerator _siteRequestGenerator { get; set; }
        private CompanyRequestGenerator _companyRequestGenerator { get; set; }

        private Guid _companyGuid { get; set; }
        private string _jwt { get; set; }
        private string _adminUrl { get; set; }

        /// <summary>The in-progress company being built or compared.</summary>
        private Company _company { get; set; }

        public CompanyBuilderManager(String adminUrl, Guid companyGuid, string jwt)
        {

            _adminUrl = adminUrl;
            _companyGuid = companyGuid;
            _jwt = jwt;

            _company = new Company();

            _clientRequestGenerator = new ClientRequestGenerator(adminUrl, _companyGuid, _jwt);
            _shipperRequestGenerator = new ShipperRequestGenerator(adminUrl, _companyGuid, _jwt);
            _adapterRegistrationRequestGenerator = new AdapterRegistrationRequestGenerator(adminUrl, _companyGuid, _jwt);
            _carrierRouteRequestGenerator = new CarrierRouteRequestGenerator(adminUrl, _companyGuid, _jwt);
            _dataConfigurationMappingRequestGenerator = new DataConfigurationMappingRequestGenerator(adminUrl, _companyGuid, _jwt);
            _documentConfigurationRequestGenerator = new DocumentConfigurationRequestGenerator(adminUrl, _companyGuid, _jwt);
            _machineRequestGenerator = new MachineRequestGenerator(adminUrl, _companyGuid, _jwt);
            _printerConfigurationRequestGenerator = new PrinterConfigurationRequestGenerator(adminUrl, _companyGuid, _jwt);
            _printerDefinitionRequestGenerator = new PrinterDefinitionRequestGenerator(adminUrl, _companyGuid, _jwt);
            _profileRequestGenerator = new ProfileRequestGenerator(adminUrl, _companyGuid, _jwt);
            _scaleConfigurationRequestGenerator = new ScaleConfigurationRequestGenerator(adminUrl, _companyGuid, _jwt);
            _scheduleRequestGenerator = new ScheduleRequestGenerator(adminUrl, _companyGuid, _jwt);
            _sourceConfigurationRequestGenerator = new SourceConfigurationRequestGenerator(adminUrl, _companyGuid, _jwt);
            _siteRequestGenerator = new SiteRequestGenerator(adminUrl, _companyGuid, _jwt);
            _companyRequestGenerator = new CompanyRequestGenerator(adminUrl, _jwt);
        }

        public Company GetCompany()
        {
            return _company;
        }

        public List<Variance> GetVariances(Company existingCompany, Company modifiedCompany)
        {
            _logger.LogTrace(">> GetVariances");
            var result = new List<Variance>();

            result.AddRange(_clientRequestGenerator.GetVariances(existingCompany.Clients, modifiedCompany.Clients));
            result.AddRange(_shipperRequestGenerator.GetVariances(existingCompany.Shippers, modifiedCompany.Shippers));
            result.AddRange(_adapterRegistrationRequestGenerator.GetVariances(existingCompany.AdapterRegistrations, modifiedCompany.AdapterRegistrations));
            result.AddRange(_carrierRouteRequestGenerator.GetVariances(existingCompany.CarrierRoutes, modifiedCompany.CarrierRoutes));
            result.AddRange(_dataConfigurationMappingRequestGenerator.GetVariances(existingCompany.DataConfigurationMappings, modifiedCompany.DataConfigurationMappings));
            result.AddRange(_documentConfigurationRequestGenerator.GetVariances(existingCompany.DocumentConfigurations, modifiedCompany.DocumentConfigurations));
            result.AddRange(_machineRequestGenerator.GetVariances(existingCompany.Machines, modifiedCompany.Machines));
            result.AddRange(_printerConfigurationRequestGenerator.GetVariances(existingCompany.PrinterConfigurations, modifiedCompany.PrinterConfigurations));
            result.AddRange(_printerDefinitionRequestGenerator.GetVariances(existingCompany.PrinterDefinitions, modifiedCompany.PrinterDefinitions));
            result.AddRange(_profileRequestGenerator.GetVariances(existingCompany.Profiles, modifiedCompany.Profiles));
            result.AddRange(_scaleConfigurationRequestGenerator.GetVariances(existingCompany.ScaleConfigurations, modifiedCompany.ScaleConfigurations));
            result.AddRange(_scheduleRequestGenerator.GetVariances(existingCompany.Schedules, modifiedCompany.Schedules));
            result.AddRange(_sourceConfigurationRequestGenerator.GetVariances(existingCompany.SourceConfigurations, modifiedCompany.SourceConfigurations));

            result.AddRange(GetCompanyPropertyVariances(existingCompany, modifiedCompany));

            var siteVariances = _siteRequestGenerator.GetVariances(existingCompany.Sites, modifiedCompany.Sites);
            foreach (var siteVariance in siteVariances)
            {
                if (siteVariance.IsUpdated)
                {
                    var oldSite = (Site)siteVariance.OriginalObject;
                    var newSite = (Site)siteVariance.NewObject;
                    var context = "Site: " + oldSite.Name;
                    var siteId = oldSite.Id;

                    AddChildVariances(siteVariance, context, siteId, _machineRequestGenerator.GetVariances(
                        oldSite.Machines ?? new List<Machine>(), newSite.Machines ?? new List<Machine>()), result);
                    AddChildVariances(siteVariance, context, siteId, _shipperRequestGenerator.GetVariances(
                        oldSite.Shippers ?? new List<Shipper>(), newSite.Shippers ?? new List<Shipper>()), result);
                    AddChildVariances(siteVariance, context, siteId, _clientRequestGenerator.GetVariances(
                        oldSite.Clients ?? new List<Client>(), newSite.Clients ?? new List<Client>()), result);
                    AddChildVariances(siteVariance, context, siteId, _printerConfigurationRequestGenerator.GetVariances(
                        oldSite.PrinterConfigurations ?? new List<PrinterConfiguration>(), newSite.PrinterConfigurations ?? new List<PrinterConfiguration>()), result);
                    AddChildVariances(siteVariance, context, siteId, _printerDefinitionRequestGenerator.GetVariances(
                        oldSite.PrinterDefinitions ?? new List<PrinterDefinition>(), newSite.PrinterDefinitions ?? new List<PrinterDefinition>()), result);
                    AddChildVariances(siteVariance, context, siteId, _sourceConfigurationRequestGenerator.GetVariances(
                        oldSite.SourceConfigurations ?? new List<SourceConfiguration>(), newSite.SourceConfigurations ?? new List<SourceConfiguration>()), result);
                    AddChildVariances(siteVariance, context, siteId, _dataConfigurationMappingRequestGenerator.GetVariances(
                        oldSite.DataConfigurationMappings ?? new List<DataConfigurationMapping>(), newSite.DataConfigurationMappings ?? new List<DataConfigurationMapping>()), result);
                    AddChildVariances(siteVariance, context, siteId, _scheduleRequestGenerator.GetVariances(
                        oldSite.Schedules ?? new List<Schedule>(), newSite.Schedules ?? new List<Schedule>()), result);
                    AddChildVariances(siteVariance, context, siteId, _profileRequestGenerator.GetVariances(
                        oldSite.Profiles ?? new List<Profile>(), newSite.Profiles ?? new List<Profile>()), result);

                    // Only emit an UpdateSiteRequest when site-level properties changed.
                    // Child entity changes are handled by individual entity requests above.
                    if (HasSitePropertyChange(oldSite, newSite))
                    {
                        result.Add(siteVariance);
                    }
                }
                else
                {
                    // IsAdd or IsRemove — whole-site operations always go through
                    result.Add(siteVariance);
                }
            }

            _logger.LogTrace("<< GetVariances → {Count} variances", result.Count);
            return result;
        }


        public List<RequestBaseWithURL> GetRequests(Company existingCompany, List<Variance> variances)
        {
            _logger.LogTrace(">> GetRequests | VarianceCount={Count}", variances?.Count ?? 0);
            var result = new List<RequestBaseWithURL>();
            
            result.AddRange(_clientRequestGenerator.GetScripts(variances.Where(x => x.EntityName == "Client").ToList(), existingCompany.Clients));
            result.AddRange(_shipperRequestGenerator.GetScripts(variances.Where(x => x.EntityName == "Shipper").ToList(), existingCompany.Shippers));
            result.AddRange(_adapterRegistrationRequestGenerator.GetScripts(variances.Where(x => x.EntityName == "AdapterRegistration").ToList(), existingCompany.AdapterRegistrations));
            result.AddRange(_carrierRouteRequestGenerator.GetScripts(variances.Where(x => x.EntityName == "CarrierRoute").ToList(), existingCompany.CarrierRoutes));
            result.AddRange(_dataConfigurationMappingRequestGenerator.GetScripts(variances.Where(x => x.EntityName == "DataConfigurationMapping").ToList(), existingCompany.DataConfigurationMappings));
            result.AddRange(_documentConfigurationRequestGenerator.GetScripts(variances.Where(x => x.EntityName == "DocumentConfiguration").ToList(), existingCompany.DocumentConfigurations));
            result.AddRange(_machineRequestGenerator.GetScripts(variances.Where(x => x.EntityName == "Machine").ToList(), existingCompany.Machines));
            result.AddRange(_printerConfigurationRequestGenerator.GetScripts(variances.Where(x => x.EntityName == "PrinterConfiguration").ToList(), existingCompany.PrinterConfigurations));
            result.AddRange(_printerDefinitionRequestGenerator.GetScripts(variances.Where(x => x.EntityName == "PrinterDefinition").ToList(), existingCompany.PrinterDefinitions));
            result.AddRange(_profileRequestGenerator.GetScripts(variances.Where(x => x.EntityName == "Profile").ToList(), existingCompany.Profiles));
            result.AddRange(_scaleConfigurationRequestGenerator.GetScripts(variances.Where(x => x.EntityName == "ScaleConfiguration").ToList(), existingCompany.ScaleConfigurations));
            result.AddRange(_scheduleRequestGenerator.GetScripts(variances.Where(x => x.EntityName == "Schedule").ToList(), existingCompany.Schedules));
            result.AddRange(_sourceConfigurationRequestGenerator.GetScripts(variances.Where(x => x.EntityName == "SourceConfiguration").ToList(), existingCompany.SourceConfigurations));
            result.AddRange(_siteRequestGenerator.GetScripts(variances.Where(x => x.EntityName == "Site").ToList(), existingCompany.Sites));

            var companyVariances = variances.Where(x => x.EntityName == "Company" && x.IsUpdated).ToList();
            if (companyVariances.Any())
            {
                var updateRequest = new UpdateCompanyRequest
                {
                    Company = BuildScalarOnlyCompany(existingCompany),
                    EnterpriseId = existingCompany.EnterpriseId
                };
                result.Add(new RequestBaseWithURL
                {
                    Request = updateRequest,
                    Endpoint = _adminUrl + "UpdateCompany",
                    IsUpdated = true,
                    EntityName = "Company",
                    Variance = companyVariances.First()
                });
            }

            _logger.LogTrace("<< GetRequests → {Count} requests", result.Count);
            return result;
        }


        public List<ResponseBase> ApplyRequests(List<RequestBaseWithURL> requests)
        {
            _logger.LogTrace(">> ApplyRequests | RequestCount={Count}", requests?.Count ?? 0);
            var result = new List<ResponseBase>();


            ClientRequestGenerator clientRequestGenerator = new ClientRequestGenerator(_adminUrl, _companyGuid, _jwt);

            var clientRequests = requests.Where(x => x.EntityName.ToLowerInvariant() == "Client".ToLowerInvariant());

            foreach (var clientRequest in clientRequests)
            {
                if (clientRequest.IsUpdated)
                {
                    result.Add(_clientRequestGenerator.Update(clientRequest.Variance.NewObject as Client, clientRequest.Variance.ParentSiteId));
                }
                if (clientRequest.IsDelete)
                {
                    result.Add(_clientRequestGenerator.Remove((clientRequest.Variance.OriginalObject as Client).Id, clientRequest.Variance.ParentSiteId));
                }
                if (clientRequest.IsAdd)
                {
                    result.Add(_clientRequestGenerator.Add((clientRequest.Variance.NewObject as Client), clientRequest.Variance.ParentSiteId));
                }

            }


            var shipperRequests = requests.Where(x => x.EntityName.ToLowerInvariant() == "Shipper".ToLowerInvariant());

            foreach (var shipperRequest in shipperRequests)
            {
                if (shipperRequest.IsUpdated)
                {
                    result.Add(_shipperRequestGenerator.Update(shipperRequest.Variance.NewObject as Shipper, shipperRequest.Variance.ParentSiteId));
                }
                if (shipperRequest.IsDelete)
                {
                    result.Add(_shipperRequestGenerator.Remove((shipperRequest.Variance.OriginalObject as Shipper).Id, shipperRequest.Variance.ParentSiteId));
                }
                if (shipperRequest.IsAdd)
                {
                    result.Add(_shipperRequestGenerator.Add(shipperRequest.Variance.NewObject as Shipper));
                }

            }

            var adapterRegistrationRequests = requests.Where(x => x.EntityName.ToLowerInvariant() == "AdapterRegistration".ToLowerInvariant());

            foreach (var adapterRequest in adapterRegistrationRequests)
            {
                if (adapterRequest.IsUpdated)
                {
                    result.Add(_adapterRegistrationRequestGenerator.Update(adapterRequest.Variance.NewObject as AdapterRegistration));
                }
                if (adapterRequest.IsDelete)
                {
                    result.Add(_adapterRegistrationRequestGenerator.Remove((adapterRequest.Variance.OriginalObject as AdapterRegistration).Id));
                }
                if (adapterRequest.IsAdd)
                {
                    result.Add(_adapterRegistrationRequestGenerator.Add(adapterRequest.Variance.NewObject as AdapterRegistration));
                }

            }

            var carrierRouteRequests = requests.Where(x => x.EntityName.ToLowerInvariant() == "CarrierRoute".ToLowerInvariant());

            foreach (var carrierRouteRequest in carrierRouteRequests)
            {
                if (carrierRouteRequest.IsUpdated)
                {
                    result.Add(_carrierRouteRequestGenerator.Update(carrierRouteRequest.Variance.NewObject as CarrierRoute));
                }
                if (carrierRouteRequest.IsDelete)
                {
                    result.Add(_carrierRouteRequestGenerator.Remove((carrierRouteRequest.Variance.OriginalObject as CarrierRoute).Id));
                }
                if (carrierRouteRequest.IsAdd)
                {
                    result.Add(_carrierRouteRequestGenerator.Add(carrierRouteRequest.Variance.NewObject as CarrierRoute));
                }
            }

            var dataConfigurationMappingRequests = requests.Where(x => x.EntityName.ToLowerInvariant() == "DataConfigurationMapping".ToLowerInvariant());

            foreach (var dcmRequest in dataConfigurationMappingRequests)
            {
                if (dcmRequest.IsUpdated)
                {
                    result.Add(_dataConfigurationMappingRequestGenerator.Update(dcmRequest.Variance.NewObject as DataConfigurationMapping, dcmRequest.Variance.ParentSiteId));
                }
                if (dcmRequest.IsDelete)
                {
                    result.Add(_dataConfigurationMappingRequestGenerator.Remove((dcmRequest.Variance.OriginalObject as DataConfigurationMapping).Id));
                }
                if (dcmRequest.IsAdd)
                {
                    result.Add(_dataConfigurationMappingRequestGenerator.Add(dcmRequest.Variance.NewObject as DataConfigurationMapping, dcmRequest.Variance.ParentSiteId));
                }
            }

            var documentConfigurationRequests = requests.Where(x => x.EntityName.ToLowerInvariant() == "DocumentConfiguration".ToLowerInvariant());

            foreach (var docConfigRequest in documentConfigurationRequests)
            {
                if (docConfigRequest.IsUpdated)
                {
                    result.Add(_documentConfigurationRequestGenerator.Update(docConfigRequest.Variance.NewObject as DocumentConfiguration));
                }
                if (docConfigRequest.IsDelete)
                {
                    result.Add(_documentConfigurationRequestGenerator.Remove((docConfigRequest.Variance.OriginalObject as DocumentConfiguration).Id));
                }
                if (docConfigRequest.IsAdd)
                {
                    result.Add(_documentConfigurationRequestGenerator.Add(docConfigRequest.Variance.NewObject as DocumentConfiguration));
                }
            }

            var machineRequests = requests.Where(x => x.EntityName.ToLowerInvariant() == "Machine".ToLowerInvariant());

            foreach (var machineRequest in machineRequests)
            {
                if (machineRequest.IsUpdated)
                {
                    result.Add(_machineRequestGenerator.Update(machineRequest.Variance.NewObject as Machine, machineRequest.Variance.ParentSiteId));
                }
                if (machineRequest.IsDelete)
                {
                    result.Add(_machineRequestGenerator.Remove((machineRequest.Variance.OriginalObject as Machine).Id, machineRequest.Variance.ParentSiteId));
                }
                if (machineRequest.IsAdd)
                {
                    result.Add(_machineRequestGenerator.Add(machineRequest.Variance.NewObject as Machine, machineRequest.Variance.ParentSiteId));
                }
            }

            var printerConfigurationRequests = requests.Where(x => x.EntityName.ToLowerInvariant() == "PrinterConfiguration".ToLowerInvariant());

            foreach (var printerConfigRequest in printerConfigurationRequests)
            {
                if (printerConfigRequest.IsUpdated)
                {
                    result.Add(_printerConfigurationRequestGenerator.Update(printerConfigRequest.Variance.NewObject as PrinterConfiguration, printerConfigRequest.Variance.ParentSiteId));
                }
                if (printerConfigRequest.IsDelete)
                {
                    result.Add(_printerConfigurationRequestGenerator.Remove((printerConfigRequest.Variance.OriginalObject as PrinterConfiguration).Id, printerConfigRequest.Variance.ParentSiteId));
                }
                if (printerConfigRequest.IsAdd)
                {
                    result.Add(_printerConfigurationRequestGenerator.Add(printerConfigRequest.Variance.NewObject as PrinterConfiguration, printerConfigRequest.Variance.ParentSiteId));
                }
            }

            var printerDefinitionRequests = requests.Where(x => x.EntityName.ToLowerInvariant() == "PrinterDefinition".ToLowerInvariant());

            foreach (var printerDefRequest in printerDefinitionRequests)
            {
                if (printerDefRequest.IsUpdated)
                {
                    result.Add(_printerDefinitionRequestGenerator.Update(printerDefRequest.Variance.NewObject as PrinterDefinition, printerDefRequest.Variance.ParentSiteId));
                }
                if (printerDefRequest.IsDelete)
                {
                    result.Add(_printerDefinitionRequestGenerator.Remove((printerDefRequest.Variance.OriginalObject as PrinterDefinition).Id, printerDefRequest.Variance.ParentSiteId));
                }
                if (printerDefRequest.IsAdd)
                {
                    result.Add(_printerDefinitionRequestGenerator.Add(printerDefRequest.Variance.NewObject as PrinterDefinition, printerDefRequest.Variance.ParentSiteId));
                }
            }

            var profileRequests = requests.Where(x => x.EntityName.ToLowerInvariant() == "Profile".ToLowerInvariant());

            foreach (var profileRequest in profileRequests)
            {
                if (profileRequest.IsUpdated)
                {
                    result.Add(_profileRequestGenerator.Update(profileRequest.Variance.NewObject as Profile, profileRequest.Variance.ParentSiteId));
                }
                if (profileRequest.IsDelete)
                {
                    result.Add(_profileRequestGenerator.Remove((profileRequest.Variance.OriginalObject as Profile).Id, profileRequest.Variance.ParentSiteId));
                }
                if (profileRequest.IsAdd)
                {
                    result.Add(_profileRequestGenerator.Add(profileRequest.Variance.NewObject as Profile));
                }
            }

            var scaleConfigurationRequests = requests.Where(x => x.EntityName.ToLowerInvariant() == "ScaleConfiguration".ToLowerInvariant());

            foreach (var scaleConfigRequest in scaleConfigurationRequests)
            {
                if (scaleConfigRequest.IsUpdated)
                {
                    result.Add(_scaleConfigurationRequestGenerator.Update(scaleConfigRequest.Variance.NewObject as ScaleConfiguration));
                }
                if (scaleConfigRequest.IsDelete)
                {
                    result.Add(_scaleConfigurationRequestGenerator.Remove((scaleConfigRequest.Variance.OriginalObject as ScaleConfiguration).Id));
                }
                if (scaleConfigRequest.IsAdd)
                {
                    result.Add(_scaleConfigurationRequestGenerator.Add(scaleConfigRequest.Variance.NewObject as ScaleConfiguration));
                }
            }

            var scheduleRequests = requests.Where(x => x.EntityName.ToLowerInvariant() == "Schedule".ToLowerInvariant());

            foreach (var scheduleRequest in scheduleRequests)
            {
                if (scheduleRequest.IsUpdated)
                {
                    result.Add(_scheduleRequestGenerator.Update(scheduleRequest.Variance.NewObject as Schedule, scheduleRequest.Variance.ParentSiteId));
                }
                if (scheduleRequest.IsDelete)
                {
                    result.Add(_scheduleRequestGenerator.Remove((scheduleRequest.Variance.OriginalObject as Schedule).Id, scheduleRequest.Variance.ParentSiteId));
                }
                if (scheduleRequest.IsAdd)
                {
                    result.Add(_scheduleRequestGenerator.Add(scheduleRequest.Variance.NewObject as Schedule, scheduleRequest.Variance.ParentSiteId));
                }
            }

            var sourceConfigurationRequests = requests.Where(x => x.EntityName.ToLowerInvariant() == "SourceConfiguration".ToLowerInvariant());

            foreach (var sourceConfigRequest in sourceConfigurationRequests)
            {
                if (sourceConfigRequest.IsUpdated)
                {
                    result.Add(_sourceConfigurationRequestGenerator.Update(sourceConfigRequest.Variance.NewObject as SourceConfiguration, sourceConfigRequest.Variance.ParentSiteId));
                }
                if (sourceConfigRequest.IsDelete)
                {
                    result.Add(_sourceConfigurationRequestGenerator.Remove((sourceConfigRequest.Variance.OriginalObject as SourceConfiguration).Id, sourceConfigRequest.Variance.ParentSiteId));
                }
                if (sourceConfigRequest.IsAdd)
                {
                    result.Add(_sourceConfigurationRequestGenerator.Add(sourceConfigRequest.Variance.NewObject as SourceConfiguration, sourceConfigRequest.Variance.ParentSiteId));
                }
            }

            var siteRequests = requests.Where(x => x.EntityName.ToLowerInvariant() == "Site".ToLowerInvariant());

            foreach (var siteRequest in siteRequests)
            {
                if (siteRequest.IsUpdated)
                {
                    result.Add(_siteRequestGenerator.Update(siteRequest.Variance.NewObject as Site));
                }
                if (siteRequest.IsDelete)
                {
                    result.Add(_siteRequestGenerator.Remove((siteRequest.Variance.OriginalObject as Site).Id));
                }
                if (siteRequest.IsAdd)
                {
                    result.Add(_siteRequestGenerator.Add(siteRequest.Variance.NewObject as Site));
                }
            }

            var companyUpdateRequests = requests.Where(x => x.EntityName.Equals("Company", StringComparison.OrdinalIgnoreCase) && x.IsUpdated);
            foreach (var companyRequest in companyUpdateRequests)
            {
                if (companyRequest.Request is UpdateCompanyRequest ucr)
                    result.Add(_companyRequestGenerator.Update(ucr.Company));
            }

            _logger.LogTrace("<< ApplyRequests → {Count} responses", result.Count);
            return result;
        }

        /// <summary>
        /// Applies a single <see cref="Variance"/> to the live ShipExec server by routing
        /// it to the correct entity endpoint (e.g. UpdateShipper, RemoveClient, AddSite).
        /// When <see cref="Variance.NewObject"/> or <see cref="Variance.OriginalObject"/> are
        /// null (e.g. for historical variances loaded from storage), the typed entity is
        /// deserialized on-the-fly from <see cref="Variance.NewXML"/> / <see cref="Variance.OriginalXML"/>.
        /// </summary>
        public ResponseBase ApplyVariance(Variance variance)
        {
            switch ((variance.EntityName ?? string.Empty).ToLowerInvariant())
            {
                case "client":
                {
                    var newClient  = variance.NewObject      as Client ?? DeserializeXml<Client>(variance.NewXML);
                    var origClient = variance.OriginalObject as Client ?? DeserializeXml<Client>(variance.OriginalXML);
                    if (variance.IsUpdated) return _clientRequestGenerator.Update(newClient, variance.ParentSiteId);
                    if (variance.IsRemove)  return _clientRequestGenerator.Remove(origClient.Id, variance.ParentSiteId);
                    if (variance.IsAdd)     return _clientRequestGenerator.Add(newClient, variance.ParentSiteId);
                    break;
                }

                case "shipper":
                {
                    var newShipper  = variance.NewObject      as Shipper ?? DeserializeXml<Shipper>(variance.NewXML);
                    var origShipper = variance.OriginalObject as Shipper ?? DeserializeXml<Shipper>(variance.OriginalXML);
                    if (variance.IsUpdated) return _shipperRequestGenerator.Update(newShipper, variance.ParentSiteId);
                    if (variance.IsRemove)  return _shipperRequestGenerator.Remove(origShipper.Id, variance.ParentSiteId);
                    if (variance.IsAdd)     return _shipperRequestGenerator.Add(newShipper, variance.ParentSiteId);
                    break;
                }

                case "adapterregistration":
                {
                    var newAdapter  = variance.NewObject      as AdapterRegistration ?? DeserializeXml<AdapterRegistration>(variance.NewXML);
                    var origAdapter = variance.OriginalObject as AdapterRegistration ?? DeserializeXml<AdapterRegistration>(variance.OriginalXML);
                    if (variance.IsUpdated) return _adapterRegistrationRequestGenerator.Update(newAdapter);
                    if (variance.IsRemove)  return _adapterRegistrationRequestGenerator.Remove(origAdapter.Id);
                    if (variance.IsAdd)     return _adapterRegistrationRequestGenerator.Add(newAdapter);
                    break;
                }

                case "carrierroute":
                {
                    var newRoute  = variance.NewObject      as CarrierRoute ?? DeserializeXml<CarrierRoute>(variance.NewXML);
                    var origRoute = variance.OriginalObject as CarrierRoute ?? DeserializeXml<CarrierRoute>(variance.OriginalXML);
                    if (variance.IsUpdated) return _carrierRouteRequestGenerator.Update(newRoute);
                    if (variance.IsRemove)  return _carrierRouteRequestGenerator.Remove(origRoute.Id);
                    if (variance.IsAdd)     return _carrierRouteRequestGenerator.Add(newRoute);
                    break;
                }

                case "dataconfigurationmapping":
                {
                    var newDcm  = variance.NewObject      as DataConfigurationMapping ?? DeserializeXml<DataConfigurationMapping>(variance.NewXML);
                    var origDcm = variance.OriginalObject as DataConfigurationMapping ?? DeserializeXml<DataConfigurationMapping>(variance.OriginalXML);
                    if (variance.IsUpdated) return _dataConfigurationMappingRequestGenerator.Update(newDcm, variance.ParentSiteId);
                    if (variance.IsRemove)  return _dataConfigurationMappingRequestGenerator.Remove(origDcm.Id);
                    if (variance.IsAdd)     return _dataConfigurationMappingRequestGenerator.Add(newDcm, variance.ParentSiteId);
                    break;
                }

                case "documentconfiguration":
                {
                    var newDoc  = variance.NewObject      as DocumentConfiguration ?? DeserializeXml<DocumentConfiguration>(variance.NewXML);
                    var origDoc = variance.OriginalObject as DocumentConfiguration ?? DeserializeXml<DocumentConfiguration>(variance.OriginalXML);
                    if (variance.IsUpdated) return _documentConfigurationRequestGenerator.Update(newDoc);
                    if (variance.IsRemove)  return _documentConfigurationRequestGenerator.Remove(origDoc.Id);
                    if (variance.IsAdd)     return _documentConfigurationRequestGenerator.Add(newDoc);
                    break;
                }

                case "machine":
                {
                    var newMachine  = variance.NewObject      as Machine ?? DeserializeXml<Machine>(variance.NewXML);
                    var origMachine = variance.OriginalObject as Machine ?? DeserializeXml<Machine>(variance.OriginalXML);
                    if (variance.IsUpdated) return _machineRequestGenerator.Update(newMachine, variance.ParentSiteId);
                    if (variance.IsRemove)  return _machineRequestGenerator.Remove(origMachine.Id, variance.ParentSiteId);
                    if (variance.IsAdd)     return _machineRequestGenerator.Add(newMachine, variance.ParentSiteId);
                    break;
                }

                case "printerconfiguration":
                {
                    var newPrinterConfig  = variance.NewObject      as PrinterConfiguration ?? DeserializeXml<PrinterConfiguration>(variance.NewXML);
                    var origPrinterConfig = variance.OriginalObject as PrinterConfiguration ?? DeserializeXml<PrinterConfiguration>(variance.OriginalXML);
                    if (variance.IsUpdated) return _printerConfigurationRequestGenerator.Update(newPrinterConfig, variance.ParentSiteId);
                    if (variance.IsRemove)  return _printerConfigurationRequestGenerator.Remove(origPrinterConfig.Id, variance.ParentSiteId);
                    if (variance.IsAdd)     return _printerConfigurationRequestGenerator.Add(newPrinterConfig, variance.ParentSiteId);
                    break;
                }

                case "printerdefinition":
                {
                    var newPrinterDef  = variance.NewObject      as PrinterDefinition ?? DeserializeXml<PrinterDefinition>(variance.NewXML);
                    var origPrinterDef = variance.OriginalObject as PrinterDefinition ?? DeserializeXml<PrinterDefinition>(variance.OriginalXML);
                    if (variance.IsUpdated) return _printerDefinitionRequestGenerator.Update(newPrinterDef, variance.ParentSiteId);
                    if (variance.IsRemove)  return _printerDefinitionRequestGenerator.Remove(origPrinterDef.Id, variance.ParentSiteId);
                    if (variance.IsAdd)     return _printerDefinitionRequestGenerator.Add(newPrinterDef, variance.ParentSiteId);
                    break;
                }

                case "profile":
                {
                    var newProfile  = variance.NewObject      as Profile ?? DeserializeXml<Profile>(variance.NewXML);
                    var origProfile = variance.OriginalObject as Profile ?? DeserializeXml<Profile>(variance.OriginalXML);
                    if (variance.IsUpdated) return _profileRequestGenerator.Update(newProfile, variance.ParentSiteId);
                    if (variance.IsRemove)  return _profileRequestGenerator.Remove(origProfile.Id, variance.ParentSiteId);
                    if (variance.IsAdd)     return _profileRequestGenerator.Add(newProfile);
                    break;
                }

                case "scaleconfiguration":
                {
                    var newScaleConfig  = variance.NewObject      as ScaleConfiguration ?? DeserializeXml<ScaleConfiguration>(variance.NewXML);
                    var origScaleConfig = variance.OriginalObject as ScaleConfiguration ?? DeserializeXml<ScaleConfiguration>(variance.OriginalXML);
                    if (variance.IsUpdated) return _scaleConfigurationRequestGenerator.Update(newScaleConfig);
                    if (variance.IsRemove)  return _scaleConfigurationRequestGenerator.Remove(origScaleConfig.Id);
                    if (variance.IsAdd)     return _scaleConfigurationRequestGenerator.Add(newScaleConfig);
                    break;
                }

                case "schedule":
                {
                    var newSchedule  = variance.NewObject      as Schedule ?? DeserializeXml<Schedule>(variance.NewXML);
                    var origSchedule = variance.OriginalObject as Schedule ?? DeserializeXml<Schedule>(variance.OriginalXML);
                    if (variance.IsUpdated) return _scheduleRequestGenerator.Update(newSchedule, variance.ParentSiteId);
                    if (variance.IsRemove)  return _scheduleRequestGenerator.Remove(origSchedule.Id, variance.ParentSiteId);
                    if (variance.IsAdd)     return _scheduleRequestGenerator.Add(newSchedule, variance.ParentSiteId);
                    break;
                }

                case "sourceconfiguration":
                {
                    var newSourceConfig  = variance.NewObject      as SourceConfiguration ?? DeserializeXml<SourceConfiguration>(variance.NewXML);
                    var origSourceConfig = variance.OriginalObject as SourceConfiguration ?? DeserializeXml<SourceConfiguration>(variance.OriginalXML);
                    if (variance.IsUpdated) return _sourceConfigurationRequestGenerator.Update(newSourceConfig, variance.ParentSiteId);
                    if (variance.IsRemove)  return _sourceConfigurationRequestGenerator.Remove(origSourceConfig.Id, variance.ParentSiteId);
                    if (variance.IsAdd)     return _sourceConfigurationRequestGenerator.Add(newSourceConfig, variance.ParentSiteId);
                    break;
                }

                case "site":
                {
                    var newSite  = variance.NewObject      as Site ?? DeserializeXml<Site>(variance.NewXML);
                    var origSite = variance.OriginalObject as Site ?? DeserializeXml<Site>(variance.OriginalXML);
                    if (variance.IsUpdated) return _siteRequestGenerator.Update(newSite);
                    if (variance.IsRemove)  return _siteRequestGenerator.Remove(origSite.Id);
                    if (variance.IsAdd)     return _siteRequestGenerator.Add(newSite);
                    break;
                }

                case "company":
                    if (variance.IsUpdated && variance.NewObject is Company updatedCompany)
                        return _companyRequestGenerator.Update(BuildScalarOnlyCompany(updatedCompany));
                    break;
            }

            throw new NotSupportedException(
                $"No handler registered for entity '{variance.EntityName}'.");
        }

        /// <summary>
        /// Deserializes an XML string into an instance of <typeparamref name="T"/> using
        /// <see cref="System.Xml.Serialization.XmlSerializer"/>.  Returns <c>null</c> when
        /// <paramref name="xml"/> is null or whitespace.
        /// </summary>
        private static T DeserializeXml<T>(string xml) where T : class
        {
            if (string.IsNullOrWhiteSpace(xml)) return null;
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using (var reader = new System.IO.StringReader(xml))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        private static void AddChildVariances(Variance parent, string context, Guid siteId, List<Variance> children, List<Variance> topLevelResult)
        {
            foreach (var child in children)
            {
                child.ParentContext = context;
                child.ParentSiteId = siteId;
            }
            parent.ChildVariances.AddRange(children);
            topLevelResult.AddRange(children);
        }

        private static Company BuildScalarOnlyCompany(Company source) => new Company
        {
            Id = source.Id,
            Name = source.Name,
            Symbol = source.Symbol,
            LicenseId = source.LicenseId,
            Enabled = source.Enabled,
            ProfileId = source.ProfileId,
            RegistrationKey = source.RegistrationKey,
            EnterpriseId = source.EnterpriseId
        };

        private static List<Variance> GetCompanyPropertyVariances(Company existing, Company modified)
        {
            var result = new List<Variance>();
            foreach (var prop in typeof(Company).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var underlying = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                // Skip collections (List<T>, IEnumerable<T>, etc.) but keep Nullable<T> (int?, bool?, etc.)
                if (prop.PropertyType.IsGenericType && Nullable.GetUnderlyingType(prop.PropertyType) == null) continue;
                if (underlying.IsClass && underlying != typeof(string)) continue;
                if (underlying == typeof(Guid)) continue;
                if (prop.Name.Equals("Id", StringComparison.Ordinal)) continue;

                var origVal = prop.GetValue(existing);
                var modVal = prop.GetValue(modified);
                if (Equals(origVal, modVal)) continue;

                result.Add(new Variance
                {
                    EntityName = "Company",
                    IsUpdated = true,
                    OriginalObject = new Dictionary<string, object> { { prop.Name, origVal } },
                    NewObject = new Dictionary<string, object> { { prop.Name, modVal } }
                });
            }
            return result;
        }

        private static bool HasSitePropertyChange(Site original, Site modified)
        {
            return original.Name != modified.Name
                || original.ProfileId != modified.ProfileId
                || original.RegistrationKey != modified.RegistrationKey;
        }


    }

    public class RequestBaseWithURL
    {
        public RequestBase Request { get; set; }

        public string Endpoint { get; set; }

        public bool IsAdd { get; set; } = false;

        public bool IsDelete { get; set; } = false;

        public bool IsUpdated { get; set; } = false;

        public string EntityName { get; set; } = "";

        public Variance Variance { get; set; }
    }




    //public static class CompanyExtensions
    //{

    //    public static List<Variance> GetVariances(this Company existingCompanyConfiguration, Company modifiedCompanyConfiguration)
    //    {

    //        ClientRequestGenerator clientRequestGenerator = new ClientRequestGenerator(ad)

    //        var result = new List<Variance>();

    //        result.AddRange(existingCompanyConfiguration.Clients.GetVariances(modifiedCompanyConfiguration.Clients));

    //        //var test = Get

    //        return result;
    //    }

    //}
}
