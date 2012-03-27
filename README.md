# AURORA

The Aurora Development Team is proud to present the release of Aurora virtual world server.
 The Aurora server is an OpenSim derived project with heavy emphasis on supporting all users, 
 increased technology focus, heavy emphasis on working with other developers,
 whether it be viewer based developers or server based developers, 
 and a set of features around stability and simplified usability for users.

We aren’t just releasing new features, but a new outlook on a virtual world development for the average human.
 We encourage you to read our manifesto (http://aurora-sim.org/index.php?option=com_content&view=article&id=20&Itemid=60)
 and learn about our direction to make the people’s choice in virtual server!

## Compiling Aurora
*To compile Aurora, look at BUILDING.txt for more information.*

## Configuration
*Configuration in Aurora is a bit different than in other distributions of OpenSim.*
In Aurora, all configuration files (except for AuroraServer.ini and Aurora.ini) have been moved to the AuroraServerConfiguration and Configuration folders (respectively) and further subdivided from there into categories of .ini files.

### Config file rundown
*Note - All file paths are from the Configuration directory*

#### Configuration folder
*	Main.ini -  Contains the settings to switch between standalone, grid mode, and Simian.

#### Data folder
*	Data.ini -  Settings to switch between database modules
*	MySQL.ini -  Settings for the MySQL Database.
*	SQLite.ini -  Settings for the SQLite Database.

#### Grid folder
*	AuroraGridCommon.ini -  Contains files that are used in grid mode.

#### Modules folder
*	Advanced.ini -  Contains settings including the prioritizer, LLUDP server settings, and packet pool.
*	AssetCache.ini - Contains the asset cache settings.
*	AuroraModules.ini -  Contains all the settings for the Aurora Modules, including the Aurora Profile and Search plugins, Map Module, World Terrain settings, Chat, Messaging, Display Names, and more.
*	Concierge.ini -  Contains the concierge chat plugin settings.
*	Economy.ini -  Contains settings that have to do with the economy module in Aurora.
*	Groups.ini -  Contains all group module configuration.
*	InstanceSettings.ini -  Contains settings that have to deal with the OpenRegionSettings module.
*	IRC.ini -  Contains settings that deal with the IRC chat module connector.
*	Nature.ini -  Contains cloud, wind, sun, and tree settings.
*	Permissions.ini -  This sets up the permission modules and who can create scripts.
*	Protection.ini -  This deals with banning, restarting Aurora automatically, auto OAR backup, and more.
*	RemoteAdmin.ini -  This contains all the Remote Admin settings.
*	Search.ini -  This configures the DataSnapshot module which is used for alternative search plugins.
*	SMTPEmail.ini -  This sets up the Email module.
*	Startup.ini -  This contains settings including Default object name, checking for updates, error reporting, MegaRegions, Persistence, and Animations.
*	Stats.ini -  Sets up the optional stats module.
*	VoiceModules.ini -  Contains the voice modules configurations.

#### Physics folder
*	Physics.ini -  Contains settings to enable/disable physics plugins.
*	Meshing.ini -  Contains meshing settings.

#### Scripting folder
*	Scripting.ini -  Contains settings to enable/disable scripting plugins.
*	MRM.ini -  Contains MRM settings.
*	AuroraDotNetEngine.ini -  Contains ADNE (the script engine) settings.

#### Simian folder
*	Simian.ini -  Contains files that are used in Simian grid mode.

#### Standalone folder
*	StandaloneCommon.ini -  Contains files that are used in Standalone mode.

### Standalone Configuration
To run Aurora, you will need to edit a config file for it to run. This file is:

bin/Aurora.ini

*	You will need to modify the [Network] section, specifically the HostName parameter.
*	By default, the line is "HostName = http://127.0.0.1" and if you want to allow other users besides yourself to connect to your instance, you will need to change this to your external IP, which can be found  at http://www.whatsmyip.org/.


## Starting Aurora
To Start the simulator part of Aurora, just double click on Aurora.exe (if you are running a 64 bit machine and operating system, click on Aurora.32bitLauncher.exe). It will run for a bit until you come to a screen that will help you interactively configure your new region.

## Connecting to Aurora

To connect to the simulator with Imprudence, you must add a new grid to the Grid Manager.
1.	Click on the Grid Manager button, and in the new popup, click "Add".
2.	In the loginURI space, put "http://<IP>:9000/" where "<IP>" is your IP.
3.	If you arn't sure what your IP is, you can check at http://www.whatsmyip.org/. 
4.	After this, set a name for your grid in the Grid Name area.
5.	Then press apply and close the box. Select the grid you just created and then login with your username and password and enjoy!

## Router issues
If you are having issues logging into your simulator, take a look at http://forums.osgrid.org/viewtopic.php?f=14&t=2082 in the Router Configuration section for more information on ways to resolve this issue.
