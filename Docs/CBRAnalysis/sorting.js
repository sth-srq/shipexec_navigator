/**
 * Sorting — Array deduplication, CSV string utilities, and generic
 * comparators for sorting lists by key.
 *
 * These utilities are part of the "Tools" namespace and are used
 * throughout the CBR codebase for managing order-number lists and
 * sorting shippers/rate results.
 */

// ---------------------------------------------------------------------------
// Unique array / CSV string utilities
// ---------------------------------------------------------------------------

/**
 * Splits a delimited string into an array of unique, trimmed, non-empty values.
 *
 * @param {string} inputString    - The delimited string.
 * @param {string} [delimiter=","] - The delimiter character.
 * @returns {string[]} Array of unique values.
 */
function getUniqueArrayFromString(inputString, delimiter) {
  delimiter = delimiter || ',';
  var parts = inputString.split(delimiter).filter(function (s) {
    return s.trim().length > 0;
  });
  return parts.filter(function (value, index, self) {
    return self.indexOf(value) === index;
  });
}

/**
 * Converts an array of strings into a comma-separated string with
 * duplicates removed.
 *
 * @param {string[]} inputArray - The source array.
 * @returns {string} A comma-separated string (trailing comma included).
 */
function getUniqueCSVStringFromArray(inputArray) {
  var unique = inputArray.filter(function (value, index, self) {
    return self.indexOf(value) === index;
  });

  var result = '';
  unique.forEach(function (item) {
    if (item.trim().length > 0) {
      result += item.trim() + ',';
    }
  });
  return result;
}

/**
 * Deduplicates a comma-separated string, returning a unique CSV string.
 *
 * @param {string} inputString - The comma-separated string.
 * @returns {string} A deduplicated comma-separated string.
 */
function getUniqueCSVStringFromString(inputString) {
  var uniqueArray = getUniqueArrayFromString(inputString);
  return getUniqueCSVStringFromArray(uniqueArray);
}

// ---------------------------------------------------------------------------
// Generic comparators
// ---------------------------------------------------------------------------

/**
 * Returns a comparator function that sorts objects by a given key.
 * Useful for sorting arrays of shippers, services, etc.
 *
 * @param {string} key - The property name to sort by.
 * @returns {function} A comparator suitable for Array.prototype.sort().
 *
 * @example
 *   vm.profile.Shippers.sort(compareByKey('Name'));
 */
function compareByKey(key) {
  return function (a, b) {
    if (a[key] > b[key]) return 1;
    if (a[key] < b[key]) return -1;
    return 0;
  };
}

// ---------------------------------------------------------------------------
// Rate-result selection by SortIndex (AlstonBird)
// ---------------------------------------------------------------------------

/**
 * Finds the rate result with SortIndex === 0 and returns its service.
 * Used by mailroom profiles to auto-select the recommended service.
 *
 * @param {object[]} rateResults - The array of rate results from the server.
 * @returns {object|undefined} The service object { Symbol } or undefined.
 */
function selectServiceBySortIndex(rateResults) {
  for (var i = 0; i < rateResults.length; i++) {
    if (rateResults[i].SortIndex === 0) {
      return rateResults[i].PackageDefaults.Service;
    }
  }
}
