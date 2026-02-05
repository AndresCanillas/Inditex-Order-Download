	IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ItemData]') AND type in (N'U'))
	BEGIN
		CREATE TABLE [dbo].[ItemData](
			[WorkflowID] [int] NOT NULL,
			[ItemID] [bigint] NOT NULL,
			[ParentWorkflowID] [int] NULL,
			[ParentItemID] [bigint] NULL,
			[ItemName] [nvarchar](30) NULL,
			[Keywords] [nvarchar](250) NULL,
			[TaskID] [int] NULL,
			[TaskDate] [datetime2](7) NULL,
			[ItemPriority] [int] NOT NULL,
			[ItemStatus] [int] NOT NULL,
			[StatusReason] [nvarchar](512) NULL,
			[RouteCode] [nvarchar](30) NULL,
			[DelayedUntil] [datetime2](7) NOT NULL,
			[RetryCount] [int] NOT NULL,
			[WorkflowStatus] [int] NOT NULL,
			[CompletedFrom] [int] NULL,
			[CompletedDate] [datetime2](7) NULL,
			[ItemState] [nvarchar](max) NULL,
			[SourceEventState] [nvarchar](max) NULL,
			[WakeEventState] [nvarchar](max) NULL,
			[RejectOnFinally] [bit] NOT NULL,
			[OutstandingHandler] [int] NULL,
			[WakeTimeout] [datetime2](7) NULL,
			LastException varbinary(MAX) NULL,
			[MaxTries] [int] NOT NULL,
			[CreatedDate] [datetime2](7) NOT NULL
			CONSTRAINT [PK_ItemData] PRIMARY KEY CLUSTERED 
				(
					[WorkflowID] ASC,
					[ItemID] ASC
				) ON [PRIMARY]
		) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
	END

	IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ItemData]') AND name = N'IX_ItemName')
	CREATE UNIQUE NONCLUSTERED INDEX [IX_ItemName] ON [dbo].[ItemData]
	(
		[WorkflowID] ASC,
		[ItemName] ASC
	) ON [PRIMARY]

	IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ItemData]') AND name = N'IX_Keywords')
	CREATE NONCLUSTERED INDEX [IX_Keywords] ON [dbo].[ItemData]
	(
		[Keywords] ASC
	) ON [PRIMARY]

	IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ItemData]') AND name = N'IX_ItemStatus')
	CREATE NONCLUSTERED INDEX [IX_ItemStatus] ON [dbo].[ItemData]
	(
		[ItemStatus] ASC
	) ON [PRIMARY]

	IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ItemData]') AND name = N'IX_RunnerIndex')
	CREATE NONCLUSTERED INDEX [IX_RunnerIndex] ON [dbo].[ItemData]
	(
		[TaskID] ASC,
		[ItemStatus] ASC,
		[DelayedUntil] ASC
	) ON [PRIMARY]

	IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ItemData]') AND name = N'IX_TaskDate')
	CREATE NONCLUSTERED INDEX [IX_TaskDate] ON [dbo].[ItemData]
	(
		[WorkflowID],
		[TaskDate]
	) ON [PRIMARY]

	IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ItemData]') AND name = N'IX_WakeTimeout')
	CREATE NONCLUSTERED INDEX [IX_WakeTimeout] ON [dbo].[ItemData]
	(
		[WorkflowID] ASC,
		[TaskID] ASC,
		[WakeTimeOut] ASC
	) ON [PRIMARY]

	IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ItemLog]') AND type in (N'U'))
	BEGIN
		CREATE TABLE [dbo].[ItemLog](
			[EntryID] [bigint] IDENTITY(1,1) NOT NULL,
			[WorkflowID] [int] NOT NULL,
			[ItemID] [bigint] NOT NULL,
			[EntryType] [int] NOT NULL,
			[EntrySubType] [int] NOT NULL,
			[Visibility] [int] NOT NULL,
			[AttachedData] [nvarchar](max) NOT NULL,
			[Date] [datetime2](7) NOT NULL,
			CONSTRAINT [PK_ItemLog] PRIMARY KEY CLUSTERED 
			(
				[EntryID] ASC
			) ON [PRIMARY]
		) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
	END

	IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ItemLog]') AND name = N'IX_ItemLog')
	CREATE NONCLUSTERED INDEX [IX_ItemLog] ON [dbo].[ItemLog]
	(
		[WorkflowID] ASC,
		[ItemID] ASC
	) ON [PRIMARY]
		
	IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TaskData]') AND type in (N'U'))
	BEGIN
		CREATE TABLE [dbo].[TaskData](
			[TaskID] [int] IDENTITY(1,1) NOT NULL,
			[WorkflowID] [int] NOT NULL,
			[TaskName] [nvarchar](80) NOT NULL,
			[CanRunOutOfFlow] [bit] NOT NULL,
			[TaskType] [int] NULL,
			[Detached] [bit] NOT NULL,
			[SortOrder] [int] NOT NULL
			CONSTRAINT [PK_TaskData_1] PRIMARY KEY CLUSTERED 
			(
				[TaskID] ASC
			) ON [PRIMARY]
		) ON [PRIMARY]
	END

	IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WorkflowData]') AND type in (N'U'))
	BEGIN
		CREATE TABLE [dbo].[WorkflowData](
			[WorkflowID] [int] IDENTITY(1,1) NOT NULL,
			[Name] [nvarchar](50) NOT NULL,
			[MaxItemRetryCount] [int] NOT NULL,
			[Detached] [bit] NOT NULL,
			CONSTRAINT [PK_WorkflowData] PRIMARY KEY CLUSTERED 
			(
				[WorkflowID] ASC
			) ON [PRIMARY]
		) ON [PRIMARY]
	END

	IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ItemData_TaskData]') AND parent_object_id = OBJECT_ID(N'[dbo].[ItemData]'))
	ALTER TABLE [dbo].[ItemData]  WITH CHECK ADD  CONSTRAINT [FK_ItemData_TaskData] FOREIGN KEY([TaskID])
	REFERENCES [dbo].[TaskData] ([TaskID])
	ON DELETE CASCADE

	IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ItemData_TaskData]') AND parent_object_id = OBJECT_ID(N'[dbo].[ItemData]'))
	ALTER TABLE [dbo].[ItemData] CHECK CONSTRAINT [FK_ItemData_TaskData]

	IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ItemLog_ItemData]') AND parent_object_id = OBJECT_ID(N'[dbo].[ItemLog]'))
	ALTER TABLE [dbo].[ItemLog]  WITH CHECK ADD  CONSTRAINT [FK_ItemLog_ItemData] FOREIGN KEY([WorkflowID], [ItemID])
	REFERENCES [dbo].[ItemData] ([WorkflowID], [ItemID])
	ON DELETE CASCADE

	IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ItemLog_ItemData]') AND parent_object_id = OBJECT_ID(N'[dbo].[ItemLog]'))
	ALTER TABLE [dbo].[ItemLog] CHECK CONSTRAINT [FK_ItemLog_ItemData]

	IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_TaskData_WorkflowData]') AND parent_object_id = OBJECT_ID(N'[dbo].[TaskData]'))
	ALTER TABLE [dbo].[TaskData]  WITH CHECK ADD  CONSTRAINT [FK_TaskData_WorkflowData] FOREIGN KEY([WorkflowID])
	REFERENCES [dbo].[WorkflowData] ([WorkflowID])
	ON DELETE CASCADE

	IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_TaskData_WorkflowData]') AND parent_object_id = OBJECT_ID(N'[dbo].[TaskData]'))
	ALTER TABLE [dbo].[TaskData] CHECK CONSTRAINT [FK_TaskData_WorkflowData]

	IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ItemData' AND COLUMN_NAME = 'LastException')
	BEGIN
		ALTER TABLE ItemData ADD LastException varbinary(MAX) NULL
		IF TYPE_ID(N'WFItem') IS NOT NULL 
			DROP type WFItem
	END

	IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TaskData' AND COLUMN_NAME = 'TaskType')
	BEGIN
		ALTER TABLE TaskData ADD TaskType int NULL
	END

	IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ItemData' AND COLUMN_NAME = 'MaxTries')
	BEGIN
		ALTER TABLE ItemData ADD MaxTries int NULL
		EXEC sp_executesql N'Update ItemData set MaxTries = 5'
		ALTER TABLE ItemData Alter Column MaxTries int NOT NULL
		IF TYPE_ID(N'WFItem') IS NOT NULL 
			DROP type WFItem
        
	END

	IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'WorkflowData' AND COLUMN_NAME = 'MaxItemRetryCount')
	BEGIN
		ALTER TABLE WorkflowData drop column MaxItemRetryCount
	END

	IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ItemData' AND COLUMN_NAME = 'CreatedDate')
	BEGIN
		ALTER TABLE ItemData ADD CreatedDate DateTime NULL
		EXEC sp_executesql N'Update ItemData set CreatedDate = COALESCE(TaskDate, GETDATE())'
		ALTER TABLE ItemData ALTER COLUMN CreatedDate DateTime NOT NULL
		IF TYPE_ID(N'WFItem') IS NOT NULL 
			DROP type WFItem
	END

	IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ItemLog' AND COLUMN_NAME = 'EntrySubType')
	BEGIN
		ALTER TABLE ItemLog ADD EntrySubType int NULL
		EXEC sp_executesql N'UPDATE ItemLog set EntrySubType = CASE WHEN COALESCE(LEN(JSON_VALUE(AttachedData, ''$.ExceptionMessage'')), 0) = 0 THEN 0 ELSE 1 END '
		ALTER TABLE ItemLog ALTER COLUMN EntrySubType int NOT NULL
	END

	IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ItemLog]') AND name = N'IX_ItemLog_ByEntryTypeAndDate')
	CREATE NONCLUSTERED INDEX [IX_ItemLog_ByEntryTypeAndDate]
	ON [dbo].[ItemLog] ([ItemID],[EntryType],[EntrySubType])
	INCLUDE ([Date])


	IF OBJECT_ID('GetLastItemError') IS NULL
	BEGIN
		EXEC sp_executesql N'
			create function GetLastItemError(@itemid bigint)
			returns @result TABLE(
			  LastErrorMessage nvarchar(MAX) NOT NULL,
			  LastErrorDate DateTime2(7) NOT NULL,
			  LastErrorTaskID int NOT NULL
			)
			as
			begin
				   insert into @result
				   select TOP 1
						 JSON_VALUE(AttachedData, ''$.ExceptionMessage'') as LastErrorMessage,
						 [Date] as LastErrorDate,
						 CAST(JSON_VALUE(AttachedData, ''$.TaskID'') as int) as LastErrorTaskID
				   from ItemLog
				   where ItemID = @itemid and EntryType = 0 and EntrySubType = 1
				   order by Date desc
				   return
			end
		'
	END

	IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ItemData' AND COLUMN_NAME = 'ProjectID')
	BEGIN
		ALTER TABLE dbo.ItemData
		  ADD CONSTRAINT CK_ItemData_ItemState_IsJson
		  CHECK (ISJSON(ItemState) = 1);

		ALTER TABLE dbo.ItemData
		  ADD ProjectID AS TRY_CONVERT(int, JSON_VALUE(ItemState, '$.ProjectID')) PERSISTED;

		CREATE INDEX IX_ItemData_ProjectID_Task_Status
			ON dbo.ItemData (ProjectID, TaskID, ItemStatus);

		IF TYPE_ID(N'WFItem') IS NOT NULL 
			DROP type WFItem
	END

	IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ItemData' AND COLUMN_NAME = 'OrderID')
	BEGIN
		ALTER TABLE dbo.ItemData
		  ADD OrderID AS TRY_CONVERT(int, JSON_VALUE(ItemState, '$.OrderID')) PERSISTED;

		CREATE INDEX IX_ItemData_WakeByOrderID
			ON dbo.ItemData (WorkflowID, TaskID, ItemStatus, OrderID, ItemID);

		IF TYPE_ID(N'WFItem') IS NOT NULL 
			DROP type WFItem
	END


	IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ItemData]') AND name = N'IX_ItemData_WakeFromDelay')
	CREATE INDEX IX_ItemData_WakeFromDelay
		ON dbo.ItemData (WorkflowID, ItemStatus, DelayedUntil, TaskID, ItemID);


	IF TYPE_ID(N'WFItem') IS NULL
	CREATE TYPE WFItem AS TABLE (
		[WorkflowID] [int] NOT NULL,
		[ItemID] [bigint] NOT NULL,
		[ParentWorkflowID] [int] NULL,
		[ParentItemID] [bigint] NULL,
		[ItemName] [nvarchar](30) NULL,
		[Keywords] [nvarchar](250) NULL,
		[TaskID] [int] NULL,
		[TaskDate] [datetime2](7) NULL,
		[ItemPriority] [int] NOT NULL,
		[ItemStatus] [int] NOT NULL,
		[StatusReason] [nvarchar](512) NULL,
		[RouteCode] [nvarchar](30) NULL,
		[DelayedUntil] [datetime2](7) NOT NULL,
		[RetryCount] [int] NOT NULL,
		[WorkflowStatus] [int] NOT NULL,
		[CompletedFrom] [int] NULL,
		[CompletedDate] [datetime2](7) NULL,
		[ItemState] [nvarchar](max) NULL,
		[SourceEventState] [nvarchar](max) NULL,
		[WakeEventState] [nvarchar](max) NULL,
		[RejectOnFinally] [bit] NOT NULL,
		[OutstandingHandler] [int] NULL,
		[WakeTimeout] [datetime2](7) NULL,
		[LastException] varbinary(MAX) NULL,
		[MaxTries] [int] NOT NULL,
		[CreatedDate] [datetime2](7) NOT NULL,
		[ProjectID] [int] NULL,
		[OrderID] [int] NULL
	)
