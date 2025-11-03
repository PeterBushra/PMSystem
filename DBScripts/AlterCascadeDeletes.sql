-- Update existing database to cascade deletes from Project -> Task and Task -> TaskLog
-- Run this against your ADHMMC-PM database

-- Drop and recreate FK from Task to Project with ON DELETE CASCADE
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Task_Project')
BEGIN
    ALTER TABLE [dbo].[Task] DROP CONSTRAINT [FK_Task_Project];
END
GO
ALTER TABLE [dbo].[Task]  WITH CHECK ADD  CONSTRAINT [FK_Task_Project] FOREIGN KEY([ProjectId])
REFERENCES [dbo].[Project] ([Id]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[Task] CHECK CONSTRAINT [FK_Task_Project];
GO

-- Drop and recreate FK from TaskLog to Task with ON DELETE CASCADE
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TaskLog_Task')
BEGIN
    ALTER TABLE [dbo].[TaskLog] DROP CONSTRAINT [FK_TaskLog_Task];
END
GO
ALTER TABLE [dbo].[TaskLog]  WITH CHECK ADD  CONSTRAINT [FK_TaskLog_Task] FOREIGN KEY([TaskId])
REFERENCES [dbo].[Task] ([Id]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[TaskLog] CHECK CONSTRAINT [FK_TaskLog_Task];
GO
