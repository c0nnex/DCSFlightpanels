cd C:\dev\Scripts\
powershell .\UpdateVersion.exe -b Increment -r ddHHmm -i "C:\dev\visualstudio\Projects\FlightPanels\ProUsbPanels\Properties\AssemblyInfo.cs" -o "C:\dev\visualstudio\Projects\FlightPanels\ProUsbPanels\Properties\AssemblyInfo.cs"
powershell .\UpdateVersion.exe -v File -b Increment -r ddHHmm -i "C:\dev\visualstudio\Projects\FlightPanels\ProUsbPanels\Properties\AssemblyInfo.cs" -o "C:\dev\visualstudio\Projects\FlightPanels\ProUsbPanels\Properties\AssemblyInfo.cs"