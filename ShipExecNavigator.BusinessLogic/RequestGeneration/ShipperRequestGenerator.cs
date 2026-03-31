using PSI.Sox.Wcf.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ShipExecNavigator.BusinessLogic.EntityComparison;
using PSI.Sox.Wcf;
using ShipExecNavigator.BusinessLogic.ResponseModel;
using PSI.Sox;

namespace ShipExecNavigator.BusinessLogic.RequestGeneration
{
    public class ShipperRequestGenerator : RequestGenerationBase<GetShippersResponse,
        GetShippersRequest,
        GetShipperRequest,
        GetShipperResponse,
        AddShipperRequest,
        AddShipperResponse,
        UpdateShipperRequest,
        UpdateShipperResponse,
        RemoveShipperRequest,
        RemoveShipperResponse,
        Shipper>
    {

        public ShipperRequestGenerator(string adminUrl, Guid companyGuid, string jwt) :
            base(adminUrl, companyGuid, "GetShippers", "GetShipper", "AddShipper", "UpdateShipper", "RemoveShipper", "Shipper", jwt)
        {
        }

        public override GetShippersRequest ModifyGetAllRequest(GetShippersRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetShipperRequest ModifyGetRequestWithId(GetShipperRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            //request.ShipperId = id;
            request.Id = id;
            return request;
        }

        public AddShipperResponse Add(Shipper shipper, Guid? siteId = null)
        {
            AddShipperRequest addShipperRequest = new AddShipperRequest();
            addShipperRequest.Shipper = shipper;
            addShipperRequest.Shipper.CompanyId = CompanyGuid;
            if (siteId.HasValue) addShipperRequest.Shipper.SiteId = siteId.Value;

            return BaseAdd(addShipperRequest);
        }

        public AddShipperResponse Add(AddShipperRequest addShipperRequest)
        {
            return Add(addShipperRequest.Shipper);
        }




        public UpdateShipperResponse Update(Shipper shipper, Guid? siteId = null)
        {
            UpdateShipperRequest updateShipperRequest = new UpdateShipperRequest();
            updateShipperRequest.Shipper = shipper;
            updateShipperRequest.Shipper.CompanyId = CompanyGuid;
            updateShipperRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) { updateShipperRequest.Shipper.SiteId = siteId.Value; updateShipperRequest.SiteId = siteId.Value; }

            return BaseUpdate(updateShipperRequest);
        }

        public UpdateShipperResponse Update(UpdateShipperRequest updateShipperRequest)
        {
            return Update(updateShipperRequest.Shipper);
        }

        public RemoveShipperResponse Remove(int shipperId, Guid? siteId = null)
        {
            RemoveShipperRequest removeShipperRequest = new RemoveShipperRequest();
            removeShipperRequest.ShipperId = shipperId;
            removeShipperRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) removeShipperRequest.SiteId = siteId.Value;

            return BaseRemove(removeShipperRequest);
        }

        public RemoveShipperResponse Remove(RemoveShipperRequest removeShipperRequest)
        {
            return Remove(removeShipperRequest.ShipperId);
        }

        public override bool HasSameId(Shipper current, Shipper modified)
        {
            return current.Id == modified.Id;
        }

        public override bool ShouldUpdate(Shipper current, Shipper modified)
        {
            // String fields are compared after trimming so that leading/trailing
            // whitespace differences ("City" vs "City ") do not trigger an update.
            bool StrDiff(string a, string b) =>
                !string.Equals((a ?? string.Empty).Trim(), (b ?? string.Empty).Trim(), StringComparison.Ordinal);

            return StrDiff(current.Name,          modified.Name)
                || StrDiff(current.Symbol,        modified.Symbol)
                || StrDiff(current.Code,          modified.Code)
                || StrDiff(current.Address1,      modified.Address1)
                || StrDiff(current.Address2,      modified.Address2)
                || StrDiff(current.Address3,      modified.Address3)
                || StrDiff(current.City,          modified.City)
                || StrDiff(current.StateProvince, modified.StateProvince)
                || StrDiff(current.PostalCode,    modified.PostalCode)
                || StrDiff(current.Country,       modified.Country)
                || StrDiff(current.Company,       modified.Company)
                || StrDiff(current.Contact,       modified.Contact)
                || StrDiff(current.Phone,         modified.Phone)
                || StrDiff(current.Fax,           modified.Fax)
                || StrDiff(current.Email,         modified.Email)
                || StrDiff(current.Sms,           modified.Sms)
                || current.SiteId      != modified.SiteId
                || current.PoBox       != modified.PoBox
                || current.Residential != modified.Residential
                || JsonHelper.Serialize(current.Carriers)   != JsonHelper.Serialize(modified.Carriers)
                || JsonHelper.Serialize(current.CustomData) != JsonHelper.Serialize(modified.CustomData);
        }


        public override AddShipperRequest InitializeAddRequest(ref AddShipperRequest addRequest, Variance variance)
        {
            addRequest.Shipper = (Shipper)variance.NewObject;
            addRequest.Shipper.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) addRequest.Shipper.SiteId = variance.ParentSiteId.Value;

            return addRequest;
        }

        public override UpdateShipperRequest InitializeUpdateRequest(ref UpdateShipperRequest updateRequest, Variance variance)
        {
            updateRequest.Shipper = (Shipper)variance.NewObject;
            updateRequest.Shipper.CompanyId = CompanyGuid;
            updateRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) { updateRequest.Shipper.SiteId = variance.ParentSiteId.Value; updateRequest.SiteId = variance.ParentSiteId.Value; }

            return updateRequest;
        }

        public override RemoveShipperRequest InitializeRemoveRequest(ref RemoveShipperRequest removeRequest, Variance variance)
        {
            removeRequest.ShipperId = ((Shipper)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) removeRequest.SiteId = variance.ParentSiteId.Value;

            return removeRequest;
        }

    }

    public class GetShippersResponse : GetResponseBase
    {
        public List<Shipper> Shippers { get; set; }
    }

    }
