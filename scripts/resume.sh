#!/bin/bash

# Fix a 'screen' problem with non-root user:
chmod o+rw /dev/pts/*

su - probe -c "screen -x probe"
 
exit 0