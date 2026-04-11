/**
 * File Upload — FTP file retrieval, carrier label image upload,
 * and guide file download/upload.
 */

/**
 * Uploads a carrier label image file selected via a file input.
 * Reads the file as base64 and triggers a load with the label data.
 *
 * @param {string}   fileInputSelector - jQuery selector for the file input.
 * @param {function} onReady - Callback receiving (base64Data) when the file is read.
 */
function uploadCarrierLabelFile(fileInputSelector, onReady) {
  var $input    = $(fileInputSelector);
  var imageFile = $input.prop('files')[0];

  if (!imageFile) {
    $input.click();
    return;
  }

  var reader    = new FileReader();
  reader.readAsDataURL(imageFile);
  reader.onload = function () {
    var base64Data = this.result.replace(/^data:.+;base64,/, '');
    onReady(base64Data);
    $input.val(null);
  };
}

/**
 * Downloads a file from the server via UserMethod and triggers a browser download.
 *
 * @param {string}   fileName - The file name/key to request.
 * @param {function} httpClient - The HTTP client function.
 */
function downloadServerFile(fileName, httpClient) {
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

/**
 * Uploads a replacement file to the server by reading it as base64
 * and sending it via UserMethod.
 *
 * @param {File}     file - The File object to upload.
 * @param {string}   serverKey - The server-side key/name for the file.
 * @param {function} httpClient - The HTTP client function.
 */
function uploadReplacementFile(file, serverKey, httpClient) {
  var reader    = new FileReader();
  reader.onload = function () {
    var request = {
      Key:   serverKey,
      Value: JSON.stringify({
        fileType:    file.type,
        fileName:    file.name,
        encodedFile: this.result.split(',')[1]
      })
    };
    var payload = { Action: 'updateFile', Data: request };
    var data    = { UserContext: undefined, Data: JSON.stringify(payload) };
    httpClient('UserMethod', data);
  };
  reader.readAsDataURL(file);
}

/**
 * Binds a change handler to a paperless file input, reading the selected
 * file as base64 and storing it on the view model.
 *
 * @param {string} fileInputSelector - jQuery selector for the file input.
 * @param {object} vmInstance - The view model to store the paperless data on.
 */
function bindPaperlessFileInput(fileInputSelector, vmInstance) {
  $('body').on('change', fileInputSelector, vmInstance, function (e) {
    var file = e.target.files[0];
    if (!file) {
      e.data.paperless = null;
      return;
    }
    var reader    = new FileReader();
    reader.readAsDataURL(file);
    reader.onloadend = function () {
      e.data.paperless = { fileName: file.name, fileData: this.result.split(',')[1] };
    };
  });
}
