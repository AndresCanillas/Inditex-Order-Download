using Remotion.Linq.Clauses;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.OrderPlugins
{

    /**
     *
     * Default behavior to take orders as validated or not from Project Configuration
     */
    [FriendlyName("Smartdots.TakeAsValidPlugin")]
    [Description("Smartdots.TakeAsValidPlugin")]
    public  class TakeAsValidGenericPlugin : IOrderSetValidatorPlugin
    {
        
        private readonly IProjectRepository projectRepo;
        
        public TakeAsValidGenericPlugin(IProjectRepository projectRepo)
        {
            this.projectRepo = projectRepo;
        }

        public int TakeAsValidated(OrderPluginData orderData)
        {
            var project = projectRepo.GetByID(orderData.ProjectID);

            return project.TakeOrdersAsValid ? 1 : 0;

        }

        public void Dispose()
        {
        }
    }
}
