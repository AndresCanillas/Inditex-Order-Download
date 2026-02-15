const tracker = require("../OrderDownloadWebApi/wwwroot/js/GetOrderProcessTracker");

describe("GetOrderProcessTracker", () => {
  test("buildDefaultSteps creates all expected ordered phases in pending", () => {
    const steps = tracker.buildDefaultSteps();

    expect(steps.map((step) => step.id)).toEqual([
      "search-order",
      "download-order",
      "download-images",
      "send-qr-print-central",
      "send-file-print-central"
    ]);

    expect(steps.every((step) => step.status === tracker.STATUS.PENDING)).toBe(true);
  });

  test("markStepInProgress and completeStep update statuses and timestamps", () => {
    const steps = tracker.buildDefaultSteps();
    const start = new Date("2026-01-01T10:00:00.000Z");
    const end = new Date("2026-01-01T10:00:05.000Z");

    tracker.markStepInProgress(steps, "search-order", "Buscando pedido", start);
    tracker.completeStep(steps, "search-order", "Pedido encontrado", end);

    expect(steps[0].status).toBe(tracker.STATUS.COMPLETED);
    expect(steps[0].startedAt).toBe(start.toISOString());
    expect(steps[0].completedAt).toBe(end.toISOString());
    expect(steps[0].detail).toBe("Pedido encontrado");
  });

  test("syncStepsFromMessage marks steps based on successful workflow messages", () => {
    const steps = tracker.buildDefaultSteps();

    tracker.syncStepsFromMessage(
      steps,
      "Order number (12345) found successfully in ZARA queue.\nOrder 12345 was saved into work directory.\nFile received event sent for order 12345"
    );

    const statuses = Object.fromEntries(steps.map((step) => [step.id, step.status]));

    expect(statuses["search-order"]).toBe(tracker.STATUS.COMPLETED);
    expect(statuses["download-order"]).toBe(tracker.STATUS.COMPLETED);
    expect(statuses["download-images"]).toBe(tracker.STATUS.COMPLETED);
    expect(statuses["send-qr-print-central"]).toBe(tracker.STATUS.COMPLETED);
    expect(statuses["send-file-print-central"]).toBe(tracker.STATUS.COMPLETED);
  });

  test("syncStepsFromMessage keeps file-send step pending validation when message indicates pending images", () => {
    const steps = tracker.buildDefaultSteps();

    tracker.syncStepsFromMessage(
      steps,
      "Order number (12345) found successfully in ZARA queue.\nOrder 12345 has pending images to validate."
    );

    const fileStep = steps.find((step) => step.id === "send-file-print-central");

    expect(fileStep.status).toBe(tracker.STATUS.PENDING_VALIDATION);
    expect(fileStep.detail).toMatch(/validaci[oó]n|validation/i);
  });

  test("failStep marks the current phase as failed", () => {
    const steps = tracker.buildDefaultSteps();

    tracker.markStepInProgress(steps, "download-order", "Descargando pedido");
    tracker.failStep(steps, "download-order", "No se pudo conectar");

    const step = steps.find((x) => x.id === "download-order");
    expect(step.status).toBe(tracker.STATUS.FAILED);
    expect(step.detail).toBe("No se pudo conectar");
  });

  test("syncStepsFromMessage marks next phase as failed when message mixes success and error", () => {
    const steps = tracker.buildDefaultSteps();

    tracker.syncStepsFromMessage(
      steps,
      "Order number (12345) found successfully in ZARA queue. Error while saving order file."
    );

    const searchStep = steps.find((x) => x.id === "search-order");
    const downloadStep = steps.find((x) => x.id === "download-order");

    expect(searchStep.status).toBe(tracker.STATUS.COMPLETED);
    expect(downloadStep.status).toBe(tracker.STATUS.FAILED);
  });


  test("applyProgressEvent updates the requested step using typed stream event data", () => {
    const steps = tracker.buildDefaultSteps();

    const wasUpdated = tracker.applyProgressEvent(steps, {
      StepId: "download-order",
      Status: "in-progress",
      Message: "Saving order into work directory"
    });

    const step = steps.find((x) => x.id === "download-order");
    expect(wasUpdated).toBe(true);
    expect(step.status).toBe(tracker.STATUS.IN_PROGRESS);
    expect(step.detail).toBe("Saving order into work directory");
  });

  test("applyProgressEvent ignores unknown step ids", () => {
    const steps = tracker.buildDefaultSteps();

    const wasUpdated = tracker.applyProgressEvent(steps, {
      StepId: "unknown-step",
      Status: "completed",
      Message: "Done"
    });

    expect(wasUpdated).toBe(false);
    expect(steps.every((x) => x.status === tracker.STATUS.PENDING)).toBe(true);
  });

  test("messageContainsError detects known error patterns", () => {
    expect(tracker.messageContainsError("Error: timeout while contacting server")).toBe(true);
    expect(tracker.messageContainsError("Order found successfully in queue")).toBe(false);
  });
});
