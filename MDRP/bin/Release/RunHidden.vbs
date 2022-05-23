Set objShell = WScript.CreateObject("WScript.Shell")
objShell.Run("""" & CreateObject("Scripting.FileSystemObject").GetParentFolderName(WScript.ScriptFullName) & "\MDRP.exe"""), 0, True