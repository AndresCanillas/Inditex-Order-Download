
namespace Service.Contracts.Infrastructure.Encoding.Tempe
{
    partial class EpcRepositoryTempe
    {
        private void InitializeDatabase()
        {
            lock(syncObj)
            {
                if(dbInitialized)
                    return;
                try
                {
                    CheckDatabase();
                }
                catch { }
                dbInitialized = true;
            }
        }

        private void CheckDatabase()
        {
            using(var conn = connectionManager.OpenDB("TempeEpcService"))
            {
                conn.ExecuteNonQuery(script);
            }
        }

        private readonly string script = @"
			IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AllocatedEpc]') AND type in (N'U'))
			BEGIN
				CREATE TABLE [dbo].[AllocatedEpc](
					[Epc] [varchar](32) NOT NULL,
					[OrderID] [int] NOT NULL,
					[DetailID] [int] NOT NULL,
					[UserMemory] [varchar](8) NULL,
					[AccessPassword] [varchar](8) NULL,
					[KillPassword] [varchar](8) NULL,
					CONSTRAINT [PK_AllocatedEpc] PRIMARY KEY CLUSTERED ([Epc] ASC)
				)
			END

			IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LockInfo]') AND type in (N'U'))
			BEGIN
				CREATE TABLE [dbo].[LockInfo](
					[OrderID] [int] NOT NULL,
					[EpcLock] [int] NOT NULL,
					[UserMemoryLock] [int] NOT NULL,
					[KillPasswordLock] [int] NOT NULL,
					[AccessPasswordLock] [int] NOT NULL,
					CONSTRAINT [PK_LockInfo] PRIMARY KEY CLUSTERED ([OrderID] ASC)
				)
			END

			IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OrderDetail]') AND type in (N'U'))
			BEGIN
				CREATE TABLE [dbo].[OrderDetail](
					[OrderID] [int] NOT NULL,
					[DetailID] [int] NOT NULL,
					[Quantity] [int] NOT NULL,
					[Model] [int] NOT NULL,
					[Quality] [int] NOT NULL,
					[Color] [int] NOT NULL,
					[Size] [int] NOT NULL,
					[TagType] [int] NOT NULL,
					[TagSubType] [int] NOT NULL,
					[Allocated] [bit] NOT NULL,
					[RfidRequest] [int] NOT NULL,
                    [Used] [bit] NOT NULL,
					CONSTRAINT [PK_OrderDetail] PRIMARY KEY CLUSTERED 
					(
						[DetailID] ASC,
						[OrderID] ASC
					)
				)
			END

			IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OrderStatus]') AND type in (N'U'))
			BEGIN
				CREATE TABLE [dbo].[OrderStatus](
					[OrderID] [int] NOT NULL,
					[OrderNumber] [nvarchar](30) NOT NULL,
					[AllocationStatus] [int] NOT NULL,
					[AllocationDate] [datetime2](7) NULL,
					CONSTRAINT [PK_OrderStatus] PRIMARY KEY CLUSTERED ([OrderID] ASC)
				)
			END

			IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PreencodingOrderDetail]') AND type in (N'U'))
			BEGIN
				CREATE TABLE [dbo].[PreencodingOrderDetail](
					[OrderID] [int] NOT NULL,
					[DetailID] [int] NOT NULL,
					[Quantity] [int] NOT NULL,
					[BrandId] [int] NOT NULL,
					[ProductTypeCode] [int] NOT NULL,
					[Color] [int] NOT NULL,
					[Size] [int] NOT NULL,
					[TagType] [int] NOT NULL,
					[TagSubType] [int] NOT NULL,
					[Allocated] [bit] NOT NULL,
					[RfidRequest] [int] NOT NULL,
                    [Used] [bit] NOT NULL,
					CONSTRAINT [PK_PreencodingOrderDetail] PRIMARY KEY CLUSTERED
					(
						[OrderID] ASC,
						[DetailID] ASC
					)
				)
			END

            IF NOT EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'OrderDetail' AND COLUMN_NAME = 'Used'
            )
            BEGIN
                ALTER TABLE dbo.OrderDetail
                ADD [Used] BIT NOT NULL CONSTRAINT DF_OrderDetail_Used DEFAULT (0);
            END

            IF NOT EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'PreencodingOrderDetail' AND COLUMN_NAME = 'Used'
            )
            BEGIN
                ALTER TABLE dbo.PreencodingOrderDetail
                ADD [Used] BIT NOT NULL CONSTRAINT DF_PreencodingOrderDetail_Used DEFAULT (0);
            END

		";
    }
}
