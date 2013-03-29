#!/bin/sh
cd `dirname $0`
mono bin/Prebuild.exe /target vs2010 /targetframework v4_0 /conditionals "LINUX;NET_4_0"
if [ -d ".git" ]; then git log --pretty=format:"Aurora (%cd.%h)" --date=short -n 1 > bin/.version; fi
