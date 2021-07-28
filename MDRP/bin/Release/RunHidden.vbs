Set objShell = WScript.CreateObject("WScript.Shell")
objShell.Run("taskkill /im GroovyRP.exe"), 0, True
objShell.Run("""" & CreateObject("Scripting.FileSystemObject").GetParentFolderName(WScript.ScriptFullName) & "\GroovyRP.exe"""), 0, True
