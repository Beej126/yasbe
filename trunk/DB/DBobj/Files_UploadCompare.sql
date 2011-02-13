
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
@IncrementalID int,
@OverrideExistingMediaSubsets BIT = 0,
@Files File_UDT readonly
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

DECLARE @BackupProfileID INT
SELECT @BackupProfileID = 1 FROM Incremental WHERE IncrementalID = @IncrementalID

IF (@OverrideExistingMediaSubsets = 0)
BEGIN
  DECLARE @MediaSubsetCount INT, @CR VARCHAR(2)
  SET @CR = CHAR(13)+CHAR(10)
  SELECT @MediaSubsetCount = MAX(MediaSubsetNumber) FROM MediaSubset WHERE IncrementalID = @IncrementalID
  IF (@MediaSubsetCount > 0)
  BEGIN
    RAISERROR('[CONFIRM] There are %d existing Discs defined in this Incremental.%sSelect [Override] if you''re sure you want to lose them.', 16,1, @MediaSubsetCount, @CR)
    RETURN
  END
END

--wipe out any existing MediaSubsets, which cascades to FileArchives which cascades to Files!!!!
DELETE MediaSubset WHERE IncrementalID = @IncrementalID AND MediaSubsetNumber > 0

/*
INSERT [File] ( FullPath )
SELECT FullPath FROM @Files

INSERT FileArchive ( MediaSubsetID ,FileID ,ModifiedDate ,Size )
SELECT 1, f.FileID, f2.ModifiedDate, f2.size FROM [File] f JOIN @files f2 ON f2.fullpath = f.FullPath
*/

-- start off with a MediaSubsetNumber == 0 (aka "Optical Disc #0") to represent all files that haven't been finalized on optical media yet
DECLARE @MediaSubsetID INT
SELECT @MediaSubsetID = MediaSubsetID  FROM MediaSubset WHERE IncrementalID = @IncrementalID AND MediaSubsetNumber = 0
IF (@MediaSubsetID IS null)
BEGIN
  INSERT MediaSubset ( IncrementalID, MediaSubsetNumber ) 
  VALUES  ( @IncrementalID, 0 )
  SET @MediaSubsetID = SCOPE_IDENTITY()
END

-- find the subset of all files in this upload which are not already logged under a previous incremental backup
-- insert these newly identified records as a new incremental backup set to be worked through burning to optical media

-- the main driver behind normalizing out "File" is FullPath--- it's a big field and it needs to be indexed and it seems like a waste of space to duplicate it every time it changes
-- therefore FileArchive represents the "light" changes in attributes that a file goes through over time (just ModifiedDate and Size so far, anything else?? maybe a CRC check if needs actually arise) 
-- FileArchive should remain a very slim table since the record is only comprised of a few byte sized fields... 28 bytes wide currently

-- but the (slightly) annoying thing is that because of normalizing a File's FullPath apart from the rest of it's properties
-- means we have to perform the "new file" insert process in two stages...
-- 1) one for the Files that have never been recorded at all (File table insert)
-- 2) another insert block for Files which have changed since last time (FileArchive insert)


-- slight optimzation, do #2 first to avoid unecessarily scanning over the set represented by #1 
-- 2) insert the set of files which have been recorded previously but have *changed* since then
INSERT FileArchive(MediaSubsetID, FileID, ModifiedDate, [Size])
SELECT @MediaSubsetID, f2.FileID, f1.ModifiedDate, f1.[Size]
FROM @Files f1 
JOIN [File] f2 ON f2.FullPath = f1.FullPath
WHERE NOT EXISTS(SELECT 1 FROM FileArchive fa WHERE fa.FileID = f2.FileID AND fa.ModifiedDate = f1.ModifiedDate AND fa.[Size] = f1.[Size] )



-- 1) insert all the files which have never been uploaded at all...
-- DANG, SQL Error: "The target table 'FileArchive' of the OUTPUT INTO clause cannot be on either side of a (primary key, foreign key) relationship. Found reference constraint 'FK_FileArchive_File'."
-- so have to temp table the OUTPUT clause and then insert into FileArchive from there
DECLARE @NewFiles TABLE(FileID INT)

INSERT [File] (FullPath)
OUTPUT INSERTED.FileID INTO @NewFiles(FileID)
SELECT f1.FullPath FROM @Files f1 WHERE NOT EXISTS(SELECT 1 FROM [File] f2 WHERE f2.FullPath = f1.FullPath)

INSERT FileArchive(MediaSubsetID, FileID, ModifiedDate, [Size])
SELECT @MediaSubsetID, n.FileID, f2.ModifiedDate, f2.[Size]
FROM @NewFiles n
JOIN [File] f ON f.FileID = n.FileID
JOIN @Files f2 ON f2.FullPath = f.FullPath



/* this code is more careful to only consider previously backed up files in within the *same* backup profile
   i *think* i don't care which profile it fell under previously ... if it was backed up *at all*, that's good enough to exclude it from this backup run
   and therefore i'll avoid duplicates if i *ignore* backup profile inclusion as i've re-coded it above
   the irony is that *duplicates* seem like a good thing when it comes to backing things up... things that make you go, hmmmm
   
  SELECT 1
  FROM Incremental i
  JOIN MediaSubset m ON m.IncrementalID = i.IncrementalID
  JOIN FileArchive fa ON fa.MediaSubsetID = m.MediaSubsetID
  JOIN [File] f2 ON f2.FileID = fa.FileID
  WHERE i.BackupProfileID = @BackupProfileID
  AND f2.FullPath = f1.FullPath
  AND ( fa.ModifiedDate <> f1.ModifiedDate
  OR   fa.[Size] <> f1.[Size])
*/

END
GO

grant execute on Files_UploadCompare to public
go

