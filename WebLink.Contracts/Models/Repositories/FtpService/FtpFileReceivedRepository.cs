using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Documents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{

	public interface IFtpFileReceivedRepository : IGenericRepository<IFTPFileReceived>
	{

	}


	public class FtpFileReceivedRepository : GenericRepository<IFTPFileReceived, FtpFileReceived>, IFtpFileReceivedRepository
	{
		public FtpFileReceivedRepository(IFactory factory)
			: base(factory, (ctx) => ctx.FtpFilesReceived)
		{
		}

		
		protected override string TableName { get => "FtpFilesReceived"; }


        protected override void UpdateEntity(PrintDB ctx, IUserData userData, FtpFileReceived actual, IFTPFileReceived data)
        {

        }
    }
}
