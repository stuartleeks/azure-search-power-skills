#!/bin/bash 
set -e

CMD=conda
NAME="Miniconda"

echo -e "\e[34m»»» 📦 \e[32mInstalling \e[33m$NAME \e[35mv\e[0m ..."

curl -sSL https://repo.anaconda.com/miniconda/Miniconda3-latest-Linux-x86_64.sh -o /tmp/miniconda-install.sh
bash /tmp/miniconda-install.sh
rm /tmp/miniconda-install.sh

echo -e "\n\e[34m»»» 💾 \e[32mInstalled to: \e[33m$(which $CMD)"
echo -e "\e[34m»»» 💡 \e[32mVersion details: \e[39m$($CMD --version)"
