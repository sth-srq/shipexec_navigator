/**
 * Batch Voiding and History Operations — PreSearchHistory filters and
 * history-only data access.
 *
 * The vast majority of implementations are empty stubs.  The only meaningful
 * patterns are user-scoped history filtering.
 */

/**
 * Filters the history search so only the current user's shipments are shown.
 *
 * @param {object} searchCriteria - The search criteria object with a WhereClauses array.
 * @param {string} userId - The current user's ID.
 * @param {number} [operator=0] - The comparison operator (0 = equals, 5 = contains).
 */
function filterHistoryByUser(searchCriteria, userId, operator) {
  searchCriteria.WhereClauses.push({
    FieldName: 'UserId',
    FieldValue: userId,
    Operator: operator || 0
  });
}

/**
 * Returns the user context from whichever property is populated on the view model.
 *
 * @param {object} vmInstance - The current view model.
 * @returns {object|undefined} The user context object.
 */
function getUserContext(vmInstance) {
  return vmInstance.userContext || vmInstance.UserInformation;
}

/**
 * Generic comparator factory for sorting arrays of objects by a given key.
 *
 * @param {string} key - The property name to sort by.
 * @returns {function} A comparison function for Array.sort().
 */
function compareByKey(key) {
  return function (a, b) {
    if (a[key] > b[key]) return 1;
    if (a[key] < b[key]) return -1;
    return 0;
  };
}
