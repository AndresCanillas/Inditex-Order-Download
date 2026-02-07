using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class FtpLastRead : IFtpLastRead
    {
        #region IEntity
        [PK, Identity]
        public int ID { get; set; }
        #endregion IEntity

        #region IFtpLastRead
        public int ProjectID { get; set; }
        public string Server { get; set; }
        public string UserName { get; set; }
        public DateTime LastRead { get; set; }
        #endregion IFtpLastRead

    }
}
