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
    public class ClientRequestGenerator : RequestGenerationBase<GetClientsResponse, 
        GetCompanyClientsRequest, 
        GetClientRequest, 
        GetClientResponse, 
        AddClientRequest, 
        AddClientResponse,
        UpdateClientRequest,
        UpdateClientResponse,
        RemoveClientRequest,
        RemoveClientResponse,
        Client>
    {

        public ClientRequestGenerator(string adminUrl, Guid companyGuid, string jwt = null) :
            base(adminUrl, companyGuid, "GetCompanyClients", "GetClient", "AddClient", "UpdateClient", "RemoveClient", "Client", jwt)
        {
        }

        public override GetCompanyClientsRequest ModifyGetAllRequest(GetCompanyClientsRequest request)
        {
            request.CompanyId = CompanyGuid;
            return request;
        }

        public override GetClientRequest ModifyGetRequestWithId(GetClientRequest request, int id)
        {
            request.CompanyId = CompanyGuid;
            request.ClientId = id;
            return request;
        }

        public AddClientResponse Add(Client client, Guid? siteId = null)
        {
            AddClientRequest addClientRequest = new AddClientRequest();
            addClientRequest.Client = client;
            addClientRequest.Client.CompanyId = CompanyGuid;
            addClientRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) { addClientRequest.Client.SiteId = siteId.Value; addClientRequest.SiteId = siteId.Value; }

            return BaseAdd(addClientRequest);
        }

        public AddClientResponse Add(AddClientRequest addClientRequest)
        {
            return Add(addClientRequest.Client);
        }




        public UpdateClientResponse Update(Client client, Guid? siteId = null)
        {
            UpdateClientRequest updateClientRequest = new UpdateClientRequest();
            updateClientRequest.Client = client;
            updateClientRequest.Client.CompanyId = CompanyGuid;
            updateClientRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) { updateClientRequest.Client.SiteId = siteId.Value; updateClientRequest.SiteId = siteId.Value; }

            return BaseUpdate(updateClientRequest);
        }

        public UpdateClientResponse Update(UpdateClientRequest updateClientRequest)
        {
            return Update(updateClientRequest.Client);
        }

        public RemoveClientResponse Remove(int clientId, Guid? siteId = null)
        {
            RemoveClientRequest removeClientRequest = new RemoveClientRequest();
            removeClientRequest.ClientId = clientId;
            removeClientRequest.CompanyId = CompanyGuid;
            if (siteId.HasValue) removeClientRequest.SiteId = siteId.Value;

            return BaseRemove(removeClientRequest);
        }

        public RemoveClientResponse Remove(RemoveClientRequest removeClientRequest)
        {
            return Remove(removeClientRequest.ClientId);
        }

        public override bool HasSameId(Client current, Client modified)
        {
            return current.Id == modified.Id;
        }

        public override bool ShouldUpdate(Client current, Client modified)
        {
            var areTheyTheSame = false;

            areTheyTheSame = current.Name == modified.Name && current.Id == modified.Id;

            return !areTheyTheSame;
        }


        public override AddClientRequest InitializeAddRequest(ref AddClientRequest addRequest, Variance variance)
        {
            addRequest.Client = (Client)variance.NewObject;
            addRequest.Client.CompanyId = CompanyGuid;
            addRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) { addRequest.Client.SiteId = variance.ParentSiteId.Value; addRequest.SiteId = variance.ParentSiteId.Value; }

            return addRequest;
        }

        public override UpdateClientRequest InitializeUpdateRequest(ref UpdateClientRequest updateRequest, Variance variance)
        {
            updateRequest.Client = (Client)variance.NewObject;
            updateRequest.Client.CompanyId = CompanyGuid;
            updateRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) { updateRequest.Client.SiteId = variance.ParentSiteId.Value; updateRequest.SiteId = variance.ParentSiteId.Value; }

            return updateRequest;
        }

        public override RemoveClientRequest InitializeRemoveRequest(ref RemoveClientRequest removeRequest, Variance variance)
        {
            removeRequest.ClientId = ((Client)variance.OriginalObject).Id;
            removeRequest.CompanyId = CompanyGuid;
            if (variance.ParentSiteId.HasValue) removeRequest.SiteId = variance.ParentSiteId.Value;

            return removeRequest;
        }

    }

    public class GetClientsResponse : GetResponseBase
    {
        public List<Client> Clients { get; set; }
    }
}
