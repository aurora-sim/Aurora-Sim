#!/bin/sh
echo Configureing Aurora-Sim
wait 3
mono bin/Prebuild.exe /target vs2010 /targetframework v4_0 /conditionals NET_4_0 LINUX
if [ -d ".git" ]; then git log --pretty=format:"Aurora (%cd.%h)" --date=short -n 1 > bin/.version; fi
wait 3
echo Building Aurora-Sim 
wait 3
xbuild /property:DefineConstants="LINUX NET_4_0"
wait 3
echo Finished Building Aurora
wait 3
echo Thank you for choosing Aurora-Sim
wait 3
echo Please report any errors to out Mantis Bug Tracker http://mantis.aurora-sim.org/