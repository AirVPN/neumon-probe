#!/bin/bash

# NeuMon Probe - http://www.neumon.org
# Startup Script - v1.0 - 02/08/2013
 
# Flush (clear all existing rules)
iptables -F
iptables -F INPUT
iptables -F OUTPUT
iptables -F FORWARD
     
# Optional: Allow incoming pings (can be disabled)
iptables -A INPUT -p icmp --icmp-type echo-request -j ACCEPT
 
# Allow loopback
iptables -A INPUT -i lo -j ACCEPT
 
# Allow established sessions to receive traffic: 
iptables -A INPUT -m state --state ESTABLISHED,RELATED -j ACCEPT
     
# Allow 22/SSH port
iptables -A INPUT -p tcp --dport 22 -j ACCEPT
    
 
# Blocking traffic
iptables -A OUTPUT -j ACCEPT
iptables -A INPUT -j DROP
iptables -A FORWARD -j DROP

# Update probe
rm -f /home/probe/NeuMonProbe.exe
wget --output-document /home/probe/NeuMonProbe.exe http://www.neumon.org/repository/NeuMonProbe.exe

cd /home/probe/
mono /home/probe/NeuMonProbe.exe
 
exit 0