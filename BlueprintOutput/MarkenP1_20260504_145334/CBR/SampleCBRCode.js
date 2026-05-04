function ClientBusinessRules() {

    this.PageLoaded = function(location) {        
        if (location == '/')
            this.Home();
        else if (location === '/shipping')
            this.Shipping();
        else if (location === '/history')
            this.History();
    }

    this.NewShipment = function(shipmentRequest) {
        var userInformation = this.vm.profile.UserInformation;
        if (userInformation.Address != undefined) {
            shipmentRequest.Packages[this.vm.packageIndex].ShipperReference = client.getValueByKey('custom1', userInformation.Address.CustomData)
        }
    }
    
    this.PreSearchHistory = function(searchCriteria) {        
        var userContext = this.GetUserContext();
        searchCriteria.WhereClauses.push({ FieldName: 'UserId', FieldValue: userContext.UserId, Operator: 0 });
    }

    this.GetUserContext = function () {
        if (this.vm.userContext) return this.vm.userContext;
        if (this.vm.UserInformation) return this.vm.UserInformation;
    }

    this.Home = function () {
        userMethodExample();

        function userMethodExample() {            
            $('body').delegate('a:contains("ShipExec™ Thin Client")', 'click', function (e) {
                e.preventDefault();
                e.stopPropagation();            
                e.stopImmediatePropagation();

                var payload = JSON.stringify({ Action: 'ExportCSV' });
                var data = { Data: btoa(payload), UserContext: client.userContext };
                client.thinClientAPIRequest('UserMethod', data, true).done(function (data) {
                    debugger;
                })
                debugger;
            });            
        }
    }


    this.History = function () {
        var addressBookEntries = this.GetAddressBookEntries('Return', client.userContext);
    }

    this.Shipping = function () {
        var addressBookEntries = this.GetAddressBookEntries('Return', this.vm.UserInformation);
    }

    this.GetAddressBookEntries = function (addressBook, context) {
        var addressBookName = addressBook;
        if (typeof addressBook === 'string') {
            addressBook = this.GetAddressBook(addressBookName, context);
        }

        var addressBookEntries;
        if (addressBook != undefined) {
            var searchCriteria = { OrderByClauses: [{ FieldName: "Code", Direction: "asc", FieldType: 3 }] };
            var data = { AddressBookIds: [addressBook.Id], SearchCriteria: searchCriteria, CompanyId: context.CompanyId, SiteId: context.SiteId, UserId: context.UserId };
            this.thinClientAPIRequest('GetAddressBookEntries', data, false).done(function (response) {
                if (response.ErrorCode != 0) {
                    client.alert.Danger(response.ErrorMessage);
                } else {
                    if (response.TotalRecords == 0) {
                        client.alert.Danger('Address Book Entries: No entries found in "' + addressBookName + '" address book');
                    } else {
                        addressBookEntries = response.AddressBookEntries;
                    }
                }
            });            
        };
        return addressBookEntries;
    }

    this.GetAddressBook = function (addressBookName, context) {
        var addressBook;
        var searchCriteria = { WhereClauses: [{ FieldName: "Name", FieldValue: addressBookName, Operator: 0 }] };
        var data = { SearchCriteria: searchCriteria, CompanyId: context.CompanyId, SiteId: context.SiteId, UserId: context.UserId };
        this.thinClientAPIRequest('GetAddressBooks', data, false).done(function (response) {
            if (response.ErrorCode != 0) {
                client.alert.Danger(response.ErrorMessage);
            } else {
                if (response.TotalRecords == 0) {
                    client.alert.Danger('Address Book: "' + addressBookName + '" does not exist');                
                } else {
                    addressBook = response.AddressBooks[0];
                }
            } 
        });
        return addressBook;
    }

    this.thinClientAPIRequest = function (method, data, isAsync) {
        var shipExecServiceUrl = client.config.ShipExecServiceUrl;
        var authorizationToken = client.getAuthorizationToken();
        return $.post({ url: shipExecServiceUrl + '/' + method, data: data, async: isAsync, headers: authorizationToken });
    }

    var client = {
        config: $.getJSON({url:'config.json', async: false}).responseJSON,
        getAuthorizationToken: function () {
            if (JSON.parse(window.localStorage.getItem("TCToken")))
                return { 'Authorization': 'Bearer ' + JSON.parse(window.localStorage.getItem("TCToken")).access_token };
            return '';
        },
        getUserContext: function () {
            var userContextUrl = client.config.ShipExecServiceUrl.replace('ShippingService', 'usercontext/GET');
            var authorizationToken = client.getAuthorizationToken();
            return $.get({url: userContextUrl, headers: authorizationToken, async: false}).responseJSON;;
        },
        thinClientAPIRequest: function (method, data, isAsync) {
            var shipExecServiceUrl = client.config.ShipExecServiceUrl;
            var authorizationToken = client.getAuthorizationToken();
            return $.post({ url: shipExecServiceUrl + '/' + method, data: data, async: isAsync, headers: authorizationToken });
        },
        getValueByKey: function (key, array) {
            for (var index in array){
                if (array[index].Key.toLowerCase() == key.toLowerCase())
                    return array[index].Value;
            }
            return;
        },
        sleep: function (ms) {
            var millisecondsToWait = new Date().getTime() + ms;
            while ( new Date().getTime() < millisecondsToWait ){}
        },
        dismiss: $('body').delegate('*', 'focus', function () { $('div[role="alert"]:visible').hide(); }),
        alert: (function () {
            var closealert = $('<button class="close" />').append('<span>&times;</span>');
            var uibalert = $('<div hidden class="alert alert-dismissible alert-bottom ng-scope" role="alert" style="z-index: 2000;" />');
            uibalert.append(closealert).append('<span style="padding-right: 10px" />');

            $('body').append(uibalert);
            closealert.on('click', function () { uibalert.hide(); })

            return {
                Info: function (msg) {
                    uibalert.removeClass('alert-success alert-danger');
                    uibalert.find('span:eq(1)').text(msg);
                    uibalert.addClass('alert-info').show();
                },
                Success: function (msg) {
                    uibalert.removeClass('alert-info alert-danger');
                    uibalert.find('span:eq(1)').text(msg);
                    uibalert.addClass('alert-success').show();
                },
                Danger: function (msg) {
                    uibalert.removeClass('alert-info alert-success');
                    uibalert.find('span:eq(1)').text(msg);
                    uibalert.addClass('alert-danger').show();
                }
            }
        })()
    };
    client.userContext = client.getUserContext();

    //--------------------------------------------------------
    // Custom Functions
    //--------------------------------------------------------
    //
    //--------------------------------------------------------
    // SORT SERVICES BY ABC ORDER
    // this.vm.profile.Services.sort(compare('Name'));

    //function compare(key) {
    //    return function (a, b) {
    //        if (a[key] > b[key]) return 1;
    //        if (a[key] < b[key]) return -1;
    //        return 0;
    //    }
    //}
    //---------------------------------------------------------
    //
    //--------------------------------------------------------
    // SET FOCUS TO THE LOAD
    // $('input[type=text][ng-model="vm.loadValue"]').focus();
    //---------------------------------------------------------

}