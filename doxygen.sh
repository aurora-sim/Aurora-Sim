DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd ${DIR}
mkdir -p AuroraDocs/doxygen
rm -fr AuroraDocs/doxygen/*
doxygen doxygen.conf