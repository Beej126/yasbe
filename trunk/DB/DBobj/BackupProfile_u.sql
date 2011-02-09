
--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[BackupProfile_u]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

if not exists(select 1 from sys.types where name = 'BackupProfileFolder_type')
  create TYPE BackupProfileFolder_type AS TABLE
  (
    BackupProfileID int,
    FullPath VARCHAR(8000)
  )
go

if not exists(select 1 from sysobjects where name = 'BackupProfile_u')
	exec('create PROCEDURE BackupProfile_u as select 1 as one')
GO
alter PROCEDURE [dbo].[BackupProfile_u]
@BackupProfileID int,
@Name varchar(50) = null,
@Folders BackupProfileFolder_type readonly
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

update BackupProfile set
  Name = isnull(@Name, Name)
where BackupProfileID = @BackupProfileID

if (@@RowCount = 0)
begin
  insert BackupProile (Name) values (@Name)
end

delete BackupProfileFolder where BackupProfileID = @BackupProfileID
insert BackupProfileFolder (BackupProfileID, FullPath)
select @BackupProfileID, FullPath
from @Folders

END
GO

grant execute on BackupProfile_u to public
go

