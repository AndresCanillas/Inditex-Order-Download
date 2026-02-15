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
        { id: "search-order", title: "Búsqueda del pedido" },
        { id: "download-order", title: "Descarga del pedido" },
        { id: "download-images", title: "Descarga de imágenes (File Manager)" },
        { id: "send-qr-print-central", title: "Envío de QRs a Print Central" },
        { id: "send-file-print-central", title: "Envío de archivo a Print Central" }
    ];

    function buildDefaultSteps() {
        return STEP_DEFINITIONS.map(function (definition) {
            return {
                id: definition.id,
                title: definition.title,
                status: STATUS.PENDING,
                detail: "Pendiente",
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
        setStatus(step, STATUS.IN_PROGRESS, detail || "En proceso", dateOverride);
    }

    function completeStep(steps, stepId, detail, dateOverride) {
        var step = findStep(steps, stepId);
        if (!step) return;
        if (!step.startedAt) {
            step.startedAt = nowIso(dateOverride);
        }
        setStatus(step, STATUS.COMPLETED, detail || "Completado", dateOverride);
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
        setStatus(step, STATUS.PENDING_VALIDATION, detail || "Pendiente de validación", dateOverride);
    }

    function completeUntil(steps, lastCompletedId) {
        var reachedTarget = false;

        steps.forEach(function (step) {
            if (!reachedTarget) {
                completeStep(steps, step.id, "Completado");
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

    function syncStepsFromMessage(steps, message) {
        var normalizedMessage = (message || "").toLowerCase();

        if (normalizedMessage.includes("found successfully")) {
            completeStep(steps, "search-order", "Pedido encontrado");
        }

        if (normalizedMessage.includes("saved into work directory")) {
            completeStep(steps, "download-order", "Pedido descargado y guardado");
        }

        if (normalizedMessage.includes("pending images to validate")) {
            completeStep(steps, "download-images", "Imágenes descargadas");
            pendingValidationStep(steps, "send-file-print-central", "Pendiente por validación de imagen");
        }

        if (normalizedMessage.includes("file received event sent")) {
            completeUntil(steps, "send-file-print-central");
            completeStep(steps, "download-images", "Imágenes procesadas");
            completeStep(steps, "send-qr-print-central", "QRs enviados a Print Central");
            completeStep(steps, "send-file-print-central", "Archivo enviado a Print Central");
        }

        if (normalizedMessage.includes("not found in any queue")) {
            failStep(steps, "search-order", "Pedido no encontrado");
        }

        if (messageContainsError(normalizedMessage)) {
            var inProgressStep = steps.find(function (step) { return step.status === STATUS.IN_PROGRESS; });
            if (inProgressStep) {
                failStep(steps, inProgressStep.id, "Error durante el proceso");
                return;
            }

            var pendingStep = steps.find(function (step) { return step.status === STATUS.PENDING; });
            if (pendingStep) {
                failStep(steps, pendingStep.id, "Error durante el proceso");
                return;
            }

            var latestCompletedStep = steps.slice().reverse().find(function (step) { return step.status === STATUS.COMPLETED; });
            if (latestCompletedStep) {
                failStep(steps, latestCompletedStep.id, "Error durante el proceso");
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
