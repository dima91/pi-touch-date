# pi-touch-date

Digital calendar for Raspberry Pi and touchscreen.

Icons created by [Freepik - Flaticon](https://www.flaticon.com/authors/freepik)

## System set-up

+ [Image download](https://downloads.raspberrypi.com/raspios_arm64/images/raspios_arm64-2026-04-21/2026-04-21-raspios-trixie-arm64.img.xz)
    + create `pi` user
    + expand filesystem
    + enable ssh
    + update system
    + install `vim xterm rsync jq`
    + Disable DM with: `raspi-config -> System options -> Boot -> B1 Console Text`
    + Disable auto-login: `raspi-config -> System options -> Auto Login -> No`
    + Disable screen blanking: `raspi-config -> Display options -> Screen Blanking -> No`
+ Install .NET 8.0.420: https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.420-linux-arm64-binaries
+ [Install github-runners](https://medium.com/into-the-ai/raspberry-pi-a-web-server-with-ci-cd-pipeline-fd077b3be63a#d4ea)
+ Start the `github-runner.service`
    + copy service descriptor file to `/etc/systemd/system/` folder
    + `sudo systemctl daemon-reload`
    + `sudo systemctl status github-runner.service`
    + `sudo systemctl start github-runner.service`
