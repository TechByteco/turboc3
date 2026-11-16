Set oWS = WScript.CreateObject("WScript.Shell")
If WScript.Arguments.Count = 0 Then
    sCurrentDir = oWS.CurrentDirectory
Else
    sCurrentDir = WScript.Arguments(0)
End If

sDesktop = oWS.SpecialFolders("Desktop")
sPrograms = oWS.SpecialFolders("Programs")

Set oLink1 = oWS.CreateShortcut(sDesktop & "\Turbo C++.lnk")
oLink1.TargetPath = sCurrentDir & "\Turbo C++.exe"
oLink1.WorkingDirectory = sCurrentDir
oLink1.IconLocation = sCurrentDir & "\tc_icon.ico, 0"
oLink1.Description = "Turbo C++ 3.0"
oLink1.Save

Set oLink2 = oWS.CreateShortcut(sPrograms & "\Turbo C++.lnk")
oLink2.TargetPath = sCurrentDir & "\Turbo C++.exe"
oLink2.WorkingDirectory = sCurrentDir
oLink2.IconLocation = sCurrentDir & "\tc_icon.ico, 0"
oLink2.Description = "Turbo C++ 3.0"
oLink2.Save
