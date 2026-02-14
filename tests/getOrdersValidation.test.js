const validation = require("../OrderDownloadWebApi/wwwroot/js/GetOrdersValidation");

describe("GetOrdersValidation", () => {
  test("validates order number (5 to 10 digits, no leading zero)", () => {
    expect(validation.validateOrderNumber("1234567890")).toBe(true);
    expect(validation.validateOrderNumber("0123456789")).toBe(false);
    expect(validation.validateOrderNumber("12345")).toBe(true);
    expect(validation.validateOrderNumber("1234")).toBe(false);
    expect(validation.validateOrderNumber("12345678901")).toBe(false);
  });

  test("validates vendor id (5 to 10 digits, leading zeros allowed)", () => {
    expect(validation.validateVendorId("00001")).toBe(true);
    expect(validation.validateVendorId("12345")).toBe(true);
    expect(validation.validateVendorId("1234567890")).toBe(true);
    expect(validation.validateVendorId("1234")).toBe(false);
    expect(validation.validateVendorId("12345678901")).toBe(false);
  });

  test("validates campaign code with seasons and normalizes to uppercase", () => {
    expect(validation.validateCampaignCode("I25")).toBe(true);
    expect(validation.validateCampaignCode("v25")).toBe(true);
    expect(validation.validateCampaignCode("P25")).toBe(true);
    expect(validation.validateCampaignCode("O25")).toBe(true);
    expect(validation.validateCampaignCode("X25")).toBe(false);
    expect(validation.normalizeCampaignCode("v25")).toBe("V25");
  });

  test("validateInputs returns normalized campaign code", () => {
    const result = validation.validateInputs({
      orderNumber: "1234567890",
      campaignCode: "o25",
      vendorId: "00001"
    });

    expect(result.orderNumberValid).toBe(true);
    expect(result.campaignCodeValid).toBe(true);
    expect(result.vendorIdValid).toBe(true);
    expect(result.normalizedCampaignCode).toBe("O25");
  });
});
