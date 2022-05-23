Set objShell = WScript.CreateObject("WScript.Shell")
objShell.Run("taskkill /im MDRP.exe"), 0, True
objShell.Run("""" & CreateObject("Scripting.FileSystemObject").GetParentFolderName(WScript.ScriptFullName) & "\MDRP.exe"""), 0, True