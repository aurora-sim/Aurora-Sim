Migrations = 1

CREATE TABLE usernotes (useruuid VARCHAR(50), targetuuid VARCHAR(50),notes VARCHAR(512),noteUUID VARCHAR(50) PRIMARY KEY);

CREATE TABLE userpicks (pickuuid VARCHAR(50) PRIMARY KEY, creatoruuid VARCHAR(50),toppick VARCHAR(512),parceluuid VARCHAR(50),name VARCHAR(50),description VARCHAR(50),snapshotuuid VARCHAR(50),user VARCHAR(50),originalname VARCHAR(50),simname VARCHAR(50),posglobal VARCHAR(50),sortorder VARCHAR(50),enabled VARCHAR(50));

CREATE TABLE usersauth (userUUID VARCHAR(50) PRIMARY KEY, userLogin VARCHAR(50),userFirst VARCHAR(512),userLast VARCHAR(50),userEmail VARCHAR(50),userPass VARCHAR(50),userMac VARCHAR(50),userIP VARCHAR(50),userAcceptTOS VARCHAR(50),userGodLevel VARCHAR(50),userRealFirst VARCHAR(50),userRealLast VARCHAR(50),userAddress VARCHAR(50),userZip VARCHAR(50),userCountry VARCHAR(50),tempBanned VARCHAR(50),permaBanned VARCHAR(50),profileAllowPublish VARCHAR(50),profileMaturePublish VARCHAR(50),profileURL VARCHAR(50), AboutText VARCHAR(50), Email VARCHAR(50), CustomType VARCHAR(50), profileWantToMask VARCHAR(50),profileWantToText VARCHAR(50),profileSkillsMask VARCHAR(50),profileSkillsText VARCHAR(50),profileLanguages VARCHAR(50),visible VARCHAR(50),imviaemail VARCHAR(50),membershipGroup VARCHAR(50),FirstLifeAboutText VARCHAR(50),FirstLifeImage VARCHAR(50),Partner VARCHAR(50), Image VARCHAR(50), AArchiveName VARCHAR(50), IsNewUser VARCHAR(50));



CREATE TABLE classifieds (classifieduuid VARCHAR(50) PRIMARY KEY, creatoruuid VARCHAR(50),creationdate VARCHAR(512),expirationdate VARCHAR(50),category VARCHAR(50),name VARCHAR(50),description VARCHAR(50),parceluuid VARCHAR(50),parentestate VARCHAR(50),snapshotuuid VARCHAR(50),simname VARCHAR(50),posglobal VARCHAR(50),parcelname VARCHAR(50),classifiedflags VARCHAR(50),priceforlisting VARCHAR(50));

CREATE TABLE auroraregions (regionName VARCHAR(50), regionHandle VARCHAR(50),hidden VARCHAR(1),regionUUID VARCHAR(50) PRIMARY KEY,regionX VARCHAR(50),regionY VARCHAR(50),telehubX VARCHAR(50),telehubY VARCHAR(50));

CREATE TABLE macban (macAddress VARCHAR(50) PRIMARY KEY);

CREATE TABLE BannedViewers (Client VARCHAR(50) PRIMARY KEY);

CREATE TABLE mutelists (userID VARCHAR(50) ,muteID VARCHAR(50),muteName VARCHAR(50),muteType VARCHAR(50),muteUUID VARCHAR(50) PRIMARY KEY);

CREATE TABLE abusereports (Category VARCHAR(100) ,AReporter VARCHAR(100),OName VARCHAR(100),OUUID VARCHAR(100),AName VARCHAR(100) PRIMARY KEY,ADetails VARCHAR(100),OPos VARCHAR(100),Estate VARCHAR(100),Summary VARCHAR(100));

CREATE TABLE assetMediaURL (objectUUID VARCHAR(100), User VARCHAR(100), alt_image_enable VARCHAR(100), auto_loop VARCHAR(100), auto_play VARCHAR(100), auto_scale VARCHAR(100), auto_zoom VARCHAR(100), controls VARCHAR(100), current_url VARCHAR(100), first_click_interact VARCHAR(100), height_pixels VARCHAR(100), home_url VARCHAR(100), perms_control VARCHAR(100), perms_interact VARCHAR(100), whitelist VARCHAR(100), whitelist_enable VARCHAR(100), width_pixels VARCHAR(100), object_media_version VARCHAR(100), count VARCHAR(100), PRIMARY KEY (object_media_version, objectUUID) );

CREATE TABLE  auroraprims (primUUID varchar(45) PRIMARY KEY,primName varchar(45),primType varchar(2),primKeys varchar(1024),primValues varchar(1024) );

CREATE TABLE aurorainventoryfolders (FolderUUID VARCHAR(50) PRIMARY KEY, parentID VARCHAR(50), PreferredAssetType VARCHAR(50), Parent VARCHAR(50), Name VARCHAR(50));

CREATE TABLE Migrations (migrations VARCHAR(50) PRIMARY KEY);
insert into Migrations Values('1');