/* Aurora Hotel: additive development-only business seed.
   Apply schema.sql and run the Development app once to bootstrap Identity safely.
   No passwords/hashes are exported. This script does not reset or delete existing data.
   Re-running keeps the existing demo reservations and their history unchanged. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE N'AuroraHotel%'
    THROW 51001, 'Select the intended AuroraHotel development database first.', 1;
IF OBJECT_ID(N'dbo.Reservations') IS NULL
    THROW 51002, 'Apply docs/schema.sql before seed-demo.sql.', 1;
DECLARE @actor nvarchar(450) = (SELECT TOP(1) Id FROM dbo.AspNetUsers WHERE NormalizedEmail=N'ADMIN@AURORA.LOCAL');
IF @actor IS NULL
    THROW 51003, 'Start the Development application once to create the Identity demo accounts.', 1;
DECLARE @now datetimeoffset = SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time';
DECLARE @today date = CONVERT(date, @now);
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'AuroraHotel:operations', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
IF @lock<0 THROW 51004, 'Database busy; retry later.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.RoomTypes WHERE Name=N'SQL Demo Deluxe')
    INSERT dbo.RoomTypes(Name,Capacity,Description) VALUES(N'SQL Demo Deluxe',2,N'Loại phòng minh họa từ SQL script.');
IF NOT EXISTS (SELECT 1 FROM dbo.RoomTypes WHERE Name=N'SQL Demo Family')
    INSERT dbo.RoomTypes(Name,Capacity,Description) VALUES(N'SQL Demo Family',4,N'Loại phòng gia đình minh họa.');
DECLARE @type int=(SELECT TOP(1) Id FROM dbo.RoomTypes WHERE Name=N'SQL Demo Deluxe');
DECLARE @family int=(SELECT TOP(1) Id FROM dbo.RoomTypes WHERE Name=N'SQL Demo Family');
IF NOT EXISTS (SELECT 1 FROM dbo.Rooms WHERE RoomNumber=N'D901')
    INSERT dbo.Rooms(RoomNumber,RoomTypeId,NightlyRate,ImagePath,Description,Amenities,HousekeepingStatus,OperationalStatus)
    VALUES(N'D901',@type,800000,N'/images/room-deluxe.jpg',N'<p>Phòng demo từ script SQL.</p>',N'Wi-Fi, Điều hòa',0,0);
IF NOT EXISTS (SELECT 1 FROM dbo.Rooms WHERE RoomNumber=N'D902')
    INSERT dbo.Rooms(RoomNumber,RoomTypeId,NightlyRate,ImagePath,Description,Amenities,HousekeepingStatus,OperationalStatus)
    VALUES(N'D902',@type,800000,N'/images/room-premier.jpg',N'<p>Phòng cần dọn trước khi nhận khách.</p>',N'Wi-Fi, Điều hòa',1,0);
IF NOT EXISTS (SELECT 1 FROM dbo.Rooms WHERE RoomNumber=N'D903')
    INSERT dbo.Rooms(RoomNumber,RoomTypeId,NightlyRate,ImagePath,Description,Amenities,HousekeepingStatus,OperationalStatus)
    VALUES(N'D903',@family,1500000,N'/images/room-suite.jpg',N'<p>Phòng đang ngừng sử dụng.</p>',N'Wi-Fi, Điều hòa',0,1);
IF NOT EXISTS (SELECT 1 FROM dbo.Rooms WHERE RoomNumber=N'D904')
    INSERT dbo.Rooms(RoomNumber,RoomTypeId,NightlyRate,ImagePath,Description,Amenities,HousekeepingStatus,OperationalStatus)
    VALUES(N'D904',@type,800000,N'/images/room-deluxe.jpg',N'<p>Phòng đang lưu trú để thử thanh toán.</p>',N'Wi-Fi, Điều hòa',0,0);

IF NOT EXISTS (SELECT 1 FROM dbo.GuestProfiles WHERE Notes=N'SQL-DEMO-SEED-v1')
    INSERT dbo.GuestProfiles(UserId,FullName,Phone,Email,Notes)
    VALUES(NULL,N'Khách Minh Họa SQL',N'0900000000',N'sql.demo@aurora.example',N'SQL-DEMO-SEED-v1');
DECLARE @guest int=(SELECT TOP(1) Id FROM dbo.GuestProfiles WHERE Notes=N'SQL-DEMO-SEED-v1');
IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name=N'Bữa sáng SQL Demo')
    INSERT dbo.Services(Name,Unit,UnitPrice,IsActive) VALUES(N'Bữa sáng SQL Demo',N'suất',100000,1);
DECLARE @service int=(SELECT TOP(1) Id FROM dbo.Services WHERE Name=N'Bữa sáng SQL Demo');
DECLARE @room int=(SELECT Id FROM dbo.Rooms WHERE RoomNumber=N'D901');
IF NOT EXISTS (SELECT 1 FROM dbo.Reservations WHERE ConfirmationCode=N'AU-SQL-ARRIVAL')
    INSERT dbo.Reservations(ConfirmationCode,GuestProfileId,RoomId,ArrivalDate,DepartureDate,GuestCount,NightlyRateSnapshot,Status,Source,CreatedByUserId,CreatedAt,Notes)
    VALUES(N'AU-SQL-ARRIVAL',@guest,@room,@today,DATEADD(day,2,@today),2,800000,1,1,@actor,@now,N'Đơn Confirmed tạo bởi script SQL để demo.');
SET @room=(SELECT Id FROM dbo.Rooms WHERE RoomNumber=N'D902');
IF NOT EXISTS (SELECT 1 FROM dbo.Reservations WHERE ConfirmationCode=N'AU-SQL-PENDING')
    INSERT dbo.Reservations(ConfirmationCode,GuestProfileId,RoomId,ArrivalDate,DepartureDate,GuestCount,NightlyRateSnapshot,Status,Source,CreatedByUserId,CreatedAt,Notes)
    VALUES(N'AU-SQL-PENDING',@guest,@room,@today,DATEADD(day,2,@today),2,800000,0,1,@actor,@now,N'Đơn Pending, chưa giữ chỗ.');
SET @room=(SELECT Id FROM dbo.Rooms WHERE RoomNumber=N'D904');
IF NOT EXISTS (SELECT 1 FROM dbo.Reservations WHERE ConfirmationCode=N'AU-SQL-INHOUSE')
BEGIN
    INSERT dbo.Reservations(ConfirmationCode,GuestProfileId,RoomId,ArrivalDate,DepartureDate,GuestCount,NightlyRateSnapshot,Status,Source,CreatedByUserId,CreatedAt,CheckInAt,Notes)
    VALUES(N'AU-SQL-INHOUSE',@guest,@room,@today,DATEADD(day,2,@today),2,800000,2,1,@actor,@now,@now,N'Lưu trú minh họa, tổng bảng kê 1.800.000 VND.');
    DECLARE @reservation int=SCOPE_IDENTITY();
    INSERT dbo.Folios(ReservationId,Status,CreatedAt) VALUES(@reservation,0,@now);
    DECLARE @folio int=SCOPE_IDENTITY();
    INSERT dbo.FolioItems(FolioId,Kind,ServiceId,DescriptionSnapshot,Quantity,UnitPriceSnapshot)
    VALUES(@folio,0,NULL,N'Tiền phòng D904 - 2 đêm',2,800000),(@folio,1,@service,N'Bữa sáng SQL Demo / suất',2,100000);
END;
COMMIT;
PRINT N'Đã thêm dữ liệu SQL minh họa. Đăng nhập để thao tác AU-SQL-ARRIVAL / AU-SQL-PENDING / AU-SQL-INHOUSE.';
