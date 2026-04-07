/**
 * Event Handling — Keyboard event detection and element event inspection.
 */

/**
 * Checks whether a specific event handler is already attached to a jQuery element.
 *
 * @param {jQuery}   $element - The jQuery-wrapped element.
 * @param {string}   eventName - The event type (e.g. "click", "change").
 * @param {function} handler - The handler function to look for.
 * @returns {boolean} True if the handler is already attached.
 */
function isEventAttached($element, eventName, handler) {
  var events = $._data($element.get(0), 'events');
  if (!events) return false;

  var handlerStr = handler.toString();
  return events[eventName]?.some(function (ev) {
    return ev.handler.toString() === handlerStr;
  }) || false;
}
