(function (root, factory) {
    if (typeof module !== "undefined" && module.exports) {
        module.exports = factory();
    } else {
        root.GetOrdersRequestState = factory();
    }
})(this, function () {
    "use strict";

    function createState(buttonElement) {
        var isRequestInProgress = false;

        function setButtonDisabled(isDisabled) {
            if (!buttonElement || typeof buttonElement.prop !== "function") {
                return;
            }
            buttonElement.prop("disabled", !!isDisabled);
        }

        return {
            canSubmit: function () {
                return !isRequestInProgress;
            },
            begin: function () {
                if (isRequestInProgress) {
                    return false;
                }

                isRequestInProgress = true;
                setButtonDisabled(true);
                return true;
            },
            end: function () {
                isRequestInProgress = false;
                setButtonDisabled(false);
            },
            isInProgress: function () {
                return isRequestInProgress;
            }
        };
    }

    return {
        createState: createState
    };
});
