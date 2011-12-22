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
drop proc BackupProfile_u
drop TYPE File_UDT 
*/
if not exists(select 1 from sys.types where name = 'File_UDT')
  create TYPE File_UDT AS TABLE
  (
    FullPath varchar(900) PRIMARY KEY, 
    ModifiedDate datetime, 
    [Size] bigint
  )
GO

GRANT EXECUTE ON TYPE::dbo.File_UDT TO PUBLIC
GO