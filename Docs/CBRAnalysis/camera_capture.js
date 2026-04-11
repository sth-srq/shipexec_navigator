/**
 * Camera Capture — Use the device camera to capture a shipping label image.
 *
 * Patterns found:
 *   - Start a video stream with rear-facing camera at QHD resolution
 *   - Capture a frame as a base64 PNG
 *   - Stop the video stream
 *   - Zoom level management via localStorage
 */

/**
 * Starts the device camera video stream and attaches it to the given element.
 *
 * @param {string} videoElementId - The DOM id of the <video> element.
 * @param {object} videoModal - Object to store zoom/track state.
 */
function startVideoStream(videoElementId, videoModal) {
  if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
    console.error('Your browser does not support the getUserMedia API.');
    return;
  }

  var constraints = {
    audio: false,
    video: {
      zoom: true,
      facingMode: 'environment',
      width:  { ideal: 2560 },
      height: { ideal: 1440 }
    }
  };

  navigator.mediaDevices.getUserMedia(constraints).then(function (stream) {
    var video        = document.getElementById(videoElementId);
    video.srcObject  = stream;

    var track        = stream.getVideoTracks()[0];
    var capabilities = track.getCapabilities();

    videoModal.zoom = videoModal.zoom || {
      min:     capabilities.zoom.min,
      max:     capabilities.zoom.max,
      current: track.getSettings().zoom
    };

    var savedZoom = localStorage.getItem('zoomLevel') || track.getSettings().zoom;
    localStorage.setItem('zoomLevel', savedZoom);
    track.applyConstraints({ advanced: [{ zoom: savedZoom }] });
    videoModal.track = track;
  }).catch(function (error) {
    console.error('Error accessing the camera:', error);
  });
}

/**
 * Captures the current video frame as a base64 PNG string.
 *
 * @param {string} videoElementId - The DOM id of the <video> element.
 * @returns {Promise<string>} Resolves with the base64-encoded image data (no prefix).
 */
function captureImage(videoElementId) {
  var video        = document.getElementById(videoElementId);
  var track        = video.srcObject.getVideoTracks()[0];
  var imageCapture = new ImageCapture(track);

  return imageCapture.grabFrame().then(function (imageBitmap) {
    var canvas = document.createElement('canvas');
    var ctx    = canvas.getContext('2d');
    canvas.width  = imageBitmap.width;
    canvas.height = imageBitmap.height;
    ctx.drawImage(imageBitmap, 0, 0);

    var dataUrl = canvas.toDataURL('image/png', 1.0);
    return dataUrl.replace(/^data:.+;base64,/, '');
  });
}

/**
 * Stops all tracks on the camera stream and releases the video element.
 *
 * @param {string}  videoElementId - The DOM id of the <video> element.
 * @param {boolean} isCanceled - When true, clears the captured label image.
 */
function stopVideoStream(videoElementId, isCanceled) {
  var video = document.getElementById(videoElementId);
  if (video.srcObject) {
    video.srcObject.getTracks().forEach(function (track) { track.stop(); });
    video.srcObject = null;
  }
}
