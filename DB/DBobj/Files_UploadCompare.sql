
--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[Files_UploadCompare]    Script Date: 06/19/2009 15:58:30 ******/
use YASBE
go

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

if not exists(select 1 from sysobjects where name = 'Files_UploadCompare')
	exec('create PROCEDURE Files_UploadCompare as select 1 as one')
GO
alter PROCEDURE [dbo].[Files_UploadCompare]
@BackupProfileID INT,
@NextDiscNumber INT = NULL OUT,
@AllFiles File_UDT READONLY
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

-- new approach, simply return all files which don't match something already in the database 
-- then we don't have to worry about partial results left in the tables ... 
-- we just upload the current batch of files when we're with each burn and then start fresh with the next batch selection from there
-- there will be no records in FileArchive unless they've been put there specifically as marking a "finalized" MediaSubset

SELECT *,
  CONVERT(BIT, 0) AS Selected,
  CONVERT(BIT, 0) AS SkipError
FROM @AllFiles a
WHERE NOT EXISTS(
  SELECT 1
  FROM FileArchive fa
  JOIN [File] f ON fa.FileID = f.FileID
  WHERE f.FullPath = a.FullPath AND fa.ModifiedDate = a.ModifiedDate AND fa.Size = a.Size
)

DECLARE @IncrementalID int
SELECT @IncrementalID = MAX(IncrementalID) FROM [Incremental] WHERE BackupProfileID = BackupProfileID

SELECT @NextDiscNumber = isnull(COUNT(1),0)+1 FROM MediaSubset WHERE IncrementalID = @IncrementalID

END
GO

grant execute on Files_UploadCompare to public
go

