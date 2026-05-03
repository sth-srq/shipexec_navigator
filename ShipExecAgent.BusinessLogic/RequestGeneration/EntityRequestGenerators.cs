using PSI.Sox;
using PSI.Sox.Wcf;
using PSI.Sox.Wcf.Administration;
using ShipExecAgent.BusinessLogic.EntityComparison;
using System;
using System.Collections.Generic;

namespace ShipExecAgent.BusinessLogic.RequestGeneration
{
    // ===========================================================================
    // CarrierRoute
    // ===========================================================================
    public class CarrierRouteRequestGenerator : RequestGenerationBase<
        GetCarrierRoutesResponse,
        GetCarrierRoutesRequest,
        GetCarrierRouteRequest,
        GetCarrierRouteResponse,
        AddCarrierRouteRequest,
        AddCarrierRouteResponse,
        UpdateCarrierRouteRequest,
        UpdateCarrierRouteResponse,
        RemoveCarrierRouteRequest,
        RemoveCarrierRouteResponse,
        CarrierRoute>
    {
        public CarrierRouteRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetCarrierRoutes", "GetCarrierRoute", "AddCarrierRoute", "UpdateCarrierRoute", "RemoveCarrierRoute", "CarrierRoute", jwt) { }

        public override GetCarrierRoutesRequest ModifyGetAllRequest(GetCarrierRoutesRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetCarrierRouteRequest ModifyGetRequestWithId(GetCarrierRouteRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.Id = id;
            return request;
        }

        public AddCarrierRouteResponse Add(CarrierRoute carrierRoute)
        {
            AddCarrierRouteRequest addRequest = new AddCarrierRouteRequest();
            addRequest.CarrierRoute = carrierRoute;
            addRequest.CompanyId = CompanyGuid;
            return BaseAdd(addRequest);
        }

        public AddCarrierRouteResponse Add(AddCarrierRouteRequest addRequest) => Add(addRequest.CarrierRoute);

        public UpdateCarrierRouteResponse Update(CarrierRoute carrierRoute)
        {
            UpdateCarrierRouteRequest updateRequest = new UpdateCarrierRouteRequest();
            updateRequest.CarrierRoute = carrierRoute;
            updateRequest.CompanyId = CompanyGuid;
            return BaseUpdate(updateRequest);
        }

        public UpdateCarrierRouteResponse Update(UpdateCarrierRouteRequest updateRequest) => Update(updateRequest.CarrierRoute);

        public RemoveCarrierRouteResponse Remove(int id)
        {
            RemoveCarrierRouteRequest removeRequest = new RemoveCarrierRouteRequest();
            removeRequest.Id = id;
            removeRequest.CompanyId = CompanyGuid;
            return BaseRemove(removeRequest);
        }

        public RemoveCarrierRouteResponse Remove(RemoveCarrierRouteRequest removeRequest) => Remove(removeRequest.Id);

        public override bool HasSameId(CarrierRoute current, CarrierRoute modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(CarrierRoute current, CarrierRoute modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddCarrierRouteRequest InitializeAddRequest(ref AddCarrierRouteRequest addRequest, Variance variance)
        {
            addRequest.CarrierRoute = (CarrierRoute)variance.NewObject;
            addRequest.CompanyId = CompanyGuid;
            return addRequest;
        }

        public override UpdateCarrierRouteRequest InitializeUpdateRequest(ref UpdateCarrierRouteRequest updateRequest, Variance variance)
        {
            updateRequest.CarrierRoute = (CarrierRoute)variance.NewObject;
            updateRequest.CompanyId = CompanyGuid;
            return updateRequest;
        }

        public override RemoveCarrierRouteRequest InitializeRemoveRequest(ref RemoveCarrierRouteRequest removeRequest, Variance variance)
        {
            removeRequest.Id = ((CarrierRoute)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            return removeRequest;
        }
    }

    // ===========================================================================
    // DataConfigurationMapping
    // Note: RemoveDataConfigurationMappingRequest is in PSI.Sox.Wcf (not Administration).
    //       AddDataConfigurationMappingRequest.dataConfiguration is a lowercase property.
    // ===========================================================================
    public class DataConfigurationMappingRequestGenerator : RequestGenerationBase<
        GetCompanyDataConfigurationMappingResponse,
        GetCompanyDataConfigurationMappingRequest,
        GetDataConfigurationMappingRequest,
        GetDataConfigurationMappingResponse,
        AddDataConfigurationMappingRequest,
        AddDataConfigurationMappingResponse,
        UpdateDataConfigurationMappingRequest,
        UpdateDataConfigurationMappingResponse,
        RemoveDataConfigurationMappingRequest,
        RemoveDataConfigurationMappingResponse,
        DataConfigurationMapping>
    {
        public DataConfigurationMappingRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetCompanyDataConfigurationMapping", "GetDataConfigurationMapping", "AddDataConfigurationMapping", "UpdateDataConfigurationMapping", "RemoveDataConfigurationMapping", "DataConfigurationMapping", jwt) { }

        public override GetCompanyDataConfigurationMappingRequest ModifyGetAllRequest(GetCompanyDataConfigurationMappingRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetDataConfigurationMappingRequest ModifyGetRequestWithId(GetDataConfigurationMappingRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.Id = id;
            return request;
        }

        public AddDataConfigurationMappingResponse Add(DataConfigurationMapping dataConfigurationMapping, Guid? siteId = null)
        {
            AddDataConfigurationMappingRequest addRequest = new AddDataConfigurationMappingRequest();
            addRequest.dataConfiguration = dataConfigurationMapping;
            addRequest.dataConfiguration.CompanyId = CompanyGuid;
            addRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) { addRequest.dataConfiguration.SiteId = siteId.Value; addRequest.SiteId = siteId.Value; }
            return BaseAdd(addRequest);
        }

        public AddDataConfigurationMappingResponse Add(AddDataConfigurationMappingRequest addRequest) => BaseAdd(addRequest);

        public UpdateDataConfigurationMappingResponse Update(DataConfigurationMapping dataConfigurationMapping, Guid? siteId = null)
        {
            UpdateDataConfigurationMappingRequest updateRequest = new UpdateDataConfigurationMappingRequest();
            updateRequest.DataConfigurationMapping = dataConfigurationMapping;
            updateRequest.DataConfigurationMapping.CompanyId = CompanyGuid;
            updateRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) { updateRequest.DataConfigurationMapping.SiteId = siteId.Value; updateRequest.SiteId = siteId.Value; }
            return BaseUpdate(updateRequest);
        }

        public UpdateDataConfigurationMappingResponse Update(UpdateDataConfigurationMappingRequest updateRequest) => Update(updateRequest.DataConfigurationMapping);

        public RemoveDataConfigurationMappingResponse Remove(int id)
        {
            RemoveDataConfigurationMappingRequest removeRequest = new RemoveDataConfigurationMappingRequest();
            removeRequest.Id = id;
            removeRequest.CompanyId = CompanyGuid;
            return BaseRemove(removeRequest);
        }

        public RemoveDataConfigurationMappingResponse Remove(RemoveDataConfigurationMappingRequest removeRequest) => Remove(removeRequest.Id);

        public override bool HasSameId(DataConfigurationMapping current, DataConfigurationMapping modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(DataConfigurationMapping current, DataConfigurationMapping modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddDataConfigurationMappingRequest InitializeAddRequest(ref AddDataConfigurationMappingRequest addRequest, Variance variance)
        {
            addRequest.dataConfiguration = (DataConfigurationMapping)variance.NewObject;
            addRequest.dataConfiguration.CompanyId = CompanyGuid;
            addRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) { addRequest.dataConfiguration.SiteId = variance.ParentSiteId.Value; addRequest.SiteId = variance.ParentSiteId.Value; }
            return addRequest;
        }

        public override UpdateDataConfigurationMappingRequest InitializeUpdateRequest(ref UpdateDataConfigurationMappingRequest updateRequest, Variance variance)
        {
            updateRequest.DataConfigurationMapping = (DataConfigurationMapping)variance.NewObject;
            updateRequest.DataConfigurationMapping.CompanyId = CompanyGuid;
            updateRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) { updateRequest.DataConfigurationMapping.SiteId = variance.ParentSiteId.Value; updateRequest.SiteId = variance.ParentSiteId.Value; }
            return updateRequest;
        }

        public override RemoveDataConfigurationMappingRequest InitializeRemoveRequest(ref RemoveDataConfigurationMappingRequest removeRequest, Variance variance)
        {
            removeRequest.Id = ((DataConfigurationMapping)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            return removeRequest;
        }
    }

    // ===========================================================================
    // DocumentConfiguration
    // ===========================================================================
    public class DocumentConfigurationRequestGenerator : RequestGenerationBase<
        GetDocumentConfigurationsResponse,
        GetCompanyDocumentConfigurationsRequest,
        GetDocumentConfigurationRequest,
        GetDocumentConfigurationResponse,
        AddDocumentConfigurationRequest,
        AddDocumentConfigurationResponse,
        UpdateDocumentConfigurationRequest,
        UpdateDocumentConfigurationResponse,
        RemoveDocumentConfigurationRequest,
        RemoveDocumentConfigurationResponse,
        DocumentConfiguration>
    {
        public DocumentConfigurationRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetCompanyDocumentConfigurations", "GetDocumentConfiguration", "AddDocumentConfiguration", "UpdateDocumentConfiguration", "RemoveDocumentConfiguration", "DocumentConfiguration", jwt) { }

        public override GetCompanyDocumentConfigurationsRequest ModifyGetAllRequest(GetCompanyDocumentConfigurationsRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetDocumentConfigurationRequest ModifyGetRequestWithId(GetDocumentConfigurationRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.DocumentConfigurationId = id;
            return request;
        }

        public AddDocumentConfigurationResponse Add(DocumentConfiguration documentConfiguration)
        {
            AddDocumentConfigurationRequest addRequest = new AddDocumentConfigurationRequest();
            addRequest.DocumentConfiguration = documentConfiguration;
            addRequest.DocumentConfiguration.CompanyId = CompanyGuid;
            return BaseAdd(addRequest);
        }

        public AddDocumentConfigurationResponse Add(AddDocumentConfigurationRequest addRequest) => Add(addRequest.DocumentConfiguration);

        public UpdateDocumentConfigurationResponse Update(DocumentConfiguration documentConfiguration)
        {
            UpdateDocumentConfigurationRequest updateRequest = new UpdateDocumentConfigurationRequest();
            updateRequest.DocumentConfiguration = documentConfiguration;
            updateRequest.CompanyId = CompanyGuid;
            return BaseUpdate(updateRequest);
        }

        public UpdateDocumentConfigurationResponse Update(UpdateDocumentConfigurationRequest updateRequest) => Update(updateRequest.DocumentConfiguration);

        public RemoveDocumentConfigurationResponse Remove(int id)
        {
            RemoveDocumentConfigurationRequest removeRequest = new RemoveDocumentConfigurationRequest();
            removeRequest.DocumentConfigurationId = id;
            removeRequest.CompanyId = CompanyGuid;
            return BaseRemove(removeRequest);
        }

        public RemoveDocumentConfigurationResponse Remove(RemoveDocumentConfigurationRequest removeRequest) => Remove(removeRequest.DocumentConfigurationId);

        public override bool HasSameId(DocumentConfiguration current, DocumentConfiguration modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(DocumentConfiguration current, DocumentConfiguration modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddDocumentConfigurationRequest InitializeAddRequest(ref AddDocumentConfigurationRequest addRequest, Variance variance)
        {
            addRequest.DocumentConfiguration = (DocumentConfiguration)variance.NewObject;
            addRequest.DocumentConfiguration.CompanyId = CompanyGuid;
            return addRequest;
        }

        public override UpdateDocumentConfigurationRequest InitializeUpdateRequest(ref UpdateDocumentConfigurationRequest updateRequest, Variance variance)
        {
            updateRequest.DocumentConfiguration = (DocumentConfiguration)variance.NewObject;
            updateRequest.CompanyId = CompanyGuid;
            return updateRequest;
        }

        public override RemoveDocumentConfigurationRequest InitializeRemoveRequest(ref RemoveDocumentConfigurationRequest removeRequest, Variance variance)
        {
            removeRequest.DocumentConfigurationId = ((DocumentConfiguration)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            return removeRequest;
        }
    }

    // ===========================================================================
    // Machine
    // ===========================================================================
    public class MachineRequestGenerator : RequestGenerationBase<
        GetMachinesResponse,
        GetCompanyMachinesRequest,
        GetMachineRequest,
        GetMachineResponse,
        AddMachineRequest,
        AddMachineResponse,
        UpdateMachineRequest,
        UpdateMachineResponse,
        RemoveMachineRequest,
        RemoveMachineResponse,
        Machine>
    {
        public MachineRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetCompanyMachines", "GetMachine", "AddMachine", "UpdateMachine", "RemoveMachine", "Machine", jwt) { }

        public override GetCompanyMachinesRequest ModifyGetAllRequest(GetCompanyMachinesRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetMachineRequest ModifyGetRequestWithId(GetMachineRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.MachineId = id;
            return request;
        }

        public AddMachineResponse Add(Machine machine, Guid? siteId = null)
        {
            AddMachineRequest addRequest = new AddMachineRequest();
            addRequest.Machine = machine;
            addRequest.Machine.CompanyId = CompanyGuid;
            addRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) { addRequest.Machine.SiteId = siteId.Value; addRequest.SiteId = siteId.Value; }
            return BaseAdd(addRequest);
        }

        public AddMachineResponse Add(AddMachineRequest addRequest) => Add(addRequest.Machine);

        public UpdateMachineResponse Update(Machine machine, Guid? siteId = null)
        {
            UpdateMachineRequest updateRequest = new UpdateMachineRequest();
            updateRequest.Machine = machine;
            updateRequest.Machine.CompanyId = CompanyGuid;
            updateRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) { updateRequest.Machine.SiteId = siteId.Value; updateRequest.SiteId = siteId.Value; }
            return BaseUpdate(updateRequest);
        }

        public UpdateMachineResponse Update(UpdateMachineRequest updateRequest) => Update(updateRequest.Machine);

        public RemoveMachineResponse Remove(int id, Guid? siteId = null)
        {
            RemoveMachineRequest removeRequest = new RemoveMachineRequest();
            removeRequest.MachineId = id;
            removeRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) removeRequest.SiteId = siteId.Value;
            return BaseRemove(removeRequest);
        }

        public RemoveMachineResponse Remove(RemoveMachineRequest removeRequest) => Remove(removeRequest.MachineId);

        public override bool HasSameId(Machine current, Machine modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(Machine current, Machine modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddMachineRequest InitializeAddRequest(ref AddMachineRequest addRequest, Variance variance)
        {
            addRequest.Machine = (Machine)variance.NewObject;
            addRequest.Machine.CompanyId = CompanyGuid;
            addRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) { addRequest.Machine.SiteId = variance.ParentSiteId.Value; addRequest.SiteId = variance.ParentSiteId.Value; }
            return addRequest;
        }

        public override UpdateMachineRequest InitializeUpdateRequest(ref UpdateMachineRequest updateRequest, Variance variance)
        {
            updateRequest.Machine = (Machine)variance.NewObject;
            updateRequest.Machine.CompanyId = CompanyGuid;
            updateRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) { updateRequest.Machine.SiteId = variance.ParentSiteId.Value; updateRequest.SiteId = variance.ParentSiteId.Value; }
            return updateRequest;
        }

        public override RemoveMachineRequest InitializeRemoveRequest(ref RemoveMachineRequest removeRequest, Variance variance)
        {
            removeRequest.MachineId = ((Machine)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) removeRequest.SiteId = variance.ParentSiteId.Value;
            return removeRequest;
        }
    }

    // ===========================================================================
    // PrinterConfiguration
    // Note: GetPrinterConfigurationRequest uses Id; RemovePrinterConfigurationRequest uses PrinterConfigurationId.
    // ===========================================================================
    public class PrinterConfigurationRequestGenerator : RequestGenerationBase<
        GetPrinterConfigurationsResponse,
        GetCompanyPrinterConfigurationsRequest,
        GetPrinterConfigurationRequest,
        GetPrinterConfigurationResponse,
        AddPrinterConfigurationRequest,
        AddPrinterConfigurationResponse,
        UpdatePrinterConfigurationRequest,
        UpdatePrinterConfigurationResponse,
        RemovePrinterConfigurationRequest,
        RemovePrinterConfigurationResponse,
        PrinterConfiguration>
    {
        public PrinterConfigurationRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetCompanyPrinterConfigurations", "GetPrinterConfiguration", "AddPrinterConfiguration", "UpdatePrinterConfiguration", "RemovePrinterConfiguration", "PrinterConfiguration", jwt) { }

        public override GetCompanyPrinterConfigurationsRequest ModifyGetAllRequest(GetCompanyPrinterConfigurationsRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetPrinterConfigurationRequest ModifyGetRequestWithId(GetPrinterConfigurationRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.Id = id;
            return request;
        }

        public AddPrinterConfigurationResponse Add(PrinterConfiguration printerConfiguration, Guid? siteId = null)
        {
            AddPrinterConfigurationRequest addRequest = new AddPrinterConfigurationRequest();
            addRequest.PrinterConfiguration = printerConfiguration;
            addRequest.PrinterConfiguration.CompanyId = CompanyGuid;
            addRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) { addRequest.PrinterConfiguration.SiteId = siteId.Value; addRequest.SiteId = siteId.Value; }
            return BaseAdd(addRequest);
        }

        public AddPrinterConfigurationResponse Add(AddPrinterConfigurationRequest addRequest) => Add(addRequest.PrinterConfiguration);

        public UpdatePrinterConfigurationResponse Update(PrinterConfiguration printerConfiguration, Guid? siteId = null)
        {
            UpdatePrinterConfigurationRequest updateRequest = new UpdatePrinterConfigurationRequest();
            updateRequest.PrinterConfiguration = printerConfiguration;
            updateRequest.PrinterConfiguration.CompanyId = CompanyGuid;
            updateRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) { updateRequest.PrinterConfiguration.SiteId = siteId.Value; updateRequest.SiteId = siteId.Value; }
            return BaseUpdate(updateRequest);
        }

        public UpdatePrinterConfigurationResponse Update(UpdatePrinterConfigurationRequest updateRequest) => Update(updateRequest.PrinterConfiguration);

        public RemovePrinterConfigurationResponse Remove(int id, Guid? siteId = null)
        {
            RemovePrinterConfigurationRequest removeRequest = new RemovePrinterConfigurationRequest();
            removeRequest.PrinterConfigurationId = id;
            removeRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) removeRequest.SiteId = siteId.Value;
            return BaseRemove(removeRequest);
        }

        public RemovePrinterConfigurationResponse Remove(RemovePrinterConfigurationRequest removeRequest) => Remove(removeRequest.PrinterConfigurationId);

        public override bool HasSameId(PrinterConfiguration current, PrinterConfiguration modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(PrinterConfiguration current, PrinterConfiguration modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddPrinterConfigurationRequest InitializeAddRequest(ref AddPrinterConfigurationRequest addRequest, Variance variance)
        {
            addRequest.PrinterConfiguration = (PrinterConfiguration)variance.NewObject;
            addRequest.PrinterConfiguration.CompanyId = CompanyGuid;
            addRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) { addRequest.PrinterConfiguration.SiteId = variance.ParentSiteId.Value; addRequest.SiteId = variance.ParentSiteId.Value; }
            return addRequest;
        }

        public override UpdatePrinterConfigurationRequest InitializeUpdateRequest(ref UpdatePrinterConfigurationRequest updateRequest, Variance variance)
        {
            updateRequest.PrinterConfiguration = (PrinterConfiguration)variance.NewObject;
            updateRequest.PrinterConfiguration.CompanyId = CompanyGuid;
            updateRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) { updateRequest.PrinterConfiguration.SiteId = variance.ParentSiteId.Value; updateRequest.SiteId = variance.ParentSiteId.Value; }
            return updateRequest;
        }

        public override RemovePrinterConfigurationRequest InitializeRemoveRequest(ref RemovePrinterConfigurationRequest removeRequest, Variance variance)
        {
            removeRequest.PrinterConfigurationId = ((PrinterConfiguration)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) removeRequest.SiteId = variance.ParentSiteId.Value;
            return removeRequest;
        }
    }

    // ===========================================================================
    // PrinterDefinition
    // ===========================================================================
    public class PrinterDefinitionRequestGenerator : RequestGenerationBase<
        GetPrinterDefinitionsResponse,
        GetCompanyPrinterDefinitionsRequest,
        GetPrinterDefinitionRequest,
        GetPrinterDefinitionResponse,
        AddPrinterDefinitionRequest,
        AddPrinterDefinitionResponse,
        UpdatePrinterDefinitionRequest,
        UpdatePrinterDefinitionResponse,
        RemovePrinterDefinitionRequest,
        RemovePrinterDefinitionResponse,
        PrinterDefinition>
    {
        public PrinterDefinitionRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetCompanyPrinterDefinitions", "GetPrinterDefinition", "AddPrinterDefinition", "UpdatePrinterDefinition", "RemovePrinterDefinition", "PrinterDefinition", jwt) { }

        public override GetCompanyPrinterDefinitionsRequest ModifyGetAllRequest(GetCompanyPrinterDefinitionsRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetPrinterDefinitionRequest ModifyGetRequestWithId(GetPrinterDefinitionRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.PrinterDefinitionId = id;
            return request;
        }

        public AddPrinterDefinitionResponse Add(PrinterDefinition printerDefinition, Guid? siteId = null)
        {
            AddPrinterDefinitionRequest addRequest = new AddPrinterDefinitionRequest();
            addRequest.PrinterDefinition = printerDefinition;
            addRequest.PrinterDefinition.CompanyId = CompanyGuid;
            if (siteId.HasValue) addRequest.PrinterDefinition.SiteId = siteId.Value;
            return BaseAdd(addRequest);
        }

        public AddPrinterDefinitionResponse Add(AddPrinterDefinitionRequest addRequest) => Add(addRequest.PrinterDefinition);

        public UpdatePrinterDefinitionResponse Update(PrinterDefinition printerDefinition, Guid? siteId = null)
        {
            UpdatePrinterDefinitionRequest updateRequest = new UpdatePrinterDefinitionRequest();
            updateRequest.PrinterDefinition = printerDefinition;
            updateRequest.PrinterDefinition.CompanyId = CompanyGuid;
            if (siteId.HasValue) updateRequest.PrinterDefinition.SiteId = siteId.Value;
            return BaseUpdate(updateRequest);
        }

        public UpdatePrinterDefinitionResponse Update(UpdatePrinterDefinitionRequest updateRequest) => Update(updateRequest.PrinterDefinition);

        public RemovePrinterDefinitionResponse Remove(int id, Guid? siteId = null)
        {
            RemovePrinterDefinitionRequest removeRequest = new RemovePrinterDefinitionRequest();
            removeRequest.PrinterDefinitionId = id;
            removeRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) removeRequest.SiteId = siteId.Value;
            return BaseRemove(removeRequest);
        }

        public RemovePrinterDefinitionResponse Remove(RemovePrinterDefinitionRequest removeRequest) => Remove(removeRequest.PrinterDefinitionId);

        public override bool HasSameId(PrinterDefinition current, PrinterDefinition modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(PrinterDefinition current, PrinterDefinition modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddPrinterDefinitionRequest InitializeAddRequest(ref AddPrinterDefinitionRequest addRequest, Variance variance)
        {
            addRequest.PrinterDefinition = (PrinterDefinition)variance.NewObject;
            addRequest.PrinterDefinition.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) addRequest.PrinterDefinition.SiteId = variance.ParentSiteId.Value;
            return addRequest;
        }

        public override UpdatePrinterDefinitionRequest InitializeUpdateRequest(ref UpdatePrinterDefinitionRequest updateRequest, Variance variance)
        {
            updateRequest.PrinterDefinition = (PrinterDefinition)variance.NewObject;
            updateRequest.PrinterDefinition.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) updateRequest.PrinterDefinition.SiteId = variance.ParentSiteId.Value;
            return updateRequest;
        }

        public override RemovePrinterDefinitionRequest InitializeRemoveRequest(ref RemovePrinterDefinitionRequest removeRequest, Variance variance)
        {
            removeRequest.PrinterDefinitionId = ((PrinterDefinition)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) removeRequest.SiteId = variance.ParentSiteId.Value;
            return removeRequest;
        }
    }

    // ===========================================================================
    // Profile
    // Note: No AddProfileRequest exists. AddCompanyProfileRequest takes Name + CompanyId
    //       instead of a Profile entity; use profile.Name to populate it.
    // ===========================================================================
    public class ProfileRequestGenerator : RequestGenerationBase<
        GetProfilesResponse,
        GetCompanyProfilesRequest,
        GetProfileRequest,
        GetProfileResponse,
        AddCompanyProfileRequest,
        AddProfileResponse,
        UpdateProfileRequest,
        UpdateProfileResponse,
        RemoveProfileRequest,
        RemoveProfileResponse,
        Profile>
    {
        public ProfileRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetCompanyProfiles", "GetProfile", "AddCompanyProfile", "UpdateProfile", "RemoveProfile", "Profile", jwt) { }

        public override GetCompanyProfilesRequest ModifyGetAllRequest(GetCompanyProfilesRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetProfileRequest ModifyGetRequestWithId(GetProfileRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.ProfileId = id;
            return request;
        }

        public AddProfileResponse Add(Profile profile)
        {
            AddCompanyProfileRequest addRequest = new AddCompanyProfileRequest();
            addRequest.Name = profile.Name;
            addRequest.CompanyId = CompanyGuid;
            return BaseAdd(addRequest);
        }

        public AddProfileResponse Add(AddCompanyProfileRequest addRequest) => BaseAdd(addRequest);

        public UpdateProfileResponse Update(Profile profile, Guid? siteId = null)
        {
            UpdateProfileRequest updateRequest = new UpdateProfileRequest();
            updateRequest.Profile = profile;
            updateRequest.Profile.CompanyId = CompanyGuid;
            updateRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) { updateRequest.Profile.SiteId = siteId.Value; updateRequest.SiteId = siteId.Value; }
            return BaseUpdate(updateRequest);
        }

        public UpdateProfileResponse Update(UpdateProfileRequest updateRequest) => Update(updateRequest.Profile);

        public RemoveProfileResponse Remove(int id, Guid? siteId = null)
        {
            RemoveProfileRequest removeRequest = new RemoveProfileRequest();
            removeRequest.ProfileId = id;
            removeRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) removeRequest.SiteId = siteId.Value;
            return BaseRemove(removeRequest);
        }

        public RemoveProfileResponse Remove(RemoveProfileRequest removeRequest) => Remove(removeRequest.ProfileId);

        public override bool HasSameId(Profile current, Profile modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(Profile current, Profile modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddCompanyProfileRequest InitializeAddRequest(ref AddCompanyProfileRequest addRequest, Variance variance)
        {
            addRequest.Name = ((Profile)variance.NewObject).Name;
            addRequest.CompanyId = CompanyGuid;
            return addRequest;
        }

        public override UpdateProfileRequest InitializeUpdateRequest(ref UpdateProfileRequest updateRequest, Variance variance)
        {
            updateRequest.Profile = (Profile)variance.NewObject;
            updateRequest.Profile.CompanyId = CompanyGuid;
            updateRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) { updateRequest.Profile.SiteId = variance.ParentSiteId.Value; updateRequest.SiteId = variance.ParentSiteId.Value; }
            return updateRequest;
        }

        public override RemoveProfileRequest InitializeRemoveRequest(ref RemoveProfileRequest removeRequest, Variance variance)
        {
            removeRequest.ProfileId = ((Profile)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) removeRequest.SiteId = variance.ParentSiteId.Value;
            return removeRequest;
        }
    }

    // ===========================================================================
    // ScaleConfiguration
    // ===========================================================================
    public class ScaleConfigurationRequestGenerator : RequestGenerationBase<
        GetScaleConfigurationsResponse,
        GetCompanyScaleConfigurationsRequest,
        GetScaleConfigurationRequest,
        GetScaleConfigurationResponse,
        AddScaleConfigurationRequest,
        AddScaleConfigurationResponse,
        UpdateScaleConfigurationRequest,
        UpdateScaleConfigurationResponse,
        RemoveScaleConfigurationRequest,
        RemoveScaleConfigurationResponse,
        ScaleConfiguration>
    {
        public ScaleConfigurationRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetCompanyScaleConfigurations", "GetScaleConfiguration", "AddScaleConfiguration", "UpdateScaleConfiguration", "RemoveScaleConfiguration", "ScaleConfiguration", jwt) { }

        public override GetCompanyScaleConfigurationsRequest ModifyGetAllRequest(GetCompanyScaleConfigurationsRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetScaleConfigurationRequest ModifyGetRequestWithId(GetScaleConfigurationRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.ScaleConfigurationId = id;
            return request;
        }

        public AddScaleConfigurationResponse Add(ScaleConfiguration scaleConfiguration)
        {
            AddScaleConfigurationRequest addRequest = new AddScaleConfigurationRequest();
            addRequest.ScaleConfiguration = scaleConfiguration;
            addRequest.ScaleConfiguration.CompanyId = CompanyGuid;
            return BaseAdd(addRequest);
        }

        public AddScaleConfigurationResponse Add(AddScaleConfigurationRequest addRequest) => Add(addRequest.ScaleConfiguration);

        public UpdateScaleConfigurationResponse Update(ScaleConfiguration scaleConfiguration)
        {
            UpdateScaleConfigurationRequest updateRequest = new UpdateScaleConfigurationRequest();
            updateRequest.ScaleConfiguration = scaleConfiguration;
            updateRequest.CompanyId = CompanyGuid;
            return BaseUpdate(updateRequest);
        }

        public UpdateScaleConfigurationResponse Update(UpdateScaleConfigurationRequest updateRequest) => Update(updateRequest.ScaleConfiguration);

        public RemoveScaleConfigurationResponse Remove(int id)
        {
            RemoveScaleConfigurationRequest removeRequest = new RemoveScaleConfigurationRequest();
            removeRequest.ScaleConfigurationId = id;
            removeRequest.CompanyId = CompanyGuid;
            return BaseRemove(removeRequest);
        }

        public RemoveScaleConfigurationResponse Remove(RemoveScaleConfigurationRequest removeRequest) => Remove(removeRequest.ScaleConfigurationId);

        public override bool HasSameId(ScaleConfiguration current, ScaleConfiguration modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(ScaleConfiguration current, ScaleConfiguration modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddScaleConfigurationRequest InitializeAddRequest(ref AddScaleConfigurationRequest addRequest, Variance variance)
        {
            addRequest.ScaleConfiguration = (ScaleConfiguration)variance.NewObject;
            addRequest.ScaleConfiguration.CompanyId = CompanyGuid;
            return addRequest;
        }

        public override UpdateScaleConfigurationRequest InitializeUpdateRequest(ref UpdateScaleConfigurationRequest updateRequest, Variance variance)
        {
            updateRequest.ScaleConfiguration = (ScaleConfiguration)variance.NewObject;
            updateRequest.CompanyId = CompanyGuid;
            return updateRequest;
        }

        public override RemoveScaleConfigurationRequest InitializeRemoveRequest(ref RemoveScaleConfigurationRequest removeRequest, Variance variance)
        {
            removeRequest.ScaleConfigurationId = ((ScaleConfiguration)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            return removeRequest;
        }
    }

    // ===========================================================================
    // Schedule
    // ===========================================================================
    public class ScheduleRequestGenerator : RequestGenerationBase<
        GetSchedulesResponse,
        GetCompanySchedulesRequest,
        GetScheduleRequest,
        GetScheduleResponse,
        AddScheduleRequest,
        AddScheduleResponse,
        UpdateScheduleRequest,
        UpdateScheduleResponse,
        RemoveScheduleRequest,
        RemoveScheduleResponse,
        Schedule>
    {
        public ScheduleRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetCompanySchedules", "GetSchedule", "AddSchedule", "UpdateSchedule", "RemoveSchedule", "Schedule", jwt) { }

        public override GetCompanySchedulesRequest ModifyGetAllRequest(GetCompanySchedulesRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetScheduleRequest ModifyGetRequestWithId(GetScheduleRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.Id = id;
            return request;
        }

        public AddScheduleResponse Add(Schedule schedule, Guid? siteId = null)
        {
            AddScheduleRequest addRequest = new AddScheduleRequest();
            addRequest.Schedule = schedule;
            addRequest.Schedule.CompanyId = CompanyGuid;
            addRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) { addRequest.Schedule.SiteId = siteId.Value; addRequest.SiteId = siteId.Value; }
            return BaseAdd(addRequest);
        }

        public AddScheduleResponse Add(AddScheduleRequest addRequest) => Add(addRequest.Schedule);

        public UpdateScheduleResponse Update(Schedule schedule, Guid? siteId = null)
        {
            UpdateScheduleRequest updateRequest = new UpdateScheduleRequest();
            updateRequest.Schedule = schedule;
            updateRequest.Schedule.CompanyId = CompanyGuid;
            if (siteId.HasValue) updateRequest.Schedule.SiteId = siteId.Value;
            return BaseUpdate(updateRequest);
        }

        public UpdateScheduleResponse Update(UpdateScheduleRequest updateRequest) => Update(updateRequest.Schedule);

        public RemoveScheduleResponse Remove(int id, Guid? siteId = null)
        {
            RemoveScheduleRequest removeRequest = new RemoveScheduleRequest();
            removeRequest.Id = id;
            removeRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) removeRequest.SiteId = siteId.Value;
            return BaseRemove(removeRequest);
        }

        public RemoveScheduleResponse Remove(RemoveScheduleRequest removeRequest) => Remove(removeRequest.Id);

        public override bool HasSameId(Schedule current, Schedule modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(Schedule current, Schedule modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddScheduleRequest InitializeAddRequest(ref AddScheduleRequest addRequest, Variance variance)
        {
            addRequest.Schedule = (Schedule)variance.NewObject;
            addRequest.Schedule.CompanyId = CompanyGuid;
            addRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) { addRequest.Schedule.SiteId = variance.ParentSiteId.Value; addRequest.SiteId = variance.ParentSiteId.Value; }
            return addRequest;
        }

        public override UpdateScheduleRequest InitializeUpdateRequest(ref UpdateScheduleRequest updateRequest, Variance variance)
        {
            updateRequest.Schedule = (Schedule)variance.NewObject;
            updateRequest.Schedule.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) updateRequest.Schedule.SiteId = variance.ParentSiteId.Value;
            return updateRequest;
        }

        public override RemoveScheduleRequest InitializeRemoveRequest(ref RemoveScheduleRequest removeRequest, Variance variance)
        {
            removeRequest.Id = ((Schedule)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) removeRequest.SiteId = variance.ParentSiteId.Value;
            return removeRequest;
        }
    }

    // ===========================================================================
    // SourceConfiguration
    // ===========================================================================
    public class SourceConfigurationRequestGenerator : RequestGenerationBase<
        GetCompanySourceConfigurationsResponse,
        GetCompanySourceConfigurationsRequest,
        GetSourceConfigurationRequest,
        GetSourceConfigurationResponse,
        AddSourceConfigurationRequest,
        AddSourceConfigurationResponse,
        UpdateSourceConfigurationRequest,
        UpdateSourceConfigurationResponse,
        RemoveSourceConfigurationRequest,
        RemoveSourceConfigurationResponse,
        SourceConfiguration>
    {
        public SourceConfigurationRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetCompanySourceConfigurations", "GetSourceConfiguration", "AddSourceConfiguration", "UpdateSourceConfiguration", "RemoveSourceConfiguration", "SourceConfiguration", jwt) { }

        public override GetCompanySourceConfigurationsRequest ModifyGetAllRequest(GetCompanySourceConfigurationsRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetSourceConfigurationRequest ModifyGetRequestWithId(GetSourceConfigurationRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.Id = id;
            return request;
        }

        public AddSourceConfigurationResponse Add(SourceConfiguration sourceConfiguration, Guid? siteId = null)
        {
            AddSourceConfigurationRequest addRequest = new AddSourceConfigurationRequest();
            addRequest.SourceConfiguration = sourceConfiguration;
            addRequest.SourceConfiguration.CompanyId = CompanyGuid;
            if (siteId.HasValue) addRequest.SourceConfiguration.SiteId = siteId.Value;
            return BaseAdd(addRequest);
        }

        public AddSourceConfigurationResponse Add(AddSourceConfigurationRequest addRequest) => Add(addRequest.SourceConfiguration);

        public UpdateSourceConfigurationResponse Update(SourceConfiguration sourceConfiguration, Guid? siteId = null)
        {
            UpdateSourceConfigurationRequest updateRequest = new UpdateSourceConfigurationRequest();
            updateRequest.SourceConfiguration = sourceConfiguration;
            updateRequest.SourceConfiguration.CompanyId = CompanyGuid;
            if (siteId.HasValue) updateRequest.SourceConfiguration.SiteId = siteId.Value;
            return BaseUpdate(updateRequest);
        }

        public UpdateSourceConfigurationResponse Update(UpdateSourceConfigurationRequest updateRequest) => Update(updateRequest.SourceConfiguration);

        public RemoveSourceConfigurationResponse Remove(int id, Guid? siteId = null)
        {
            RemoveSourceConfigurationRequest removeRequest = new RemoveSourceConfigurationRequest();
            removeRequest.Id = id;
            removeRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) removeRequest.SiteId = siteId.Value;
            return BaseRemove(removeRequest);
        }

        public RemoveSourceConfigurationResponse Remove(RemoveSourceConfigurationRequest removeRequest) => Remove(removeRequest.Id);

        public override bool HasSameId(SourceConfiguration current, SourceConfiguration modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(SourceConfiguration current, SourceConfiguration modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddSourceConfigurationRequest InitializeAddRequest(ref AddSourceConfigurationRequest addRequest, Variance variance)
        {
            addRequest.SourceConfiguration = (SourceConfiguration)variance.NewObject;
            addRequest.SourceConfiguration.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) addRequest.SourceConfiguration.SiteId = variance.ParentSiteId.Value;
            return addRequest;
        }

        public override UpdateSourceConfigurationRequest InitializeUpdateRequest(ref UpdateSourceConfigurationRequest updateRequest, Variance variance)
        {
            updateRequest.SourceConfiguration = (SourceConfiguration)variance.NewObject;
            updateRequest.SourceConfiguration.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) updateRequest.SourceConfiguration.SiteId = variance.ParentSiteId.Value;
            return updateRequest;
        }

        public override RemoveSourceConfigurationRequest InitializeRemoveRequest(ref RemoveSourceConfigurationRequest removeRequest, Variance variance)
        {
            removeRequest.Id = ((SourceConfiguration)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) removeRequest.SiteId = variance.ParentSiteId.Value;
            return removeRequest;
        }
    }

    // ===========================================================================
    // BoxType
    // ===========================================================================
    public class BoxTypeRequestGenerator : RequestGenerationBase<
        GetBoxTypesResponse,
        GetCompanyBoxTypesRequest,
        GetBoxTypeRequest,
        GetBoxTypeResponse,
        AddBoxTypeRequest,
        AddBoxTypeResponse,
        UpdateBoxTypeRequest,
        UpdateBoxTypeResponse,
        RemoveBoxTypeRequest,
        RemoveBoxTypeResponse,
        BoxType>
    {
        public BoxTypeRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetCompanyBoxTypes", "GetBoxType", "AddBoxType", "UpdateBoxType", "RemoveBoxType", "BoxType", jwt) { }

        public override GetCompanyBoxTypesRequest ModifyGetAllRequest(GetCompanyBoxTypesRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetBoxTypeRequest ModifyGetRequestWithId(GetBoxTypeRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.Id = id;
            return request;
        }

        public AddBoxTypeResponse Add(BoxType boxType)
        {
            AddBoxTypeRequest addRequest = new AddBoxTypeRequest();
            addRequest.BoxType = boxType;
            addRequest.CompanyId = CompanyGuid;
            return BaseAdd(addRequest);
        }

        public AddBoxTypeResponse Add(AddBoxTypeRequest addRequest) => Add(addRequest.BoxType);

        public UpdateBoxTypeResponse Update(BoxType boxType)
        {
            UpdateBoxTypeRequest updateRequest = new UpdateBoxTypeRequest();
            updateRequest.BoxType = boxType;
            updateRequest.CompanyId = CompanyGuid;
            return BaseUpdate(updateRequest);
        }

        public UpdateBoxTypeResponse Update(UpdateBoxTypeRequest updateRequest) => Update(updateRequest.BoxType);

        public RemoveBoxTypeResponse Remove(int id)
        {
            RemoveBoxTypeRequest removeRequest = new RemoveBoxTypeRequest();
            removeRequest.Id = id;
            removeRequest.CompanyId = CompanyGuid;
            return BaseRemove(removeRequest);
        }

        public RemoveBoxTypeResponse Remove(RemoveBoxTypeRequest removeRequest) => Remove(removeRequest.Id);

        public override bool HasSameId(BoxType current, BoxType modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(BoxType current, BoxType modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddBoxTypeRequest InitializeAddRequest(ref AddBoxTypeRequest addRequest, Variance variance)
        {
            addRequest.BoxType = (BoxType)variance.NewObject;
            addRequest.CompanyId = CompanyGuid;
            return addRequest;
        }

        public override UpdateBoxTypeRequest InitializeUpdateRequest(ref UpdateBoxTypeRequest updateRequest, Variance variance)
        {
            updateRequest.BoxType = (BoxType)variance.NewObject;
            updateRequest.CompanyId = CompanyGuid;
            return updateRequest;
        }

        public override RemoveBoxTypeRequest InitializeRemoveRequest(ref RemoveBoxTypeRequest removeRequest, Variance variance)
        {
            removeRequest.Id = ((BoxType)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            return removeRequest;
        }
    }

    // ===========================================================================
    // DatabaseDefinition
    // Note: GetDatabaseDefinitionRequest and RemoveDatabaseDefinitionRequest do not
    //       have a CompanyId. AddDatabaseDefinitionRequest takes only a Name string,
    //       not a DatabaseDefinition entity; use databaseDefinition.Name to populate.
    // ===========================================================================
    public class DatabaseDefinitionRequestGenerator : RequestGenerationBase<
        GetDatabaseDefinitionsResponse,
        GetDatabaseDefinitionsRequest,
        GetDatabaseDefinitionRequest,
        GetDatabaseDefinitionResponse,
        AddDatabaseDefinitionRequest,
        AddDatabaseDefinitionResponse,
        UpdateDatabaseDefinitionRequest,
        UpdateDatabaseDefinitionResponse,
        RemoveDatabaseDefinitionRequest,
        RemoveDatabaseDefinitionResponse,
        DatabaseDefinition>
    {
        public DatabaseDefinitionRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetDatabaseDefinitions", "GetDatabaseDefinition", "AddDatabaseDefinition", "UpdateDatabaseDefinition", "RemoveDatabaseDefinition", "DatabaseDefinition", jwt) { }

        public override GetDatabaseDefinitionsRequest ModifyGetAllRequest(GetDatabaseDefinitionsRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetDatabaseDefinitionRequest ModifyGetRequestWithId(GetDatabaseDefinitionRequest request, int id)
        {
            request.Id = id;
            return request;
        }

        public AddDatabaseDefinitionResponse Add(DatabaseDefinition databaseDefinition)
        {
            AddDatabaseDefinitionRequest addRequest = new AddDatabaseDefinitionRequest();
            addRequest.Name = databaseDefinition.Name;
            return BaseAdd(addRequest);
        }

        public AddDatabaseDefinitionResponse Add(AddDatabaseDefinitionRequest addRequest) => BaseAdd(addRequest);

        public UpdateDatabaseDefinitionResponse Update(DatabaseDefinition databaseDefinition)
        {
            UpdateDatabaseDefinitionRequest updateRequest = new UpdateDatabaseDefinitionRequest();
            updateRequest.DatabaseDefinition = databaseDefinition;
            return BaseUpdate(updateRequest);
        }

        public UpdateDatabaseDefinitionResponse Update(UpdateDatabaseDefinitionRequest updateRequest) => Update(updateRequest.DatabaseDefinition);

        public RemoveDatabaseDefinitionResponse Remove(int id)
        {
            RemoveDatabaseDefinitionRequest removeRequest = new RemoveDatabaseDefinitionRequest();
            removeRequest.Id = id;
            return BaseRemove(removeRequest);
        }

        public RemoveDatabaseDefinitionResponse Remove(RemoveDatabaseDefinitionRequest removeRequest) => Remove(removeRequest.Id);

        public override bool HasSameId(DatabaseDefinition current, DatabaseDefinition modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(DatabaseDefinition current, DatabaseDefinition modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddDatabaseDefinitionRequest InitializeAddRequest(ref AddDatabaseDefinitionRequest addRequest, Variance variance)
        {
            addRequest.Name = ((DatabaseDefinition)variance.NewObject).Name;
            return addRequest;
        }

        public override UpdateDatabaseDefinitionRequest InitializeUpdateRequest(ref UpdateDatabaseDefinitionRequest updateRequest, Variance variance)
        {
            updateRequest.DatabaseDefinition = (DatabaseDefinition)variance.NewObject;
            return updateRequest;
        }

        public override RemoveDatabaseDefinitionRequest InitializeRemoveRequest(ref RemoveDatabaseDefinitionRequest removeRequest, Variance variance)
        {
            removeRequest.Id = ((DatabaseDefinition)variance.OriginalObject).Id;
            return removeRequest;
        }
    }

    // ===========================================================================
    // DistributionList
    // ===========================================================================
    public class DistributionListRequestGenerator : RequestGenerationBase<
        GetDistributionListsResponse,
        GetCompanyDistributionListsRequest,
        GetDistributionListRequest,
        GetDistributionListResponse,
        AddDistributionListRequest,
        AddDistributionListResponse,
        UpdateDistributionListRequest,
        UpdateDistributionListResponse,
        RemoveDistributionListRequest,
        RemoveDistributionListResponse,
        DistributionList>
    {
        public DistributionListRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetCompanyDistributionLists", "GetDistributionList", "AddDistributionList", "UpdateDistributionList", "RemoveDistributionList", "DistributionList", jwt) { }

        public override GetCompanyDistributionListsRequest ModifyGetAllRequest(GetCompanyDistributionListsRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetDistributionListRequest ModifyGetRequestWithId(GetDistributionListRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.Id = id;
            return request;
        }

        public AddDistributionListResponse Add(DistributionList distributionList)
        {
            AddDistributionListRequest addRequest = new AddDistributionListRequest();
            addRequest.DistributionList = distributionList;
            addRequest.CompanyId = CompanyGuid;
            return BaseAdd(addRequest);
        }

        public AddDistributionListResponse Add(AddDistributionListRequest addRequest) => Add(addRequest.DistributionList);

        public UpdateDistributionListResponse Update(DistributionList distributionList)
        {
            UpdateDistributionListRequest updateRequest = new UpdateDistributionListRequest();
            updateRequest.DistributionList = distributionList;
            updateRequest.CompanyId = CompanyGuid;
            return BaseUpdate(updateRequest);
        }

        public UpdateDistributionListResponse Update(UpdateDistributionListRequest updateRequest) => Update(updateRequest.DistributionList);

        public RemoveDistributionListResponse Remove(int id)
        {
            RemoveDistributionListRequest removeRequest = new RemoveDistributionListRequest();
            removeRequest.Id = id;
            removeRequest.CompanyId = CompanyGuid;
            return BaseRemove(removeRequest);
        }

        public RemoveDistributionListResponse Remove(RemoveDistributionListRequest removeRequest) => Remove(removeRequest.Id);

        public override bool HasSameId(DistributionList current, DistributionList modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(DistributionList current, DistributionList modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddDistributionListRequest InitializeAddRequest(ref AddDistributionListRequest addRequest, Variance variance)
        {
            addRequest.DistributionList = (DistributionList)variance.NewObject;
            addRequest.CompanyId = CompanyGuid;
            return addRequest;
        }

        public override UpdateDistributionListRequest InitializeUpdateRequest(ref UpdateDistributionListRequest updateRequest, Variance variance)
        {
            updateRequest.DistributionList = (DistributionList)variance.NewObject;
            updateRequest.CompanyId = CompanyGuid;
            return updateRequest;
        }

        public override RemoveDistributionListRequest InitializeRemoveRequest(ref RemoveDistributionListRequest removeRequest, Variance variance)
        {
            removeRequest.Id = ((DistributionList)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            return removeRequest;
        }
    }

    // ===========================================================================
    // HazmatContent
    // Note: GetHazmatContentsResponse is in PSI.Sox.Wcf (not Administration).
    // ===========================================================================
    public class HazmatContentRequestGenerator : RequestGenerationBase<
        GetHazmatContentsResponse,
        GetCompanyHazmatContentsRequest,
        GetHazmatContentRequest,
        GetHazmatContentResponse,
        AddHazmatContentRequest,
        AddHazmatContentResponse,
        UpdateHazmatContentRequest,
        UpdateHazmatContentResponse,
        RemoveHazmatContentRequest,
        RemoveHazmatContentResponse,
        HazmatContent>
    {
        public HazmatContentRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetCompanyHazmatContents", "GetHazmatContent", "AddHazmatContent", "UpdateHazmatContent", "RemoveHazmatContent", "HazmatContent", jwt) { }

        public override GetCompanyHazmatContentsRequest ModifyGetAllRequest(GetCompanyHazmatContentsRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetHazmatContentRequest ModifyGetRequestWithId(GetHazmatContentRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.Id = id;
            return request;
        }

        public AddHazmatContentResponse Add(HazmatContent hazmatContent)
        {
            AddHazmatContentRequest addRequest = new AddHazmatContentRequest();
            addRequest.HazmatContent = hazmatContent;
            addRequest.CompanyId = CompanyGuid;
            return BaseAdd(addRequest);
        }

        public AddHazmatContentResponse Add(AddHazmatContentRequest addRequest) => Add(addRequest.HazmatContent);

        public UpdateHazmatContentResponse Update(HazmatContent hazmatContent)
        {
            UpdateHazmatContentRequest updateRequest = new UpdateHazmatContentRequest();
            updateRequest.HazmatContent = hazmatContent;
            updateRequest.CompanyId = CompanyGuid;
            return BaseUpdate(updateRequest);
        }

        public UpdateHazmatContentResponse Update(UpdateHazmatContentRequest updateRequest) => Update(updateRequest.HazmatContent);

        public RemoveHazmatContentResponse Remove(int id)
        {
            RemoveHazmatContentRequest removeRequest = new RemoveHazmatContentRequest();
            removeRequest.Id = id;
            removeRequest.CompanyId = CompanyGuid;
            return BaseRemove(removeRequest);
        }

        public RemoveHazmatContentResponse Remove(RemoveHazmatContentRequest removeRequest) => Remove(removeRequest.Id);

        public override bool HasSameId(HazmatContent current, HazmatContent modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(HazmatContent current, HazmatContent modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddHazmatContentRequest InitializeAddRequest(ref AddHazmatContentRequest addRequest, Variance variance)
        {
            addRequest.HazmatContent = (HazmatContent)variance.NewObject;
            addRequest.CompanyId = CompanyGuid;
            return addRequest;
        }

        public override UpdateHazmatContentRequest InitializeUpdateRequest(ref UpdateHazmatContentRequest updateRequest, Variance variance)
        {
            updateRequest.HazmatContent = (HazmatContent)variance.NewObject;
            updateRequest.CompanyId = CompanyGuid;
            return updateRequest;
        }

        public override RemoveHazmatContentRequest InitializeRemoveRequest(ref RemoveHazmatContentRequest removeRequest, Variance variance)
        {
            removeRequest.Id = ((HazmatContent)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            return removeRequest;
        }
    }

    // ===========================================================================
    // Notification
    // Note: GetNotificationsResponse is in PSI.Sox.Wcf (not Administration).
    // ===========================================================================
    public class NotificationRequestGenerator : RequestGenerationBase<
        GetNotificationsResponse,
        GetCompanyNotificationsRequest,
        GetNotificationRequest,
        GetNotificationResponse,
        AddNotificationRequest,
        AddNotificationResponse,
        UpdateNotificationRequest,
        UpdateNotificationResponse,
        RemoveNotificationRequest,
        RemoveNotificationResponse,
        Notification>
    {
        public NotificationRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetCompanyNotifications", "GetNotification", "AddNotification", "UpdateNotification", "RemoveNotification", "Notification", jwt) { }

        public override GetCompanyNotificationsRequest ModifyGetAllRequest(GetCompanyNotificationsRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetNotificationRequest ModifyGetRequestWithId(GetNotificationRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.Id = id;
            return request;
        }

        public AddNotificationResponse Add(Notification notification)
        {
            AddNotificationRequest addRequest = new AddNotificationRequest();
            addRequest.Notification = notification;
            addRequest.CompanyId = CompanyGuid;
            return BaseAdd(addRequest);
        }

        public AddNotificationResponse Add(AddNotificationRequest addRequest) => Add(addRequest.Notification);

        public UpdateNotificationResponse Update(Notification notification)
        {
            UpdateNotificationRequest updateRequest = new UpdateNotificationRequest();
            updateRequest.Notification = notification;
            updateRequest.CompanyId = CompanyGuid;
            return BaseUpdate(updateRequest);
        }

        public UpdateNotificationResponse Update(UpdateNotificationRequest updateRequest) => Update(updateRequest.Notification);

        public RemoveNotificationResponse Remove(int id)
        {
            RemoveNotificationRequest removeRequest = new RemoveNotificationRequest();
            removeRequest.Id = id;
            removeRequest.CompanyId = CompanyGuid;
            return BaseRemove(removeRequest);
        }

        public RemoveNotificationResponse Remove(RemoveNotificationRequest removeRequest) => Remove(removeRequest.Id);

        public override bool HasSameId(Notification current, Notification modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(Notification current, Notification modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddNotificationRequest InitializeAddRequest(ref AddNotificationRequest addRequest, Variance variance)
        {
            addRequest.Notification = (Notification)variance.NewObject;
            addRequest.CompanyId = CompanyGuid;
            return addRequest;
        }

        public override UpdateNotificationRequest InitializeUpdateRequest(ref UpdateNotificationRequest updateRequest, Variance variance)
        {
            updateRequest.Notification = (Notification)variance.NewObject;
            updateRequest.CompanyId = CompanyGuid;
            return updateRequest;
        }

        public override RemoveNotificationRequest InitializeRemoveRequest(ref RemoveNotificationRequest removeRequest, Variance variance)
        {
            removeRequest.Id = ((Notification)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            return removeRequest;
        }
    }

    // ===========================================================================
    // Report
    // ===========================================================================
    public class ReportRequestGenerator : RequestGenerationBase<
        GetReportsResponse,
        GetReportsRequest,
        GetReportRequest,
        GetReportResponse,
        AddReportRequest,
        AddReportResponse,
        UpdateReportRequest,
        UpdateReportResponse,
        RemoveReportRequest,
        RemoveReportResponse,
        Report>
    {
        public ReportRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetReports", "GetReport", "AddReport", "UpdateReport", "RemoveReport", "Report", jwt) { }

        public override GetReportsRequest ModifyGetAllRequest(GetReportsRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetReportRequest ModifyGetRequestWithId(GetReportRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.Id = id;
            return request;
        }

        public AddReportResponse Add(Report report)
        {
            AddReportRequest addRequest = new AddReportRequest();
            addRequest.Report = report;
            addRequest.CompanyId = CompanyGuid;
            return BaseAdd(addRequest);
        }

        public AddReportResponse Add(AddReportRequest addRequest) => Add(addRequest.Report);

        public UpdateReportResponse Update(Report report)
        {
            UpdateReportRequest updateRequest = new UpdateReportRequest();
            updateRequest.Report = report;
            updateRequest.CompanyId = CompanyGuid;
            return BaseUpdate(updateRequest);
        }

        public UpdateReportResponse Update(UpdateReportRequest updateRequest) => Update(updateRequest.Report);

        public RemoveReportResponse Remove(int id)
        {
            RemoveReportRequest removeRequest = new RemoveReportRequest();
            removeRequest.Id = id;
            removeRequest.CompanyId = CompanyGuid;
            return BaseRemove(removeRequest);
        }

        public RemoveReportResponse Remove(RemoveReportRequest removeRequest) => Remove(removeRequest.Id);

        public override bool HasSameId(Report current, Report modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(Report current, Report modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddReportRequest InitializeAddRequest(ref AddReportRequest addRequest, Variance variance)
        {
            addRequest.Report = (Report)variance.NewObject;
            addRequest.CompanyId = CompanyGuid;
            return addRequest;
        }

        public override UpdateReportRequest InitializeUpdateRequest(ref UpdateReportRequest updateRequest, Variance variance)
        {
            updateRequest.Report = (Report)variance.NewObject;
            updateRequest.CompanyId = CompanyGuid;
            return updateRequest;
        }

        public override RemoveReportRequest InitializeRemoveRequest(ref RemoveReportRequest removeRequest, Variance variance)
        {
            removeRequest.Id = ((Report)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            return removeRequest;
        }
    }

    // ===========================================================================
    // ServerBusinessRule
    // Note: GetServerBusinessRulesResponse.ServerBusinessRule is a singular-named list property.
    // ===========================================================================
    public class ServerBusinessRuleRequestGenerator : RequestGenerationBase<
        GetServerBusinessRulesResponse,
        GetServerBusinessRulesRequest,
        GetServerBusinessRuleRequest,
        GetServerBusinessRuleResponse,
        AddServerBusinessRuleRequest,
        AddServerBusinessRuleResponse,
        UpdateServerBusinessRuleRequest,
        UpdateServerBusinessRuleResponse,
        RemoveServerBusinessRuleRequest,
        RemoveServerBusinessRuleResponse,
        ServerBusinessRule>
    {
        public ServerBusinessRuleRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetServerBusinessRules", "GetServerBusinessRule", "AddServerBusinessRule", "UpdateServerBusinessRule", "RemoveServerBusinessRule", "ServerBusinessRule", jwt) { }

        public override GetServerBusinessRulesRequest ModifyGetAllRequest(GetServerBusinessRulesRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetServerBusinessRuleRequest ModifyGetRequestWithId(GetServerBusinessRuleRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.Id = id;
            return request;
        }

        public AddServerBusinessRuleResponse Add(ServerBusinessRule serverBusinessRule)
        {
            AddServerBusinessRuleRequest addRequest = new AddServerBusinessRuleRequest();
            addRequest.ServerBusinessRule = serverBusinessRule;
            addRequest.ServerBusinessRule.CompanyId = CompanyGuid;
            return BaseAdd(addRequest);
        }

        public AddServerBusinessRuleResponse Add(AddServerBusinessRuleRequest addRequest) => Add(addRequest.ServerBusinessRule);

        public UpdateServerBusinessRuleResponse Update(ServerBusinessRule serverBusinessRule)
        {
            UpdateServerBusinessRuleRequest updateRequest = new UpdateServerBusinessRuleRequest();
            updateRequest.ServerBusinessRule = serverBusinessRule;
            updateRequest.ServerBusinessRule.CompanyId = CompanyGuid;
            return BaseUpdate(updateRequest);
        }

        public UpdateServerBusinessRuleResponse Update(UpdateServerBusinessRuleRequest updateRequest) => Update(updateRequest.ServerBusinessRule);

        public RemoveServerBusinessRuleResponse Remove(int id)
        {
            RemoveServerBusinessRuleRequest removeRequest = new RemoveServerBusinessRuleRequest();
            removeRequest.Id = id;
            removeRequest.CompanyId = CompanyGuid;
            return BaseRemove(removeRequest);
        }

        public RemoveServerBusinessRuleResponse Remove(RemoveServerBusinessRuleRequest removeRequest) => Remove(removeRequest.Id);

        public override bool HasSameId(ServerBusinessRule current, ServerBusinessRule modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(ServerBusinessRule current, ServerBusinessRule modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddServerBusinessRuleRequest InitializeAddRequest(ref AddServerBusinessRuleRequest addRequest, Variance variance)
        {
            addRequest.ServerBusinessRule = (ServerBusinessRule)variance.NewObject;
            addRequest.ServerBusinessRule.CompanyId = CompanyGuid;
            return addRequest;
        }

        public override UpdateServerBusinessRuleRequest InitializeUpdateRequest(ref UpdateServerBusinessRuleRequest updateRequest, Variance variance)
        {
            updateRequest.ServerBusinessRule = (ServerBusinessRule)variance.NewObject;
            updateRequest.ServerBusinessRule.CompanyId = CompanyGuid;
            return updateRequest;
        }

        public override RemoveServerBusinessRuleRequest InitializeRemoveRequest(ref RemoveServerBusinessRuleRequest removeRequest, Variance variance)
        {
            removeRequest.Id = ((ServerBusinessRule)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            return removeRequest;
        }
    }

    // ===========================================================================
    // Template
    // ===========================================================================
    public class TemplateRequestGenerator : RequestGenerationBase<
        GetTemplatesResponse,
        GetCompanyTemplatesRequest,
        GetTemplateRequest,
        GetTemplateResponse,
        AddTemplateRequest,
        AddTemplateResponse,
        UpdateTemplateRequest,
        UpdateTemplateResponse,
        RemoveTemplateRequest,
        RemoveTemplateResponse,
        Template>
    {
        public TemplateRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetCompanyTemplates", "GetTemplate", "AddTemplate", "UpdateTemplate", "RemoveTemplate", "Template", jwt) { }

        public override GetCompanyTemplatesRequest ModifyGetAllRequest(GetCompanyTemplatesRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetTemplateRequest ModifyGetRequestWithId(GetTemplateRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.Id = id;
            return request;
        }

        public AddTemplateResponse Add(Template template)
        {
            AddTemplateRequest addRequest = new AddTemplateRequest();
            addRequest.Template = template;
            addRequest.CompanyId = CompanyGuid;
            return BaseAdd(addRequest);
        }

        public AddTemplateResponse Add(AddTemplateRequest addRequest) => Add(addRequest.Template);

        public UpdateTemplateResponse Update(Template template)
        {
            UpdateTemplateRequest updateRequest = new UpdateTemplateRequest();
            updateRequest.Template = template;
            updateRequest.CompanyId = CompanyGuid;
            return BaseUpdate(updateRequest);
        }

        public UpdateTemplateResponse Update(UpdateTemplateRequest updateRequest) => Update(updateRequest.Template);

        public RemoveTemplateResponse Remove(int id)
        {
            RemoveTemplateRequest removeRequest = new RemoveTemplateRequest();
            removeRequest.Id = id;
            removeRequest.CompanyId = CompanyGuid;
            return BaseRemove(removeRequest);
        }

        public RemoveTemplateResponse Remove(RemoveTemplateRequest removeRequest) => Remove(removeRequest.Id);

        public override bool HasSameId(Template current, Template modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(Template current, Template modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddTemplateRequest InitializeAddRequest(ref AddTemplateRequest addRequest, Variance variance)
        {
            addRequest.Template = (Template)variance.NewObject;
            addRequest.CompanyId = CompanyGuid;
            return addRequest;
        }

        public override UpdateTemplateRequest InitializeUpdateRequest(ref UpdateTemplateRequest updateRequest, Variance variance)
        {
            updateRequest.Template = (Template)variance.NewObject;
            updateRequest.CompanyId = CompanyGuid;
            return updateRequest;
        }

        public override RemoveTemplateRequest InitializeRemoveRequest(ref RemoveTemplateRequest removeRequest, Variance variance)
        {
            removeRequest.Id = ((Template)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            return removeRequest;
        }
    }

    // ===========================================================================
    // Validation
    // Note: AddCompanyValidationRequest is used (no plain AddValidationRequest exists).
    //       UpdateValidationRequest takes Name + ValidationId instead of a Validation entity.
    // ===========================================================================
    public class ValidationRequestGenerator : RequestGenerationBase<
        GetValidationsResponse,
        GetCompanyValidationsRequest,
        GetValidationRequest,
        GetValidationResponse,
        AddCompanyValidationRequest,
        AddValidationResponse,
        UpdateValidationRequest,
        UpdateValidationResponse,
        RemoveValidationRequest,
        RemoveValidationResponse,
        Validation>
    {
        public ValidationRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetCompanyValidations", "GetValidation", "AddCompanyValidation", "UpdateValidation", "RemoveValidation", "Validation", jwt) { }

        public override GetCompanyValidationsRequest ModifyGetAllRequest(GetCompanyValidationsRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetValidationRequest ModifyGetRequestWithId(GetValidationRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.Id = id;
            return request;
        }

        public AddValidationResponse Add(Validation validation)
        {
            AddCompanyValidationRequest addRequest = new AddCompanyValidationRequest();
            addRequest.Validation = validation;
            addRequest.CompanyId = CompanyGuid;
            return BaseAdd(addRequest);
        }

        public AddValidationResponse Add(AddCompanyValidationRequest addRequest) => BaseAdd(addRequest);

        public UpdateValidationResponse Update(Validation validation)
        {
            UpdateValidationRequest updateRequest = new UpdateValidationRequest();
            updateRequest.Name = validation.Name;
            updateRequest.ValidationId = validation.Id;
            return BaseUpdate(updateRequest);
        }

        public UpdateValidationResponse Update(UpdateValidationRequest updateRequest) => BaseUpdate(updateRequest);

        public RemoveValidationResponse Remove(int id)
        {
            RemoveValidationRequest removeRequest = new RemoveValidationRequest();
            removeRequest.Id = id;
            removeRequest.CompanyId = CompanyGuid;
            return BaseRemove(removeRequest);
        }

        public RemoveValidationResponse Remove(RemoveValidationRequest removeRequest) => Remove(removeRequest.Id);

        public override bool HasSameId(Validation current, Validation modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(Validation current, Validation modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddCompanyValidationRequest InitializeAddRequest(ref AddCompanyValidationRequest addRequest, Variance variance)
        {
            addRequest.Validation = (Validation)variance.NewObject;
            addRequest.CompanyId = CompanyGuid;
            return addRequest;
        }

        public override UpdateValidationRequest InitializeUpdateRequest(ref UpdateValidationRequest updateRequest, Variance variance)
        {
            updateRequest.Name = ((Validation)variance.NewObject).Name;
            updateRequest.ValidationId = ((Validation)variance.OriginalObject).Id;
            return updateRequest;
        }

        public override RemoveValidationRequest InitializeRemoveRequest(ref RemoveValidationRequest removeRequest, Variance variance)
        {
            removeRequest.Id = ((Validation)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            return removeRequest;
        }
    }

    // ===========================================================================
    // Site
    // Note: AddSiteRequest takes only Name + CompanyId (no Site entity).
    //       RemoveSiteRequest uses SiteId (Guid). HasSameId uses Guid comparison.
    //       ShouldUpdate uses JSON so any nested change (e.g. added Machine) is caught.
    // ===========================================================================
    public class SiteRequestGenerator : RequestGenerationBase<
        GetSitesResponse,
        GetSitesRequest,
        GetSiteRequest,
        GetSiteResponse,
        AddSiteRequest,
        AddSiteResponse,
        UpdateSiteRequest,
        UpdateSiteResponse,
        RemoveSiteRequest,
        RemoveSiteResponse,
        Site>
    {
        public SiteRequestGenerator(string adminUrl, Guid companyGuid, string jwt)
            : base(adminUrl, companyGuid, "GetSites", "GetSite", "AddSite", "UpdateSite", "RemoveSite", "Site", jwt) { }

        public override GetSitesRequest ModifyGetAllRequest(GetSitesRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetSiteRequest ModifyGetRequestWithId(GetSiteRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public AddSiteResponse Add(Site site)
        {
            AddSiteRequest addRequest = new AddSiteRequest();
            addRequest.Name = site.Name;
            addRequest.CompanyId = CompanyGuid;
            return BaseAdd(addRequest);
        }

        public AddSiteResponse Add(AddSiteRequest addRequest) => BaseAdd(addRequest);

        public UpdateSiteResponse Update(Site site)
        {
            UpdateSiteRequest updateRequest = new UpdateSiteRequest();
            updateRequest.Site = site;
            updateRequest.CompanyId = CompanyGuid;
            updateRequest.SiteId = site.Id;
            return BaseUpdate(updateRequest);
        }

        public UpdateSiteResponse Update(UpdateSiteRequest updateRequest) => Update(updateRequest.Site);

        public RemoveSiteResponse Remove(Guid siteId)
        {
            RemoveSiteRequest removeRequest = new RemoveSiteRequest();
            removeRequest.SiteId = siteId;
            removeRequest.CompanyId = CompanyGuid;
            return BaseRemove(removeRequest);
        }

        public RemoveSiteResponse Remove(RemoveSiteRequest removeRequest) => Remove(removeRequest.SiteId);

        public override bool HasSameId(Site current, Site modified) => current.Id == modified.Id;

        public override bool ShouldUpdate(Site current, Site modified)
            => JsonHelper.Serialize(current) != JsonHelper.Serialize(modified);

        public override AddSiteRequest InitializeAddRequest(ref AddSiteRequest addRequest, Variance variance)
        {
            addRequest.Name = ((Site)variance.NewObject).Name;
            addRequest.CompanyId = CompanyGuid;
            return addRequest;
        }

        public override UpdateSiteRequest InitializeUpdateRequest(ref UpdateSiteRequest updateRequest, Variance variance)
        {
            updateRequest.Site = (Site)variance.NewObject;
            updateRequest.CompanyId = CompanyGuid;
            updateRequest.SiteId = ((Site)variance.NewObject).Id;
            return updateRequest;
        }

        public override RemoveSiteRequest InitializeRemoveRequest(ref RemoveSiteRequest removeRequest, Variance variance)
        {
            removeRequest.SiteId = ((Site)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            return removeRequest;
        }
    }
}
