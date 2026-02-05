using System;

namespace Service.Contracts.PrintCentral
{
    public interface IOrderSetValidatorPlugin : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderData"></param>
        /// <returns>1 if take as valid, 0 no changes</returns>
        int TakeAsValidated(OrderPluginData orderData);
    }
}
