--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

use YASBE
go

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

/*
drop proc MediaSubset_Commit
drop TYPE MediaSubsetFile_UDT 
*/
if not exists(select 1 from sys.types where name = 'MediaSubsetFile_UDT')
  create TYPE FileArchiveID_UDT AS TABLE
  (
    FileArchiveID INT
  )
go