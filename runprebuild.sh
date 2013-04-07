#!/bin/sh
ARCH="x86"
CONFIG="Debug"
BUILD=false

USAGE="[-c <config>] -a <arch>"
LONG_USAGE="Configuration options to pass to prebuild environment

Options:
  -c|--config Build configuration Debug(default) or Release
  -a|--arch Architecture to target x86(default), x64, or AnyCPU
"

while case "$#" in 0) break ;; esac
do
  case "$1" in
    -c|--config)
      shift
      CONFIG="$1"
      ;;
    -a|--arch)
      shift
      ARCH="$1"
      ;;
    -b|--build)
      BUILD=true
      ;;
    -h|--help)
      echo "$USAGE"
      echo "$LONG_USAGE"
      exit
      ;;
    *)
      echo "Illegal option!"
      echo "$USAGE"
      echo "$LONG_USAGE"
      exit
      ;;
  esac
  shift
done

echo Configuring Aurora-Sim

mono bin/Prebuild.exe /target vs2010 /targetframework v4_0 /conditionals "LINUX;NET_4_0"
if [ -d ".git" ]; then git log --pretty=format:"Aurora (%cd.%h)" --date=short -n 1 > bin/.version; fi

if ${BUILD:=true} ; then
  echo Building Aurora-Sim
  xbuild /property:Configuration="$CONFIG" /property:Platform="$ARCH"
  echo Finished Building Aurora
  echo Thank you for choosing Aurora-Sim
  echo Please report any errors to out Mantis Bug Tracker http://mantis.aurora-sim.org/
fi
