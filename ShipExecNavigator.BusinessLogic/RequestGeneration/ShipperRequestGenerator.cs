using PSI.Sox.Data;
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
using Microsoft.Extensions.Logging;
using ShipExecNavigator.BusinessLogic.Logging;

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
            request.SearchCriteria = new SearchCriteria
            {
                Skip = 0,
                Take = int.MaxValue,
                WhereClauses = new List<WhereClause>(),
                OrderByClauses = new List<OrderByClause>()
            };
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

        /// <summary>
        /// After a <see cref="Remove"/> call, contains one <see cref="Variance"/> for
        /// each adapter-shipper mapping that was cascade-deleted before the shipper
        /// itself was removed.  Callers can attach these to the parent shipper
        /// variance so they appear in the UI.
        /// </summary>
        public List<Variance> LastCascadeVariances { get; private set; } = new List<Variance>();

        public RemoveShipperResponse Remove(int shipperId, Guid? siteId = null)
        {
            LastCascadeVariances = RemoveAdapterShipperMappingsForShipper(shipperId);

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

        /// <summary>
        /// Finds and removes all adapter-shipper mappings that reference the given
        /// <paramref name="shipperId"/> so the shipper can be safely deleted without
        /// violating foreign-key constraints.
        /// Returns a <see cref="Variance"/> for each mapping that was deleted so
        /// callers can surface the cascade removals in the UI.
        /// </summary>
        private List<Variance> RemoveAdapterShipperMappingsForShipper(int shipperId)
        {
            var logger = LoggerProvider.CreateLogger<ShipperRequestGenerator>();
            var variances = new List<Variance>();

            var adapterGen = new AdapterRegistrationRequestGenerator(AdminUrl, CompanyGuid, JWT);
            var adapterResponse = adapterGen.Get();
            var registrations = adapterResponse?.AdapterRegistrations;
            if (registrations == null || registrations.Count == 0)
                return variances;

            foreach (var registration in registrations)
            {
                var mappings = GetAdapterShipperMappings(registration.Id);
                if (mappings == null || mappings.Count == 0)
                    continue;

                foreach (var mapping in mappings)
                {
                    if (mapping.ShipExecShipperId == shipperId)
                    {
                        logger.LogInformation(
                            "Removing AdapterShipperMapping {MappingId} (AdapterRegistration {AdapterRegId}) before deleting Shipper {ShipperId}",
                            mapping.Id, registration.Id, shipperId);
                        RemoveAdapterShipperMapping(mapping.Id);

                        variances.Add(new Variance
                        {
                            EntityName     = "AdapterShipperMapping",
                            OriginalObject = mapping,
                            IsRemove       = true,
                            CompanyId      = CompanyGuid,
                            Description    = $"Cascade-removed AdapterShipperMapping {mapping.Id} (AdapterRegistration {registration.Id}) for Shipper {shipperId}",
                            ChangeType     = "Remove",
                        });
                    }
                }
            }

            return variances;
        }

        /// <summary>
        /// Fetches all adapter-shipper mappings for a given adapter registration.
        /// </summary>
        private List<PSI.Sox.Adapter.AdapterShipperMapping> GetAdapterShipperMappings(int adapterRegistrationId)
        {
            var request = new GetAdapterShipperMappingsRequest
            {
                AdapterRegistrationId = adapterRegistrationId,
                CompanyId = CompanyGuid,
                SearchCriteria = new SearchCriteria
                {
                    WhereClauses = new List<WhereClause>(),
                    OrderByClauses = new List<OrderByClause>()
                }
            };

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, AdminUrl + "GetAdapterShipperMappings"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", JWT);
                var json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var webResult = httpClient.SendAsync(requestMessage).Result;
                string resultContent = webResult.Content.ReadAsStringAsync().Result;
                var response = JsonHelper.Deserialize<GetAdapterShipperMappingsResponse>(resultContent);
                return response?.AdapterShipperMappings ?? new List<PSI.Sox.Adapter.AdapterShipperMapping>();
            }
        }

        /// <summary>
        /// Removes a single adapter-shipper mapping by its ID.
        /// </summary>
        private RemoveAdapterShipperMappingResponse RemoveAdapterShipperMapping(int adapterShipperMappingId)
        {
            var request = new RemoveAdapterShipperMappingRequest
            {
                AdapterShipperMappingId = adapterShipperMappingId,
                CompanyId = CompanyGuid
            };

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, AdminUrl + "RemoveAdapterShipperMapping"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", JWT);
                var json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var webResult = httpClient.SendAsync(requestMessage).Result;
                string resultContent = webResult.Content.ReadAsStringAsync().Result;
                return JsonHelper.Deserialize<RemoveAdapterShipperMappingResponse>(resultContent);
            }
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
