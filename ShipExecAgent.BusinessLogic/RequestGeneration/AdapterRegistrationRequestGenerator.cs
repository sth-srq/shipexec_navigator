using PSI.Sox.Wcf.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ShipExecAgent.BusinessLogic.EntityComparison;
using PSI.Sox.Wcf;
using ShipExecAgent.BusinessLogic.ResponseModel;
using PSI.Sox;

namespace ShipExecAgent.BusinessLogic.RequestGeneration
{
    public class AdapterRegistrationRequestGenerator : RequestGenerationBase<GetAdapterRegistrationsResponse,
        GetAdapterRegistrationsRequest,
        GetAdapterRegistrationRequest,
        GetAdapterRegistrationResponse,
        AddAdapterRegistrationRequest,
        AddAdapterRegistrationResponse,
        UpdateAdapterRegistrationRequest,
        UpdateAdapterRegistrationResponse,
        RemoveAdapterRegistrationRequest,
        RemoveAdapterRegistrationResponse,
        AdapterRegistration>
    {

        public AdapterRegistrationRequestGenerator(string adminUrl, Guid companyGuid, string jwt = null) :
            base(adminUrl, companyGuid, "GetAdapterRegistrations", "GetAdapterRegistration", "AddAdapterRegistration", "UpdateAdapterRegistration", "RemoveAdapterRegistration", "AdapterRegistration", jwt)
        {
        }

        public override GetAdapterRegistrationsRequest ModifyGetAllRequest(GetAdapterRegistrationsRequest request)
        {
            request.CompanyId = CompanyGuid;
            request.SearchCriteria = GetEmptySearchCriteria();
            return request;
        }

        public override GetAdapterRegistrationRequest ModifyGetRequestWithId(GetAdapterRegistrationRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.AdapterRegistrationId = id;
            //request.Id = id;
            return request;
        }

        public AddAdapterRegistrationResponse Add(AdapterRegistration adapterRegistration)
        {
            AddAdapterRegistrationRequest addAdapterRegistrationRequest = new AddAdapterRegistrationRequest();
            addAdapterRegistrationRequest.AdapterRegistration = adapterRegistration;
            //addAdapterRegistrationRequest.CompanyId = CompanyGuid;

            return BaseAdd(addAdapterRegistrationRequest);
        }

        public AddAdapterRegistrationResponse Add(AddAdapterRegistrationRequest addAdapterRegistrationRequest)
        {
            return Add(addAdapterRegistrationRequest.AdapterRegistration);
        }




        public UpdateAdapterRegistrationResponse Update(AdapterRegistration adapterRegistration)
        {
            UpdateAdapterRegistrationRequest updateAdapterRegistrationRequest = new UpdateAdapterRegistrationRequest();
            updateAdapterRegistrationRequest.AdapterRegistration = adapterRegistration;
            //updateAdapterRegistrationRequest.CompanyId = CompanyGuid;

            return BaseUpdate(updateAdapterRegistrationRequest);
        }

        public UpdateAdapterRegistrationResponse Update(UpdateAdapterRegistrationRequest updateAdapterRegistrationRequest)
        {
            return Update(updateAdapterRegistrationRequest.AdapterRegistration);
        }

        public RemoveAdapterRegistrationResponse Remove(int adapterRegistrationId)
        {
            RemoveAdapterRegistrationRequest removeAdapterRegistrationRequest = new RemoveAdapterRegistrationRequest();
            removeAdapterRegistrationRequest.AdapterRegistrationId = adapterRegistrationId;
            removeAdapterRegistrationRequest.CompanyId = CompanyGuid;

            return BaseRemove(removeAdapterRegistrationRequest);
        }

        public RemoveAdapterRegistrationResponse Remove(RemoveAdapterRegistrationRequest removeAdapterRegistrationRequest)
        {
            return Remove(removeAdapterRegistrationRequest.AdapterRegistrationId);
        }

        public override bool HasSameId(AdapterRegistration current, AdapterRegistration modified)
        {
            return current.Id == modified.Id;
        }

        public override bool ShouldUpdate(AdapterRegistration current, AdapterRegistration modified)
        {
            var json1 = JsonHelper.Serialize(current);
            var json2 = JsonHelper.Serialize(modified);
            return json1 != json2;
        }


        public override AddAdapterRegistrationRequest InitializeAddRequest(ref AddAdapterRegistrationRequest addRequest, Variance variance)
        {
            addRequest.AdapterRegistration = (AdapterRegistration)variance.NewObject;
            //addRequest.CompanyId = CompanyGuid;

            return addRequest;
        }

        public override UpdateAdapterRegistrationRequest InitializeUpdateRequest(ref UpdateAdapterRegistrationRequest updateRequest, Variance variance)
        {
            updateRequest.AdapterRegistration = (AdapterRegistration)variance.NewObject;
            //updateRequest.CompanyId = CompanyGuid;

            return updateRequest;
        }

        public override RemoveAdapterRegistrationRequest InitializeRemoveRequest(ref RemoveAdapterRegistrationRequest removeRequest, Variance variance)
        {
            removeRequest.AdapterRegistrationId = ((AdapterRegistration)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;

            return removeRequest;
        }

    }

    public class GetAdapterRegistrationsResponse : GetResponseBase
    {
        public List<AdapterRegistration> AdapterRegistrations { get; set; }
    }

    }
