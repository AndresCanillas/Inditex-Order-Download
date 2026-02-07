using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SmartdotsPlugins.Brownie.WizardPlugin
{
    public class FiberEntity
    {

        [Key] 
        public int Id { get; set; }
        public string Code { get; set; }
    }
}
