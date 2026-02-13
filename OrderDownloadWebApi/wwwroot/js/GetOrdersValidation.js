(function (root, factory) {
    if (typeof module !== "undefined" && module.exports) {
        module.exports = factory();
    } else {
        root.GetOrdersValidation = factory();
    }
})(this, function () {
    "use strict";

    
    var ORDER_ID_MIN_LENGTH = 5;
    var ORDER_ID_MAX_LENGTH = 10;
    var VENDOR_ID_MIN_LENGTH = 5;
    var VENDOR_ID_MAX_LENGTH = 10;
    var CAMPAIGN_CODE_PATTERN = /^[IVPO][0-9]{2}$/i;

    function normalizeCampaignCode(value) {
        if (!value) return "";
        return value.trim().toUpperCase();
    }

    function isNumeric(value) {
        return /^[0-9]+$/.test(value);
    }

    function validateOrderNumber(value) {
        if (!value) return false;
        if (!isNumeric(value)) return false;
        return value.length >= ORDER_ID_MIN_LENGTH && value.length <= ORDER_ID_MAX_LENGTH;
    }

    function validateVendorId(value) {
        if (!value) return false;
        if (!isNumeric(value)) return false;
        return value.length >= VENDOR_ID_MIN_LENGTH && value.length <= VENDOR_ID_MAX_LENGTH;
    }

    function validateCampaignCode(value) {
        if (!value) return false;
        return CAMPAIGN_CODE_PATTERN.test(value.trim());
    }

    function validateInputs(input) {
        var normalizedCampaign = normalizeCampaignCode(input.campaignCode || "");
        return {
            orderNumberValid: validateOrderNumber(input.orderNumber || ""),
            vendorIdValid: validateVendorId(input.vendorId || ""),
            campaignCodeValid: validateCampaignCode(normalizedCampaign),
            normalizedCampaignCode: normalizedCampaign
        };
    }

    return {
        constants: {
            ORDER_ID_MIN_LENGTH: ORDER_ID_MIN_LENGTH,
            ORDER_ID_MAX_LENGTH: ORDER_ID_MAX_LENGTH,
            VENDOR_ID_MIN_LENGTH: VENDOR_ID_MIN_LENGTH,
            VENDOR_ID_MAX_LENGTH: VENDOR_ID_MAX_LENGTH
        },
        normalizeCampaignCode: normalizeCampaignCode,
        validateOrderNumber: validateOrderNumber,
        validateVendorId: validateVendorId,
        validateCampaignCode: validateCampaignCode,
        validateInputs: validateInputs
    };
});
