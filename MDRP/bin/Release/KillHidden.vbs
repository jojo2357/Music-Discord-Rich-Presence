Set objShell = WScript.CreateObject("WScript.Shell")
objShell.Run("curl -X POST --connect-timeout 0.05 -d ""{message:\""please die\""}"" localhost:2357"), 0, True