(function (root, factory) {
    if (typeof module !== "undefined" && module.exports) {
        module.exports = factory();
    } else {
        root.GetOrderProcessTracker = factory();
    }
})(this, function () {
    "use strict";

    var STATUS = {
        PENDING: "pending",
        IN_PROGRESS: "in-progress",
        COMPLETED: "completed",
        FAILED: "failed",
        PENDING_VALIDATION: "pending-validation"
    };

    var STEP_DEFINITIONS = [
        { id: "search-order", title: "Order lookup" },
        { id: "download-order", title: "Order download" },
        { id: "download-images", title: "Image download (File Manager)" },
        { id: "send-qr-print-central", title: "QR send to Print Central" },
        { id: "send-file-print-central", title: "File send to Print Central" }
    ];

    function getLocalizedText(localization, key, fallback) {
        if (!localization) return fallback;
        var value = localization[key];
        return value || fallback;
    }

    function buildDefaultSteps(localization) {
        return STEP_DEFINITIONS.map(function (definition) {
            return {
                id: definition.id,
                title: getLocalizedText(localization, "stepTitle." + definition.id, definition.title),
                status: STATUS.PENDING,
                detail: getLocalizedText(localization, "detail.pending", "Pending"),
                startedAt: null,
                completedAt: null
            };
        });
    }

    function nowIso(dateOverride) {
        var date = dateOverride instanceof Date ? dateOverride : new Date();
        return date.toISOString();
    }

    function findStep(steps, stepId) {
        return steps.find(function (step) { return step.id === stepId; });
    }

    function setStatus(step, status, detail, dateOverride) {
        step.status = status;
        if (detail) {
            step.detail = detail;
        }

        if (status === STATUS.IN_PROGRESS && !step.startedAt) {
            step.startedAt = nowIso(dateOverride);
        }

        if ((status === STATUS.COMPLETED || status === STATUS.FAILED || status === STATUS.PENDING_VALIDATION) && !step.completedAt) {
            step.completedAt = nowIso(dateOverride);
        }
    }

    function markStepInProgress(steps, stepId, detail, dateOverride) {
        var step = findStep(steps, stepId);
        if (!step) return;
        setStatus(step, STATUS.IN_PROGRESS, detail || "In progress", dateOverride);
    }

    function completeStep(steps, stepId, detail, dateOverride) {
        var step = findStep(steps, stepId);
        if (!step) return;
        if (!step.startedAt) {
            step.startedAt = nowIso(dateOverride);
        }
        setStatus(step, STATUS.COMPLETED, detail || "Completed", dateOverride);
    }

    function failStep(steps, stepId, detail, dateOverride) {
        var step = findStep(steps, stepId);
        if (!step) return;
        if (!step.startedAt) {
            step.startedAt = nowIso(dateOverride);
        }
        setStatus(step, STATUS.FAILED, detail || "Error", dateOverride);
    }

    function pendingValidationStep(steps, stepId, detail, dateOverride) {
        var step = findStep(steps, stepId);
        if (!step) return;
        if (!step.startedAt) {
            step.startedAt = nowIso(dateOverride);
        }
        setStatus(step, STATUS.PENDING_VALIDATION, detail || "Pending validation", dateOverride);
    }

    function completeUntil(steps, lastCompletedId, localization) {
        var reachedTarget = false;

        steps.forEach(function (step) {
            if (!reachedTarget) {
                completeStep(steps, step.id, getLocalizedText(localization, "detail.completed", "Completed"));
            }

            if (step.id === lastCompletedId) {
                reachedTarget = true;
            }
        });
    }

    function messageContainsError(message) {
        var normalizedMessage = (message || "").toLowerCase();
        return normalizedMessage.includes("error")
            || normalizedMessage.includes("exception")
            || normalizedMessage.includes("failed")
            || normalizedMessage.includes("could not")
            || normalizedMessage.includes("not be completed");
    }


    function normalizeStatus(status) {
        var normalized = (status || "").toLowerCase();
        switch (normalized) {
            case STATUS.PENDING:
            case STATUS.IN_PROGRESS:
            case STATUS.COMPLETED:
            case STATUS.FAILED:
            case STATUS.PENDING_VALIDATION:
                return normalized;
            default:
                return STATUS.PENDING;
        }
    }

    function applyProgressEvent(steps, progressEvent) {
        if (!steps || !progressEvent) {
            return false;
        }

        var step = findStep(steps, progressEvent.StepId);
        if (!step) {
            return false;
        }

        var status = normalizeStatus(progressEvent.Status);
        setStatus(step, status, progressEvent.Message || step.detail);
        return true;
    }

    function syncStepsFromMessage(steps, message, localization) {
        var normalizedMessage = (message || "").toLowerCase();

        if (normalizedMessage.includes("found successfully")) {
            completeStep(steps, "search-order", getLocalizedText(localization, "detail.orderFound", "Order found"));
        }

        if (normalizedMessage.includes("saved into work directory")) {
            completeStep(steps, "download-order", getLocalizedText(localization, "detail.orderDownloaded", "Order downloaded and saved"));
        }

        if (normalizedMessage.includes("pending images to validate")) {
            completeStep(steps, "download-images", getLocalizedText(localization, "detail.imagesDownloaded", "Images downloaded"));
            pendingValidationStep(steps, "send-file-print-central", getLocalizedText(localization, "detail.pendingImageValidation", "Pending image validation"));
        }

        if (normalizedMessage.includes("file received event sent")) {
            completeUntil(steps, "send-file-print-central", localization);
            completeStep(steps, "download-images", getLocalizedText(localization, "detail.imagesProcessed", "Images processed"));
            completeStep(steps, "send-qr-print-central", getLocalizedText(localization, "detail.qrSent", "QRs sent to Print Central"));
            completeStep(steps, "send-file-print-central", getLocalizedText(localization, "detail.fileSent", "File sent to Print Central"));
        }

        if (normalizedMessage.includes("not found in any queue")) {
            failStep(steps, "search-order", getLocalizedText(localization, "detail.orderNotFound", "Order not found"));
        }

        if (messageContainsError(normalizedMessage)) {
            var inProgressStep = steps.find(function (step) { return step.status === STATUS.IN_PROGRESS; });
            if (inProgressStep) {
                failStep(steps, inProgressStep.id, getLocalizedText(localization, "detail.errorDuringProcessing", "Error during processing"));
                return;
            }

            var pendingStep = steps.find(function (step) { return step.status === STATUS.PENDING; });
            if (pendingStep) {
                failStep(steps, pendingStep.id, getLocalizedText(localization, "detail.errorDuringProcessing", "Error during processing"));
                return;
            }

            var latestCompletedStep = steps.slice().reverse().find(function (step) { return step.status === STATUS.COMPLETED; });
            if (latestCompletedStep) {
                failStep(steps, latestCompletedStep.id, getLocalizedText(localization, "detail.errorDuringProcessing", "Error during processing"));
            }
        }
    }

    return {
        STATUS: STATUS,
        buildDefaultSteps: buildDefaultSteps,
        markStepInProgress: markStepInProgress,
        completeStep: completeStep,
        failStep: failStep,
        pendingValidationStep: pendingValidationStep,
        syncStepsFromMessage: syncStepsFromMessage,
        messageContainsError: messageContainsError,
        applyProgressEvent: applyProgressEvent
    };
});
