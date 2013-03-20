#!/bin/sh
echo Configuring Aurora-Sim
wait 3
mono bin/Prebuild.exe /target vs2010 /targetframework v4_0 /conditionals LINUX\;NET_4_0
if [ -d ".git" ]; then git log --pretty=format:"Aurora (%cd.%h)" --date=short -n 1 > bin/.version; fi
wait 3
echo Building Aurora-Sim 
xbuild /property:DefineConstants="LINUX NET_4_0"
echo Finished Building Aurora
echo Thank you for choosing Aurora-Sim
echo Please report any errors to out Mantis Bug Tracker http://mantis.aurora-sim.org/
wait 5
