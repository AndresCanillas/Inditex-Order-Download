const requestState = require("../OrderDownloadWebApi/wwwroot/js/GetOrdersRequestState");

describe("GetOrdersRequestState", () => {
  function createButtonMock() {
    return { prop: jest.fn() };
  }

  test("begin deshabilita botón y bloquea reenvío", () => {
    const button = createButtonMock();
    const state = requestState.createState(button);

    expect(state.canSubmit()).toBe(true);
    expect(state.begin()).toBe(true);
    expect(state.canSubmit()).toBe(false);
    expect(state.isInProgress()).toBe(true);
    expect(state.begin()).toBe(false);

    expect(button.prop).toHaveBeenCalledWith("disabled", true);
    expect(button.prop).toHaveBeenCalledTimes(1);
  });

  test("end habilita botón nuevamente", () => {
    const button = createButtonMock();
    const state = requestState.createState(button);

    state.begin();
    state.end();

    expect(state.canSubmit()).toBe(true);
    expect(state.isInProgress()).toBe(false);
    expect(button.prop).toHaveBeenNthCalledWith(1, "disabled", true);
    expect(button.prop).toHaveBeenNthCalledWith(2, "disabled", false);
  });

  test("tolera ausencia de botón", () => {
    const state = requestState.createState(null);

    expect(state.begin()).toBe(true);
    expect(state.end()).toBeUndefined();
    expect(state.canSubmit()).toBe(true);
  });
});
