function ClientBusinessRules() {

    this.PageLoaded = function(location) {              
    }

    this.Keystroke = function (shipmentRequest, vm, event) {
    }

    this.PreShip = function(shipmentRequest, userParams) {       
    }

    this.PostShip = function(shipmentRequest, shipmentResponse) {
    }

    this.PreProcessBatch = function(batchReference, actions, params, vm) {
    }

    this.PostProcessBatch = function(batchResponse, vm) {
    }

    this.PreVoid = function(pkg, userParams) {
    }

    this.PostVoid = function(pkg) {
    }

    this.PrePrint = function(document, localPort) {
    }

    this.PostPrint = function(document) {
    }

    this.PreLoad = function(loadValue, shipmentRequest, userParams) {  
    }

    this.PostLoad = function(loadValue, shipmentRequest) {
    }

    this.PreRate = function(shipmentRequest, userParams) {
    }

    this.PostRate = function (shipmentRequest, rateResults) {
    }

    this.PreCloseManifest = function(manifestItem, userParams) {
    }

    this.PostCloseManifest = function(manifestItem) {
    }

    this.PreTransmit = function(transmitItem, userParams) {
    }

    this.PostTransmit = function(transmitItem) {
    }

    this.PreSearchHistory = function(searchCriteria) {
    }

    this.PostSearchHistory = function(packages) {
    }

    this.NewShipment = function(shipmentRequest) { 
    }

    this.PreBuildShipment = function(shipmentRequest) {
    }

    this.PostBuildShipment = function(shipmentRequest) {
    }

    this.RepeatShipment = function(currentShipment) {
    }

    this.PreCreateGroup = function(group, userParams) {
    }

    this.PostCreateGroup = function(group) {
    }

    this.PreModifyGroup = function(group, userParams) {
    }

    this.PostModifyGroup = function(group) {
    }

    this.PreCloseGroup = function(group, userParams) {
    }

    this.PostCloseGroup = function(group) {
    }

    this.AddPackage = function(shipmentRequest, packageIndex) {
    }

    this.CopyPackage = function(shipmentRequest, packageIndex) {
    }

    this.RemovePackage = function(shipmentRequest, packageIndex) {
    }

    this.PreviousPackage = function(shipmentRequest, packageIndex) {		        
    }

    this.NextPackage = function(shipmentRequest, packageIndex) {        
    }

    this.PostSelectAddressBook = function (shipmentRequest, nameaddress) {
    }

}