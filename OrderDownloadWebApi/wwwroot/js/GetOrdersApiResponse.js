(function (root, factory) {
    if (typeof module !== "undefined" && module.exports) {
        module.exports = factory();
    } else {
        root.GetOrdersApiResponse = factory();
    }
}(typeof self !== "undefined" ? self : this, function () {
    "use strict";

    async function parse(response) {
        if (!response) {
            return { ok: false, result: null, message: "" };
        }

        if (typeof response.text === "function") {
            var body = await response.text();
            var parsedResult = tryParseJson(body);
            return {
                ok: response.ok !== false,
                result: parsedResult,
                message: parsedResult && parsedResult.Message ? parsedResult.Message : ""
            };
        }

        return {
            ok: response.Success === true,
            result: response,
            message: response.Message ? response.Message : ""
        };
    }

    function tryParseJson(body) {
        if (!body || !String(body).trim().length) {
            return null;
        }

        try {
            return JSON.parse(body);
        } catch (error) {
            return null;
        }
    }

    return {
        parse: parse
    };
}));
