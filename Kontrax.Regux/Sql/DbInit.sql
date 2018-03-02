CREATE SCHEMA N;
GO

CREATE TABLE N.KeyType (
	Code varchar(20) NOT NULL CONSTRAINT PK_KeyType PRIMARY KEY,
	Name nvarchar(100) NOT NULL
);

INSERT INTO N.KeyType (Code, Name) VALUES
('Egn', N'ЕГН'),
('Lnch', N'ЛНЧ'),
('Ssn', N'SSN'),
('Eik', N'ЕИК');

CREATE TABLE AspNetUsers (
	Id nvarchar(128) NOT NULL,
	Email nvarchar(256),
	EmailConfirmed bit NOT NULL,
	PasswordHash nvarchar(max),
	SecurityStamp nvarchar(max),
	PhoneNumber nvarchar(max),
	PhoneNumberConfirmed bit NOT NULL,
	TwoFactorEnabled bit NOT NULL,
	LockoutEndDateUtc datetime,
	LockoutEnabled bit NOT NULL,
	AccessFailedCount int NOT NULL,
	UserName nvarchar(256) NOT NULL,
	PersonName nvarchar(max),
	Certificate varbinary(max),
	PidTypeCode varchar(20) CONSTRAINT FK_AspNetUsers_KeyType FOREIGN KEY REFERENCES N.KeyType (Code) ON UPDATE CASCADE,
    PersonIdentifier nvarchar(256),
	IsBanned bit NOT NULL,
	IsApproved bit,
	CONSTRAINT [PK_dbo.AspNetUsers] PRIMARY KEY (Id)
);

CREATE UNIQUE INDEX UserNameIndex ON AspNetUsers (UserName);
GO

CREATE TABLE AspNetUserClaims (
	Id int IDENTITY(1, 1) NOT NULL,
	UserId nvarchar(128) NOT NULL,
	ClaimType nvarchar(max),
	ClaimValue nvarchar(max),
	CONSTRAINT [PK_dbo.AspNetUserClaims] PRIMARY KEY (Id),
	CONSTRAINT [FK_dbo.AspNetUserClaims_dbo.AspNetUsers_UserId] FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);
CREATE INDEX IX_UserId ON AspNetUserClaims (UserId);

CREATE TABLE AspNetUserLogins (
	LoginProvider nvarchar(128) NOT NULL,
	ProviderKey nvarchar(128) NOT NULL,
	UserId nvarchar(128) NOT NULL,
	CONSTRAINT [PK_dbo.AspNetUserLogins] PRIMARY KEY (LoginProvider, ProviderKey, UserId),
	CONSTRAINT [FK_dbo.AspNetUserLogins_dbo.AspNetUsers_UserId] FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);
CREATE INDEX IX_UserId ON AspNetUserLogins (UserId);

CREATE TABLE AspNetRoles (
	Id nvarchar(128) NOT NULL,
	Name nvarchar(256) NOT NULL,
	CONSTRAINT [PK_dbo.AspNetRoles] PRIMARY KEY (Id)
);
CREATE UNIQUE INDEX RoleNameIndex ON AspNetRoles (Name);

INSERT INTO AspNetRoles (Id, Name) 
VALUES (NEWID(), 'GlobalAdmin');

CREATE TABLE AspNetUserRoles (
	UserId nvarchar(128) NOT NULL,
	RoleId nvarchar(128) NOT NULL,
	CONSTRAINT [PK_dbo.AspNetUserRoles] PRIMARY KEY (UserId, RoleId),
	CONSTRAINT [FK_dbo.AspNetUserRoles_dbo.AspNetRoles_RoleId] FOREIGN KEY (RoleId) REFERENCES AspNetRoles (Id) ON DELETE CASCADE,
	CONSTRAINT [FK_dbo.AspNetUserRoles_dbo.AspNetUsers_UserId] FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);
CREATE INDEX IX_RoleId ON AspNetUserRoles (RoleId);
CREATE INDEX IX_UserId ON AspNetUserRoles (UserId);
GO

/* Начален потребител admin с парола admin и роля GlobalAdmin. */
INSERT INTO AspNetUsers (Id, UserName, PasswordHash, SecurityStamp, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, IsBanned, IsApproved)
VALUES (NEWID(), 'admin', 'AEBjbLIhxkzdNXXXTj4jMhLEzUkhga/TX7lDgE5+zcSplfOu7AzBs4rD7eGX4G02Xg==', '8d6be3ef-fd03-41f7-994f-7df000150ccb', 1, 0, 0, 1, 0, 0, 1);
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id FROM AspNetUsers u, AspNetRoles r WHERE u.UserName = 'admin' and r.Name = 'GlobalAdmin';

CREATE TABLE EAuth (
	RequestId varchar(100) NOT NULL CONSTRAINT PK_EAuth PRIMARY KEY,
	RequestDateTime datetime2 NOT NULL CONSTRAINT CH_EAuth_RequestDateTime CHECK (RequestDateTime BETWEEN '2000-01-01' AND '2100-01-01'),
	RequestSaml nvarchar(max) NOT NULL,
	RelayState nvarchar(max),  -- Към 2018-02 не се използва. Просто се поддържа от SAML протокола.
	UserId nvarchar(128) CONSTRAINT FK_EAuth_AspNetUsers FOREIGN KEY REFERENCES AspNetUsers (Id) ON DELETE SET NULL,
	ResponseDateTime datetime2,
	ResponseSaml nvarchar(max),
	Error nvarchar(max),
	-- Информация, извлечена от SAML response-а.
	PidTypeCode varchar(20) CONSTRAINT FK_EAuth_KeyType FOREIGN KEY REFERENCES N.KeyType (Code) ON UPDATE CASCADE,
    PersonIdentifier nvarchar(100),
	PersonName nvarchar(100),
	Email nvarchar(100),
	Phone nvarchar(100),
	ExpirationDateTime datetime2,  -- Тази дата се връща от еАвт и би могла да бъде всякаква, затова CHECK CONSTRAINT не е приложим.
	CONSTRAINT CH_EAuth_ResponseDateTime CHECK (ResponseDateTime IS NULL OR (ResponseDateTime BETWEEN '2000-01-01' AND '2100-01-01' AND ResponseDateTime >= RequestDateTime)),
	CONSTRAINT CH_EAuth_HasResponse CHECK (
		ResponseDateTime IS NULL AND Error IS NULL AND ResponseSaml IS NULL AND
			PidTypeCode IS NULL AND PersonIdentifier IS NULL AND PersonName IS NULL AND
			Email IS NULL AND Phone IS NULL AND ExpirationDateTime IS NULL OR
		ResponseDateTime IS NOT NULL AND (Error IS NOT NULL AND Error <> '' OR ResponseSaml IS NOT NULL)
	)
);

CREATE TABLE N.Config (
	Code varchar(100) NOT NULL CONSTRAINT PK_Config PRIMARY KEY,
	Value nvarchar(max),
);

INSERT INTO N.Config (Code, Value) VALUES
-- SMTP настройки за разработка:
('SmtpHost', 'email.kontrax.bg'),
('SmtpPort', NULL),
('SmtpEnableSsl', NULL),
('SmtpUserName', ''),
('SmtpPassword', NULL),
('SmtpFromEmail', 'RegUX Dev <RegUX.Dev@kontrax.bg>'),
/*
-- SMTP настройки за тестване от Контракс:
('SmtpHost', 'cas.kontrax.local'),
('SmtpPort', NULL),
('SmtpEnableSsl', NULL),
('SmtpUserName', 'devtest1'),
('SmtpPassword', 'DevTest_1'),
('SmtpFromEmail', 'RegUX Test <RegUX.Test@kontrax.bg>'),
-- SMTP настройки в production:
('SmtpHost', 'mail.egov.bg'),
('SmtpPort', NULL),
('SmtpEnableSsl', NULL),
('SmtpUserName', '?????'),
('SmtpPassword', '?????'),
('SmtpFromEmail', 'RegUX <regux@egov.bg>'),
*/
('IisdaUpdateStart', NULL),
('IisdaUpdateServicesError', NULL),
('IisdaUpdateActivitiesError', NULL);


CREATE TABLE N.AdministrationKind (
	Code varchar(100) NOT NULL CONSTRAINT PK_AdministrationKind PRIMARY KEY,
	Name nvarchar(100) NOT NULL
);

INSERT INTO N.AdministrationKind (Code, Name) VALUES
-- Type COM:
('CouncilOfMinistersAdministration', N'Министерски съвет и неговата администрация'),
-- Type AdmStructure:
('RegionalAdminisration', N'Областни администрации'),
('MunicipalAdministration', N'Общински администрации'),
('AreaMunicipalAdministration', N'Районни администрации'),
('SpecializedLocalAdministration', N'Специализирани териториални администрации, създадени като юридически лица с нормативен акт'),
('Ministry', N'Министерства'),
('StateAgency', N'Държавни агенции'),
('StateCommisionAdministrion', N'Администрации на държавни комисии'),
('ExecutiveAgency', N'Изпълнителни агенции'),
('ExecutivePowerAdministrativeStructure', N'Административни структури, създадени със закон'),
('Act60AdiministrativeStructure', N'Административни структури, създадени с постановление на МС'),
-- Type AdvisoryBoard:
('Council', N'Съвети'),
('StatePublicConsultativeCommission', N'Държавно-обществени консултативни комисии');

-- Администрациите, услугите, правата и журналът са ядрото на този софтуер,
-- затова не се водят като номенклатурни и security таблици, а като основни.

CREATE TABLE Administration (
	Id int NOT NULL IDENTITY(1, 1) CONSTRAINT PK_Administration PRIMARY KEY,
	Name nvarchar(1000) NOT NULL,
	Number bigint,  -- Идентификатор в ИИСДА. Почти винаги съвпада с BatchId, но има 31 изключения. Изписва се с водещи нули в 10 позиции.
	BatchId bigint,  -- По-рядко използван идентификатор в ИИСДА.
	KindCode varchar(100) CONSTRAINT FK_Administration_AdministrationKind FOREIGN KEY REFERENCES N.AdministrationKind (Code) ON UPDATE CASCADE,
	Uic varchar(13),  -- ЕИК.
	IsClosed bit NOT NULL,
	-- Пример: 2.16.100.1.1.1.1.15
	Oid varchar(100),
	IsPublicServiceProvider bit NOT NULL
);

CREATE TABLE AdministrationCertificate (
	AdministrationId int NOT NULL CONSTRAINT FK_AdministrationCertificate_Administration FOREIGN KEY REFERENCES Administration (Id) ON DELETE CASCADE,
	TypeCode varchar(20) NOT NULL CONSTRAINT CH_AdministrationCertificate_Type CHECK (TypeCode IN ('RegiX', 'Root', 'ProposedRoot')),
	Data varbinary(max) NOT NULL,
	Password nvarchar(100),
	CONSTRAINT PK_AdministrationCertificate PRIMARY KEY (AdministrationId, TypeCode)
);

CREATE TABLE N.AccessLevel (
	Code varchar(20) NOT NULL CONSTRAINT PK_AccessLevel PRIMARY KEY,
	Name nvarchar(100) NOT NULL
);

INSERT INTO N.AccessLevel (Code, Name) VALUES
('Admin', N'Администратор'),
('Manager', N'Ръководител'),
('Employee', N'Служител');

-- Групира подмножество от услугите, до които дадена администрация има достъп.
CREATE TABLE LocalRole (
	AdministrationId int NOT NULL CONSTRAINT FK_LocalRole_Administration FOREIGN KEY REFERENCES Administration (Id) ON DELETE CASCADE,
	Id int NOT NULL IDENTITY(1, 1),
	Name nvarchar(100) NOT NULL
	-- Администрацията е включена в primary key-я, за да не може работно място в една адмионистрация да ползва роля от друга.
	CONSTRAINT PK_LocalRole PRIMARY KEY (AdministrationId, Id)
);

CREATE TABLE Workplace (
	UserId nvarchar(128) NOT NULL CONSTRAINT FK_Workplace_AspNetUsers FOREIGN KEY REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
	AdministrationId int NOT NULL CONSTRAINT FK_Workplace_Administration FOREIGN KEY REFERENCES Administration (Id) ON DELETE CASCADE,
	AccessLevelCode varchar(20) NOT NULL CONSTRAINT FK_Workplace_AccessLevel FOREIGN KEY REFERENCES N.AccessLevel (Code) ON UPDATE CASCADE,
	-- Допълнително ограничение на правата. Ако не е указана роля, потребителят има всички права според нивото си на достъп.
	LocalRoleId int,
	CONSTRAINT PK_Workplace PRIMARY KEY (UserId, AdministrationId),
	CONSTRAINT FK_Workplace_LocalRole FOREIGN KEY (AdministrationId, LocalRoleId) REFERENCES LocalRole (AdministrationId, Id)
);

CREATE TABLE RegiXReport (
	Id int NOT NULL IDENTITY(1, 1) CONSTRAINT PK_RegiXReport PRIMARY KEY,
	ProviderName nvarchar(1000),  -- Имената администрациите понякога са дълги.
	RegisterName nvarchar(1000),
	ReportName nvarchar(1000) NOT NULL,  -- Имената услугите понякога са дълги.
	AdapterSubdirectory nvarchar(100) NOT NULL,  -- Пример: NKPDAdapter. Поддиректория в пътя, указан от настройката AdaptersPath, която съдържа всички .xsd и примерни .xml файлове за даден регистър.
	OperationName nvarchar(100), -- Пример: GetActualStateV2 - operationName от url адреса за теглене на файловете
	RequestXsd nvarchar(100),  -- Пример: AllNKPDDataRequest.xsd.
	ResponseXsd nvarchar(100),  -- Пример: AllNKPDDataResponse.xsd.
	RequestExampleXml nvarchar(100),  -- Пример: GetNKPDAllDataRequest.xml.
	ResponseExampleXml nvarchar(100),  -- Пример: GetNKPDAllDataResponse.xml.
	-- Най-дългият код на операция засега е 121 знака:
	-- 'TechnoLogica.RegiX.IaosRegister67ZOUAdapter.APIService.IIaosRegister67ZOUAPI.GetREG35_Stages_By_Reg_Number_And_Waste_Code'.
	Operation varchar(200),
	IsDeleted bit NOT NULL
);

CREATE TABLE RegiXReportKey (
	RegiXReportId int NOT NULL CONSTRAINT FK_RegiXReportKey_RegiXReport FOREIGN KEY REFERENCES RegiXReport (Id),
	TypeCode varchar(20) NOT NULL CONSTRAINT FK_RegiXReportKey_KeyType FOREIGN KEY REFERENCES N.KeyType (Code) ON UPDATE CASCADE,
	ElementName varchar(100) NOT NULL,
	CONSTRAINT PK_RegiXReportKey PRIMARY KEY (RegiXReportId, TypeCode),
);

CREATE TABLE LocalRoleRegiXReport (
	AdministrationId int NOT NULL,
	LocalRoleId int NOT NULL,
    RegiXReportId int NOT NULL CONSTRAINT FK_LocalRoleRegiXReport_RegiXReport FOREIGN KEY REFERENCES RegiXReport (Id) ON DELETE CASCADE,
	CONSTRAINT PK_LocalRoleRegiXReport PRIMARY KEY (AdministrationId, LocalRoleId, RegiXReportId),
	CONSTRAINT FK_LocalRoleRegiXReport_LocalRole FOREIGN KEY (AdministrationId, LocalRoleId) REFERENCES LocalRole (AdministrationId, Id) ON DELETE CASCADE
);

CREATE TABLE N.ServiceSection (
	Code varchar(100) NOT NULL CONSTRAINT PK_ServiceSection PRIMARY KEY,
	Name nvarchar(100) NOT NULL
);

INSERT INTO N.ServiceSection (Code, Name) VALUES
('CommonAdmServices', N'Услуги, предоставяни от всички администрации'),
('CentralAdmServices', N'Услуги, предоставяни от централни администрации'),
('DistrictAdmServices', N'Услуги, предоставяни от областни администрации'),
('MunicipalAdmServices', N'Услуги, предоставяни от общински администрации'),
('SpecializedAdmServices', N'Услуги, предоставяни от специализирани териториални администрации');

CREATE TABLE Service (
	Id int NOT NULL IDENTITY(1, 1) CONSTRAINT PK_Service PRIMARY KEY,
	Name nvarchar(2000) NOT NULL,  -- Има услуга с име, дълго 1011 знака.
	Number bigint,  -- Идентификатор в ИИСДА. 
	SectionCode varchar(100) NOT NULL CONSTRAINT FK_Service_ServiceSection FOREIGN KEY REFERENCES N.ServiceSection (Code) ON UPDATE CASCADE,
	Subsection nvarchar(1000),  -- Има подсекция с дължина на името 168 знака.
	LegalBasis nvarchar(max),  -- Правно основание.
	IsRegime bit NOT NULL,  -- TRUE ако е режим и FALSE ако е услуга.
	RegimeName nvarchar(100),  -- Лицензиране, Разрешение, Регистрация, Съгласуване, Уведомление, Удостоверение.
	IsEu bit NOT NULL,  -- Дали произтича от правото на ЕС.
	IsInternal bit NOT NULL,  -- Дали услугата е вътрешно административна.
	IsDeleted bit NOT NULL,
	IisdaUpdateDateTime datetime2 CONSTRAINT CH_Service_IisdaUpdateDateTime CHECK (IisdaUpdateDateTime BETWEEN '2000-01-01' AND '2100-01-01'),
	IisdaUpdateResult nvarchar(max),
	-- В RegiX има само около 150 справки. Част от тях може би съответстват на някоя от 2500-те услуги.
	RegiXReportId int CONSTRAINT FK_Service_RegiXReport FOREIGN KEY REFERENCES RegiXReport (Id)
);

CREATE UNIQUE INDEX IX_Service_RegiXReportId ON Service (RegiXReportId) WHERE RegiXReportId IS NOT NULL;

-- Дейности на администрациите. Повечето са "предоставяне на услугата ProvidedServiceId".
-- Тези дейности нямат собствено име, защото се показва името на предоставяната услуга.
-- Може да има обаче и вътрешни дейности, при които не се предоставя услуга - те трябва да имат собствено име.
CREATE TABLE Activity (
	Id int NOT NULL IDENTITY (1, 1) CONSTRAINT PK_Activity PRIMARY KEY,
	AdministrationId int NOT NULL CONSTRAINT FK_Activity_Administration FOREIGN KEY REFERENCES Administration (Id) ON DELETE CASCADE,
	Name nvarchar(1000),
	ProvidedServiceId int CONSTRAINT FK_Activity_Service FOREIGN KEY REFERENCES Service (Id),
	SearchAdmServiceMainData bit NOT NULL,
	SearchBatchesForAdmService bit NOT NULL
);

CREATE INDEX IX_Activity_Administration ON Activity (AdministrationId);

-- Услуги, от които зависи всяка дейност.
CREATE TABLE Dependency (
	ActivityId int NOT NULL CONSTRAINT FK_Dependency_Activity FOREIGN KEY REFERENCES Activity (Id) ON DELETE CASCADE,
	RegiXReportId int NOT NULL CONSTRAINT FK_Dependency_RegiXReport FOREIGN KEY REFERENCES RegiXReport (Id) ON DELETE CASCADE,
	StepNumber tinyint NOT NULL CONSTRAINT CH_Dependency_StepNumber_Between1And20 CHECK (StepNumber BETWEEN 1 and 20),
	LegalBasis nvarchar(max),  -- Правно основание администрацията, в рамките на някаква нейна дейност, да ползва услугата.
	CONSTRAINT PK_Dependency PRIMARY KEY (ActivityId, RegiXReportId),
	CONSTRAINT UK_Dependency_ActivityStep UNIQUE (ActivityId, StepNumber)
);

-- При липса на ActivityId, шаблонът се прилага по подразбиране - за всички администрации.
-- При наличие на ActivityId, шаблонът се прилага само за съответната администрация.
CREATE TABLE PrintTemplate (
	Id int NOT NULL IDENTITY (1, 1) CONSTRAINT PK_PrintTemplate PRIMARY KEY,
	RegiXReportId int NOT NULL CONSTRAINT FK_PrintTemplate_RegiXReport FOREIGN KEY REFERENCES RegiXReport (Id) ON DELETE CASCADE,
	ActivityId int,
	FileName nvarchar(1000) NOT NULL,
	FileContent varbinary(max) NOT NULL,
	CONSTRAINT UK_PrintTemplate UNIQUE (RegiXReportId, ActivityId),
	CONSTRAINT FK_PrintTemplate_Dependency FOREIGN KEY (ActivityId, RegiXReportId) REFERENCES Dependency (ActivityId, RegiXReportId)
);

CREATE TABLE Batch (
	Id int NOT NULL IDENTITY(1, 1) CONSTRAINT PK_Batch PRIMARY KEY,
	CreateDateTime datetime2 NOT NULL CONSTRAINT CH_Batch_DraftDateTime CHECK (CreateDateTime BETWEEN '2000-01-01' AND '2100-01-01'),
	UserId nvarchar(128) NOT NULL CONSTRAINT FK_Batch_AspNetUsers FOREIGN KEY REFERENCES AspNetUsers (Id),
	-- Извикваща администрация.
	AdministrationId int NOT NULL CONSTRAINT FK_Batch_Administration FOREIGN KEY REFERENCES Administration (Id),
	-- Предоставяната услуга или вътрешата дейност, за която са нужни удостоверения.
	ActivityId int NOT NULL CONSTRAINT FK_Batch_Activity FOREIGN KEY REFERENCES Activity (Id),
	-- Идентификатор на инстанцията на административната услуга или процедура в администрацията.
	-- Пример: номер на преписка 123-12345-01.01.2017
	ServiceUri nvarchar(100)
);

CREATE TABLE Request (
	Id int NOT NULL IDENTITY(1, 1) CONSTRAINT PK_Request PRIMARY KEY,
	BatchId int NOT NULL CONSTRAINT FK_Request_Batch FOREIGN KEY REFERENCES Batch (Id) ON DELETE CASCADE,
	-- Заявеният вид удостоверение, който е нужен за дейността. От него се взима RegiX операцията.
	RegiXReportId int NOT NULL CONSTRAINT FK_Request_RegiXReport FOREIGN KEY REFERENCES RegiXReport (Id),
	Argument nvarchar(max) NOT NULL,
	RequestTimeStamp varbinary(max),
	ResponseTimeStamp varbinary(max),
	PersonOrCompanyId nvarchar(100),
	StartDateTime datetime2 CONSTRAINT CH_Request_StartDateTime CHECK (StartDateTime IS NULL OR StartDateTime BETWEEN '2000-01-01' AND '2100-01-01'),
	EndDateTime datetime2,
	IsCanceled bit NOT NULL,
	Error nvarchar(max),
	CONSTRAINT CH_Request_EndDateTime CHECK (EndDateTime IS NULL OR (EndDateTime BETWEEN '2000-01-01' AND '2100-01-01' AND EndDateTime >= StartDateTime))
);

CREATE INDEX IX_Request_Batch_RegiXReport ON Request (BatchId, RegiXReportId);

-- Резултатът се разделя от заявката, за да не се зарежда без нужда от Entity Framework.
CREATE TABLE Response (
	RequestId int NOT NULL
		CONSTRAINT PK_Response PRIMARY KEY
		CONSTRAINT FK_Response_Request FOREIGN KEY REFERENCES Request (Id),
	Document nvarchar(max) NOT NULL
);
GO

-- Опит да се гарантира, че резултатът от извикването е консистентен.
-- Преди да се въведе дата на резултата, трябва да се добави ред в таблица Response или заедно с датата да се попълни грешка или отказ.
-- Отказана заявка не може да има Response или грешка. Крайната дата на отказана заявка всъщност е датата на отказа.
-- Обратно, ако няма дата на резултата, не трябва да има нито ред в таблица Response, нито грешка в колона Error, нито отказ.
-- Внимание: Ако редът от таблица Response бъде изтрит или променен, constraint–ът НЕ се преизчислява; нито ако се редактира колона, която не се споменава в constraint-а.
CREATE FUNCTION ResponseExists(@RequestId int)
RETURNS bit AS
BEGIN
	RETURN CASE WHEN EXISTS (SELECT 0 FROM Response WHERE RequestId = @RequestId) THEN 1 ELSE 0 END;
END
GO
ALTER TABLE Request ADD CONSTRAINT CH_Request_HasResponseOrIsCanceled CHECK (
	-- Ако няма отговор, нито грешка, крайната дата е попълнена точно когато заявката е отказана.
	Error IS NULL AND dbo.ResponseExists(Id) = 0 AND (IsCanceled = CASE WHEN EndDateTime IS NULL THEN 0 ELSE 1 END) OR
	-- Ако има отговор или грешка, крайната дата винати е попълнена и заявката не може да бъде отказана.
	(Error IS NOT NULL AND Error <> '' OR dbo.ResponseExists(Id) = 1) AND IsCanceled = 0 AND EndDateTime IS NOT NULL);

CREATE TABLE N.AuditType (
	Code varchar(20) CONSTRAINT PK_AuditType PRIMARY KEY,
	Name nvarchar(100) NOT NULL
);

INSERT INTO N.AuditType (Code, Name) VALUES
('Read', N'Четене'),
('Write', N'Запис'),
('LoginSuccess', N'Успешен вход'),
('LoginError', N'Грешка при вход'),
('RegiXRequest', N'Заявка към RegiX'),
('RegiXResponse', N'Отговор от RegiX'),
('RegiXError', N'Грешка от RegiX');

CREATE TABLE Audit (
	Id int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Audit PRIMARY KEY,
	UserId nvarchar(128) NULL CONSTRAINT FK_Audit_AspNetUsers FOREIGN KEY REFERENCES AspNetUsers (Id),
	DateTime datetime2(7) NOT NULL,
	IpAddress nvarchar(100),
	Url nvarchar(1000),
	Data nvarchar(max),
	Notes nvarchar(max),
	Duration bigint,
	UserName nvarchar(256),
	Controller nvarchar(1000),
	Action nvarchar(1000),
	SessionId nvarchar(1000),
	RequestMethod nvarchar(100),
	Hash varbinary(128),
	AuditTypeCode varchar(20) NOT NULL CONSTRAINT FK_Audit_AuditType FOREIGN KEY REFERENCES N.AuditType (Code),
	RequestId int CONSTRAINT FK_Audit_Request FOREIGN KEY REFERENCES Request (Id),
	EntityName varchar(100),
	EntityRecordId nvarchar(100)
);

CREATE INDEX IX_Audit_DateTime ON Audit (DateTime);
CREATE INDEX IX_Audit_AuditTypeCode ON Audit (AuditTypeCode);

CREATE TABLE AuditDetail (
	Id int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_AuditDetail PRIMARY KEY NONCLUSTERED,
	AuditId int NOT NULL CONSTRAINT FK_AuditDetail_Audit FOREIGN KEY (AuditId) REFERENCES Audit (Id) ON DELETE CASCADE,
	AuditDetailType char(1) NOT NULL, -- enum AuditLogDetailType (C, U, D)
	EntityName varchar(100) NOT NULL,
	RecordId nvarchar(100) NOT NULL,
	PropertyName varchar(100) NOT NULL,
	OriginalValue nvarchar(max),
	NewValue nvarchar(max),
	OriginalValueDescription nvarchar(max),
	NewValueDescription nvarchar(max)
);

CREATE TABLE AuditConfig (
	EntityName varchar(100) NOT NULL,
	PropertyName varchar(100) NOT NULL, -- Слагам string.Empty, ако преводът е име на таблица, а не на колона - за да има индекс
	Translation nvarchar(100),
	Mapping varchar(100),
	CONSTRAINT PK_AuditConfig PRIMARY KEY (EntityName, PropertyName)
);

GO

CREATE VIEW AuditView AS
SELECT 
	a.Action,
	a.Controller,
	a.Data,
	a.Duration,
	a.Id,
	a.IpAddress,
	a.SessionId,
	a.DateTime,
	a.Url,
	a.UserId,
	a.UserName,
	a.Notes,
	a.RequestMethod,
	a.AuditTypeCode,
	at.Name AuditTypeName,
	a.EntityName,
	a.EntityRecordId,
	a.Hash,
	a.RequestId,
	b.UserId RequestUserId,
	b.AdministrationId RequestAdministrationId,
	b.ActivityId RequestActivityId,
	b.ServiceUri RequestServiceUri,
	req.RegiXReportId RequestRegiXReportId,
	req.Argument RequestArgument,
	req.StartDateTime RequestStartDateTime,
	req.EndDateTime RequestEndDateTime,
	req.Error RequestError,
	res.Document ResponseDocument,
	-- По неясна причина само "SELECT MAX(Id)..." работи бързо. "SELECT TOP 1 Id..." е бавно. "OUTER APPLY (SELECT TOP 1 Id, Hash...)" също е бавно.
	(SELECT MAX(Id) FROM Audit p WHERE p.Id < a.Id) PreviousId,
	(SELECT TOP(1) Hash FROM Audit p WHERE p.Id < a.Id ORDER BY p.Id DESC) PreviousHash
FROM Audit a
LEFT JOIN Request req
ON (a.RequestId = req.Id)
LEFT JOIN Response res
ON (req.Id = res.RequestId)
LEFT JOIN Batch b
ON req.BatchId = b.Id
LEFT JOIN N.AuditType at
ON (a.AuditTypeCode = at.Code)
GO

CREATE SCHEMA Pub;
GO

CREATE TABLE Pub.Signal (
	Id int NOT NULL IDENTITY(1, 1) CONSTRAINT PK_Signal PRIMARY KEY,
	SubmitDateTime datetime2 NOT NULL CONSTRAINT CH_Signal_SubmitDateTime CHECK (SubmitDateTime BETWEEN '2000-01-01' AND '2100-01-01'),
	IncidentDateTime datetime2 NOT NULL CONSTRAINT CH_Signal_IncidentDateTime CHECK (IncidentDateTime BETWEEN '2000-01-01' AND '2100-01-01'),
	ServiceId int NOT NULL CONSTRAINT FK_Signal_Service FOREIGN KEY REFERENCES Service (Id),
	-- Не е задължително да се избира служител, защото подателят може да не знае името му.
	EmployeeId nvarchar(128) CONSTRAINT FK_Signal_AspNetUsers FOREIGN KEY REFERENCES AspNetUsers (Id),
	-- Ако подателят знае част от името на служителя, но не може да го намери в списъка.
	EmployeeName nvarchar(100),
    AdministrationId int NOT NULL CONSTRAINT FK_Signal_Administration FOREIGN KEY (AdministrationId) REFERENCES dbo.Administration (Id), 
	SenderId nvarchar(128) NOT NULL CONSTRAINT FK_Signal_Sender FOREIGN KEY REFERENCES dbo.AspNetusers (Id),
	SenderContact nvarchar(max),
	SenderNote nvarchar(max),
	IsProposal bit NOT NULL,
	ResolveDateTime datetime2 CONSTRAINT CH_Signal_ResolveDateTime CHECK (ResolveDateTime IS NULL OR ResolveDateTime BETWEEN '2000-01-01' AND '2100-01-01'),
	ResolverNote nvarchar(max)
);
