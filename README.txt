===========  AURORA ==============
The Aurora Development Team is proud to present the release of Aurora virtual world server. The Aurora server is an OpenSim derived project with heavy emphasis on supporting all users, increased technology focus, heavy emphasis on working with other developers, whether it be viewer based developers or server based developers, and a set of features around stability and simplified usability for users.

We aren’t just releasing new features, but a new outlook on a virtual world development for the average human. We encourage you to read our manifesto (https://github.com/MatrixSmythe/Aurora/wikis) and learn about our direction to make the people’s choice in virtual server!

---- Compiling Aurora ----

- To compile Aurora, look at BUILDING.txt for more information.

---- Configuration ----

- Configuration in Aurora is a bit different than in other distributions of OpenSim. In Aurora, all configuration files (except for Robust.ini and OpenSim.ini) have been moved to the Configuration folder and further subdivided from there into categories of .ini files. 

--- Config file rundown ---

*Note - All file paths are from the Configuration directory

-- Configuration folder -- 

- Main.ini -  Contains the settings to switch between standalone, grid mode, and Simian.

-- End Configuration folder -- 
-- Data folder -- 

- Data.ini -  Settings to switch between database modules
- MSSQL.ini -  Settings for the MSSQL Database
- MySQL.ini -  Settings for the MySQL Database.
- SQLite.ini -  Settings for the SQLite Database.

-- End Data folder --
-- Grid folder --

- GridCommon.ini -  Contains files that are used in grid mode.

-- End Grid folder --
-- Modules folder --

- Advanced.ini -  Contains settings including the prioritizer, LLUDP server settings, and packet pool.
- AssetCache.ini - Contains the asset cache settings.
- AuroraModules.ini -  Contains all the settings for the Aurora Modules, including the Aurora Profile and Search plugins, Map Module, World Terrain settings, Chat, Messaging, Display Names, and more.
- Concierge.ini -  Contains the concierge chat plugin settings.
- Economy.ini -  Contains settings that have to do with the economy module in Aurora.
- Groups.ini -  Contains all group module configuration.
- InstanceSettings.ini -  Contains settings that have to deal with the OpenRegionSettings module.
- IRC.ini -  Contains settings that deal with the IRC chat module connector.
- Nature.ini -  Contains cloud, wind, sun, and tree settings.
- OSGridModules.ini -  This contains the settings for modules that are not used in Aurora, but have been included to easily set up alternate profile and search modules.
- Permissions.ini -  This sets up the permission modules and who can create scripts.
- Protection.ini -  This deals with banning, restarting Aurora automatically, auto OAR backup, and more.
- RemoteAdmin.ini -  This contains all the Remote Admin settings.
- Search.ini -  This configures the DataSnapshot module which is used for alternative search plugins.
- SMTPEmail.ini -  This sets up the Email module.
- Startup.ini -  This contains settings including Default object name, checking for updates, error reporting, MegaRegions, Persistence, and Animations.
- Stats.ini -  Sets up the optional stats module.
- SVNBackup.ini -  Sets up the SVN module.
- VoiceModules.ini -  Contains the voice modules configurations.

-- End Modules folder --
-- Physics folder --

- Physics.ini -  Contains settings to enable/disable physics plugins.
- Meshing.ini -  Contains meshing settings.

-- End Physics folder --
-- Scripting folder --

- Scripting.ini -  Contains settings to enable/disable scripting plugins.
- MRM.ini -  Contains MRM settings.
- AuroraDotNetEngine.ini -  Contains ADNE settings.

-- End Scripting folder --
-- Simian folder --

- Simian.ini -  Contains files that are used in Simian grid mode.
- HyperSimian.ini -  Contains files that are used in Hypergrid enabled Simian grid mode.

-- End Simian folder --
-- Standalone folder --

- StandaloneCommon.ini -  Contains files that are used in Standalone mode.

-- End Standalone folder --


---- Starting Aurora ----

- To Start the simulator part of Aurora, just double click on OpenSim.exe (if you are running a 64 bit machine and operating system, click on OpenSim.32bitLauncher.exe). It will run for a bit until you come to a screen that will help you interactively configure your new region.

---- Connecting to Aurora ----

- To connect to the simulator with Imprudence, you must add a new grid to the Grid Manager. Click on the Grid Manager button, and in the new popup, click "Add". In the loginURI space, put "http://<IP>:9000/" where "<IP>" is your IP. If you arn't sure what your IP is, you can check at http://www.whatsmyip.org/. After this, set a name for your grid in the Grid Name area. Then press apply and close the box. Select the grid you just created and then login with your username and password and enjoy!


---- Router issues ----

- If you are having issues logging into your simulator, take a look at 
http://forums.osgrid.org/viewtopic.php?f=14&t=2082
in the Router Configuration section for more information about how to resolve this issue.

========= OPENSIM ===========

Welcome to OpenSim!

==================
==== OVERVIEW ====
==================

OpenSim is a BSD Licensed Open Source project to develop a functioning
virtual worlds server platform capable of supporting multiple clients
and servers in a heterogeneous grid structure. OpenSim is written in
C#, and can run under Mono or the Microsoft .NET runtimes.

=========================
=== Compiling OpenSim ===
=========================

Please see BUILDING.txt if you downloaded a source distribution and 
need to build OpenSim before running it.

==================================
=== Running OpenSim on Windows ===
==================================

We recommend that you run OpenSim from a command prompt on Windows in order
to capture any errors.

To run OpenSim from a command prompt

 * cd to the bin/ directory where you unpacked OpenSim
 * run OpenSim.exe

Now see the "Configuring OpenSim" section

================================
=== Running OpenSim on Linux ===
================================

You will need Mono >= 2.4.2 to run OpenSim.  On some Linux distributions you
may need to install additional packages.  See http://opensimulator.org/wiki/Dependencies
for more information.

To run OpenSim, from the unpacked distribution type:

 * cd bin
 * mono OpenSim.exe

Now see the "Configuring OpenSim" section

===========================
=== Configuring OpenSim ===
===========================

When OpenSim starts for the first time, you will be prompted with a
series of questions that look something like:

[09-17 03:54:40] DEFAULT REGION CONFIG: Simulator Name [OpenSim Test]:

For all the options except simulator name, you can safely hit enter to accept
the default if you want to connect using a client on the same machine or over
your local network.

You will then be asked "Do you wish to join an existing estate?".  If you're
starting OpenSim for the first time then answer no (which is the default) and
provide an estate name.

Shortly afterwards, you will then be asked to enter an estate owner first name,
last name, password and e-mail (which can be left blank).  Do not forget these
details, since initially only this account will be able to manage your region
in-world.  You can also use these details to perform your first login.

Once you are presented with a prompt that looks like:

  Region (My region name) #

You have successfully started OpenSim.

If you want to create another user account to login rather than the estate
account, then type "create user" on the OpenSim console and follow the prompts.

Helpful resources:
 * http://opensimulator.org/wiki/Configuration
 * http://opensimulator.org/wiki/Configuring_Regions

==================================
=== Connecting to your OpenSim ===
==================================

By default your sim will be available for login on port 9000.  You can login by
adding -loginuri http://127.0.0.1:9000 to the command that starts Second Life
(e.g. in the Target: box of the client icon properties on Windows).  You can
also login using the network IP address of the machine running OpenSim (e.g.
http://192.168.1.2:9000)

To login, use the avatar details that you gave for your estate ownership or the
one you set up using the "create user" command.

===================
=== Bug reports ===
===================

In the very likely event of bugs biting you (err, your OpenSim) we
encourage you to see whether the problem has already been reported on
the OpenSim mantis system. You can find the OpenSim mantis system at

    http://opensimulator.org/mantis/main_page.php

If your bug has already been reported, you might want to add to the
bug description and supply additional information.

If your bug has not been reported yet, file a bug report ("opening a
mantis"). Useful information to include:
 * description of what went wrong
 * stack trace
 * OpenSim.log (attach as file)
 * OpenSim.ini (attach as file)
 * if running under mono: run OpenSim.exe with the "--debug" flag:

       mono --debug OpenSim.exe

===================================
=== More Information on OpenSim ===
===================================

More extensive information on building, running, and configuring
OpenSim, as well as how to report bugs, and participate in the OpenSim
project can always be found at http://opensimulator.org.

Thanks for trying OpenSim, we hope it is a pleasant experience.
