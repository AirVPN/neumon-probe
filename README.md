NeuMon.org Probe - http://www.neumon.org/

Released under GPL3.

Developed in C#, run out-of-the-box under Windows, require Mono under Linux, OSX or other systems.


Users can run this software on it's ISP line.

This software collect informations requested by neumon.org projects, to create reports of blocked/censored websites or resources.


Custom DNS, firewall, antivirus, or custom hosts may generate false-positive. 

For this reason, we reccomend to run the probe on a dedicated hardware. A Raspberry-PI (<35$) is perfect.

Before run a probe, look our support forum ( https://airvpn.org/forum/30-net-neutrality-monitor/ ) and indroduce yourself.



--------------------
Installation - Raspberry PI Hardware Probe


Download Raspbian "wheezy" raw image from here: http://www.raspberrypi.org/downloads

Use Win32DiskImager

Choose console-only, choose password.

Login as root, or use sudo.

     apt-get install whois curl tcpdump ntp traceroute sysstat screen unzip psmisc p7zip-full slurm wipe dnsutils conntrack

     apt-get install mono-complete

     mkdir /home/probe

     useradd -d /home/probe -m probe -p `mkpasswd MyProbeUserPassword`

Copy /scripts/startup.sh to /home/probe/startup.sh

Copy /scripts/resume.sh to /home/probe/resume.sh

     chown probe:probe /home/probe/*

     chmod 700 /home/probe/*.sh

In /etc/rc.local:

     su - probe -c "screen -dmS probe /home/probe/startup.sh"



After a reboot, login and use 

     /home/probe/resume.sh

to control the execution. Exit without terminate the process with 'Ctrl-A' and 'd' keys.


