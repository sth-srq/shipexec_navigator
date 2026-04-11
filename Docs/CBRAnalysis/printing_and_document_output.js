/**
 * Printing and Document Output — Traveler label PDF generation, ZPL
 * printer defaults, file download, and PrePrint/PostPrint hooks.
 *
 * Patterns found:
 *   - AlstonBird: Generate a traveler label PDF via UserMethod and open
 *     it in a new browser tab as a data URI
 *   - Bulgari MX: Prepend ZPL printer-default control string to raw label data
 *   - CMR: Download a file from the server as a data-URI link click
 */

// ---------------------------------------------------------------------------
// Traveler label PDF generation (AlstonBird)
// ---------------------------------------------------------------------------

/**
 * Sends the current shipment to the server via UserMethod and opens the
 * returned PDF traveler label in a new browser tab.
 *
 * @param {object}   shipmentRequest     - The current shipment request.
 * @param {function} thinClientAPIRequest - Synchronous API request function.
 */
function printTravelerLabel(shipmentRequest, thinClientAPIRequest) {
  var data   = { Data: JSON.stringify(shipmentRequest) };
  var result = thinClientAPIRequest('UserMethod', data, false).responseJSON;
  var response     = JSON.parse(atob(result.Data));
  var pdfBase64    = response.DocumentResponses[0].PdfData[0];
  var dataURI      = 'data:application/pdf;base64,' + pdfBase64;

  window.open(dataURI, '_blank');
  alert('Delegate Label Created');
}

// ---------------------------------------------------------------------------
// ZPL printer defaults (Bulgari MX)
// ---------------------------------------------------------------------------

/**
 * Prepends ZPL printer-default commands to the raw label data so that the
 * printer initialises correctly when profile settings are not sent.
 *
 * @param {object} doc - The document object (has DocumentSymbol and RawData).
 * @param {string} expectedSymbol - The document symbol to match (e.g. "UPSAPI.UPS.PACKAGE_LABEL.STANDARD").
 */
function prependZplDefaults(doc, expectedSymbol) {
  if (doc.DocumentSymbol !== expectedSymbol || !doc.RawData) return;

  var printerDefaults = '^XA^LH0,0^XSY,Y^MD30^XZ\n';
  var originalRaw     = atou(doc.RawData);
  doc.RawData[0]      = utoa(printerDefaults + originalRaw);
}

// ---------------------------------------------------------------------------
// File download (CMR)
// ---------------------------------------------------------------------------

/**
 * Downloads a file from the server by requesting its content via
 * UserMethod and triggering a browser download.
 *
 * @param {string}   fileName   - The name of the file to download.
 * @param {function} httpClient - The thin-client HTTP client function.
 */
function downloadFile(fileName, httpClient) {
  var payload = { Action: 'downloadFile', Data: fileName };
  var data    = { UserContext: undefined, Data: JSON.stringify(payload) };

  httpClient('UserMethod', data).then(function (ret) {
    if (ret.ErrorCode !== 0) return;

    var fileData     = JSON.parse(atob(ret.Data));
    var linkData     = 'data:' + fileData.fileType + ';base64,' + fileData.encodedFile;
    var downloadLink = document.createElement('a');

    downloadLink.style.display = 'none';
    downloadLink.download      = fileData.fileName;
    downloadLink.href          = linkData;

    document.body.appendChild(downloadLink);
    downloadLink.click();
    document.body.removeChild(downloadLink);
  });
}
